using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using prjGoHike.APIControllers.GoHikeSafe;
using prjGoHike.Models;
using prjGoHike.Services.SpatialJoins;

if (args.Contains("--current-db-readonly"))
{
    await CurrentDatabaseChecks.ReadOnlyAsync();
    return;
}
if (args.Contains("--current-db"))
{
    await CurrentDatabaseChecks.RunAsync();
    return;
}
if (args.Contains("--current-db-temp"))
{
    await CurrentDatabaseChecks.TemporaryTablesAsync();
    return;
}

var checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}
var retry = typeof(SpatialJoinJob).GetMethod(nameof(SpatialJoinJob.ExecuteAsync))!.GetCustomAttribute<AutomaticRetryAttribute>()!;
Check(retry.Attempts == 3 && retry.OnAttemptsExceeded == AttemptsExceededAction.Fail, "Explicit three retries, then final failure.");
foreach (var distance in new[] { 0.01m, 100m, 10000.00m }) Check(SpatialJoinRules.IsValidDistance(distance), "Valid threshold.");
foreach (var distance in new[] { 0m, -1m, 10000.01m, 0.009m, 100.004m, 1.000m }) Check(!SpatialJoinRules.IsValidDistance(distance), "Reject original invalid range/scale.");
Check(SpatialJoinRules.StatusForHangfireState("Scheduled") == "RetryPending", "Scheduled retry is not final failure.");
Check(SpatialJoinRules.StatusForHangfireState("Failed") == "Failed", "Final Hangfire failure is terminal.");

var store = new MemoryRunStore();
var storage = new FakeJobStorage();
var jobs = new FakeJobClient(storage);
var options = Options.Create(new SpatialJoinOptions { Enabled = true });
var service = new SpatialJoinService(store, options, NullLogger<SpatialJoinService>.Instance, jobs, storage);
var job = new SpatialJoinJob(store, NullLogger<SpatialJoinJob>.Instance);
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("spatial-tests-only-signing-key-012345678901"));
var builder = WebApplication.CreateBuilder();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Logging.ClearProviders();
builder.Services.AddSingleton<ISpatialJoinService>(service);
builder.Services.AddControllers().AddApplicationPart(typeof(SpatialJoinsApiController).Assembly);
builder.Services.AddOpenApi("v1", o => o.ShouldInclude = d => d.RelativePath?.StartsWith("api/admin/spatial-joins") == true);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new()
{
    ValidateIssuer = true, ValidIssuer = "tests", ValidateAudience = true, ValidAudience = "tests",
    ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = key
});
builder.Services.AddAuthorization();
await using var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapOpenApi();
await app.StartAsync();
var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
using var client = new HttpClient { BaseAddress = new Uri(address) };
const string route = "/api/admin/spatial-joins";
try
{
    foreach (var role in new string?[] { null, "Member" })
    {
        Authenticate(role);
        var expected = role is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
        await Send(HttpMethod.Post, route, "{\"distanceMeters\":100}", expected);
        foreach (var path in new[] { route, route + "/1", route + "/associations" }) await Send(HttpMethod.Get, path, null, expected);
    }
    Check(store.CreateCalls == 0, "Unauthorized requests never reach persistence.");
    Authenticate("Admin");
    foreach (var payload in new[] { "{}", "{\"distanceMeters\":null}", "{\"distanceMeters\":0}", "{\"distanceMeters\":10000.01}",
        "{\"distanceMeters\":100.004}", "{\"distanceMeters\":1.000}", "{\"distanceMeters\":\"invalid\"}" })
        await Send(HttpMethod.Post, route, payload, HttpStatusCode.BadRequest);
    Check(store.CreateCalls == 0 && jobs.Created.Count == 0, "Validation happens before SQL parameter creation or enqueue.");
    var created = await Send(HttpMethod.Post, route, "{\"distanceMeters\":100,\"status\":\"Succeeded\",\"jobType\":\"Other\",\"hangfireJobId\":\"untrusted\"}", HttpStatusCode.Accepted);
    Check(created!["data"]!["status"]!.GetValue<string>() == "Queued", "202 exposes current bound state.");
    Check(created["data"]!["statusUrl"]!.GetValue<string>() == route + "/1", "Run status URL is stable.");
    Check(!created["data"]!.AsObject().ContainsKey("hangfireJobId"), "Internal Hangfire ID is not exposed.");
    Check(store.Runs[1].JobType == "SpatialJoin" && store.Runs[1].HangfireJobId == "1", "Server owns job type, state and executor ID.");
    Check(jobs.Created[0].Type == typeof(SpatialJoinJob) && jobs.Created[0].Args.Count == 3
        && jobs.Created[0].Args[0] is long id && id == 1 && jobs.Created[0].Args[1] is null,
        "Only RunId is serialized as business input, with special context/token slots.");
    await Send(HttpMethod.Post, route, "{\"distanceMeters\":50}", HttpStatusCode.Conflict);
    Check(jobs.Created.Count == 1, "An active Run blocks enqueue of another threshold.");
    store.ThrowOnCreateLock = true;
    await Send(HttpMethod.Post, route, "{\"distanceMeters\":50}", HttpStatusCode.Conflict);
    store.ThrowOnCreateLock = false;
    var read = await Send(HttpMethod.Get, route + "/1", null, HttpStatusCode.OK);
    Check(read!["data"]!["createdAt"]!.GetValue<string>().EndsWith('Z'), "Run timestamps are serialized as UTC.");
    await Send(HttpMethod.Get, route + "/999", null, HttpStatusCode.NotFound);
    await Send(HttpMethod.Get, route + "/0", null, HttpStatusCode.BadRequest);
    foreach (var suffix in new[] { "?page=0", "?pageSize=101", "?pageSize=0" }) await Send(HttpMethod.Get, route + suffix, null, HttpStatusCode.BadRequest);
    await Send(HttpMethod.Get, route + "/associations?afterTrailId=1", null, HttpStatusCode.BadRequest);
    await Send(HttpMethod.Get, route + "/associations?trailId=0", null, HttpStatusCode.BadRequest);

    store.ThrowOnSync = true;
    try { await job.ExecuteRunAsync(1, "1", CancellationToken.None); Check(false, "An attempt failure must propagate."); }
    catch (TimeoutException) { Check(true, "Attempt exception propagated to Hangfire."); }
    var startedAt = store.Runs[1].StartedAt;
    Check(store.Runs[1].Status == "Processing" && store.Runs[1].FinishedAt is null, "An attempt failure is not terminal.");
    Check(!store.Runs[1].ErrorMessage!.Contains("secret"), "Persisted errors exclude exception details.");
    storage.States["1"] = "Scheduled";
    await service.ReconcileRunAsync(1, "1", CancellationToken.None);
    Check(store.Runs[1].Status == "RetryPending" && store.Runs[1].FinishedAt is null, "Reconciliation reflects actual retry scheduling.");
    await job.ExecuteRunAsync(1, "duplicate", CancellationToken.None);
    Check(store.SyncCalls == 1, "Discarded executor ID does not synchronize.");
    store.ThrowOnSync = false;
    await job.ExecuteRunAsync(1, "1", CancellationToken.None);
    Check(store.Runs[1].Status == "Succeeded" && store.Runs[1].StartedAt == startedAt, "Retry succeeds and preserves first start time.");
    var completedAt = store.Runs[1].FinishedAt;
    await job.ExecuteRunAsync(1, "1", CancellationToken.None);
    storage.States["1"] = "Failed";
    await service.ReconcileRunAsync(1, "1", CancellationToken.None);
    Check(store.SyncCalls == 2 && store.Runs[1].Status == "Succeeded" && store.Runs[1].FinishedAt == completedAt, "Successful Run reentry and stale failure notifications cannot rewrite it.");

    jobs.Unavailable = true;
    var pending = await Send(HttpMethod.Post, route, "{\"distanceMeters\":0.01}", HttpStatusCode.Accepted);
    Check(pending!["data"]!["status"]!.GetValue<string>() == "Pending" && store.Runs[2].HangfireJobId is null, "Persisted Run survives Hangfire outage.");
    store.Runs[2].CreatedAt = DateTime.UtcNow.AddMinutes(-2);
    jobs.Unavailable = false;
    await service.RecoverAsync(CancellationToken.None);
    Check(store.Runs[2].Status == "Queued" && store.Runs[2].HangfireJobId == "2", "Recovery enqueues aged Pending requests.");
    storage.Unavailable = true;
    await service.RecoverAsync(CancellationToken.None);
    Check(store.Runs[2].Status == "Queued" && store.Runs[2].HangfireJobId == "2", "Storage outage is not mistaken for job disappearance.");
    storage.Unavailable = false;
    storage.States.Remove("2");
    await service.RecoverAsync(CancellationToken.None);
    Check(store.Runs[2].Status == "Failed" && store.Runs[2].FinishedAt is not null, "Confirmed missing job terminates its bound Run.");
    await job.ExecuteRunAsync(2, "2", CancellationToken.None);
    Check(store.SyncCalls == 2, "Old final Failed job cannot overwrite newer results.");

    jobs.OnCreate = (jobId, _) => store.BindAsync(3, jobId, true, CancellationToken.None).GetAwaiter().GetResult();
    var racing = await Send(HttpMethod.Post, route, "{\"distanceMeters\":10000.00}", HttpStatusCode.Accepted);
    Check(racing!["data"]!["status"]!.GetValue<string>() == "Processing", "ID binding does not downgrade a worker that already started.");
    jobs.OnCreate = null;
    store.Runs[3].Status = "Failed";
    store.ThrowOnBind = true;
    await Send(HttpMethod.Post, route, "{\"distanceMeters\":10}", HttpStatusCode.Accepted);
    Check(store.Runs[4].HangfireJobId is null && jobs.Created.Count == 4, "Enqueue acknowledgement loss leaves a recoverable Run.");
    store.ThrowOnBind = false;
    await job.ExecuteRunAsync(4, "4", CancellationToken.None);
    Check(store.Runs[4].Status == "Succeeded" && store.Runs[4].HangfireJobId == "4", "Worker adopts unbound Run after enqueue acknowledgement loss.");
    var list = await Send(HttpMethod.Get, route + "?pageSize=1", null, HttpStatusCode.OK);
    Check(list!["data"]!["hasMore"]!.GetValue<bool>() && list["data"]!["items"]!.AsArray().Count == 1, "Run listing is bounded and reports another page.");
    store.Associations.Add(new(1, "Trail", 1, "Indicator", 0m, 1m, DateTime.UtcNow));
    var associations = await Send(HttpMethod.Get, route + "/associations", null, HttpStatusCode.OK);
    Check(associations!["data"]!["items"]!.AsArray().Count == 1, "Current candidate associations have a separate query endpoint.");
    store.ThrowOnAccess = true;
    var unavailable = await Send(HttpMethod.Get, route + "/1", null, HttpStatusCode.ServiceUnavailable);
    Check(!unavailable!.ToJsonString().Contains("secret"), "API storage failures expose controlled responses.");
    await Send(HttpMethod.Post, route, "{\"distanceMeters\":100}", HttpStatusCode.ServiceUnavailable);
    store.ThrowOnAccess = false;
    options.Value.Enabled = false;
    await Send(HttpMethod.Post, route, "{\"distanceMeters\":100}", HttpStatusCode.ServiceUnavailable);
    options.Value.Enabled = true;

    var document = await Send(HttpMethod.Get, "/openapi/v1.json", null, HttpStatusCode.OK);
    Check(document!["paths"]![route]!["post"]!["responses"]!["202"] is not null, "OpenAPI describes accepted background work.");
    Check(document["paths"]![route + "/associations"]!["get"]!["summary"] is not null, "OpenAPI distinguishes current candidates from historical Run results.");
    await using var metadataContext = new MetadataContext();
    var model = metadataContext.Model.FindEntityType(typeof(BackgroundJobRun))!;
    Check(model.GetTableName() == "BackgroundJobRuns", $"Business Run maps to BackgroundJobRuns, actual: {model.GetTableName()}.");
    Check(model.FindProperty("DistanceMeters")!.GetColumnType()!.Replace(" ", "") == "decimal(12,2)",
        $"Business Run distance type matches finalized schema, actual: {model.FindProperty("DistanceMeters")!.GetColumnType()}.");
    Check(metadataContext.Model.FindEntityType(typeof(TrailIndicator))!.FindProperty("EvaluatedScore")!.IsNullable, "Unknown scores remain nullable.");
    Console.WriteLine($"PASS: {checks} spatial workflow, HTTP validation/auth, recovery and metadata checks.");
    await SqlIntegrationChecks.RunAsync(Check);
}
finally { await app.StopAsync(); }

void Authenticate(string? role) => client.DefaultRequestHeaders.Authorization = role is null ? null : new AuthenticationHeaderValue("Bearer",
    new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("tests", "tests", [new Claim(ClaimTypes.Role, role)],
        expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256))));

async Task<JsonNode?> Send(HttpMethod method, string path, string? payload, HttpStatusCode expected)
{
    using var request = new HttpRequestMessage(method, path);
    if (payload is not null) request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
    using var response = await client.SendAsync(request);
    var text = await response.Content.ReadAsStringAsync();
    Check(response.StatusCode == expected, $"{method} {path}: expected {expected}, got {response.StatusCode}: {text}");
    if (expected == HttpStatusCode.Accepted) Check(response.Headers.Location is not null, "202 includes Location.");
    return string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
}

sealed class MetadataContext : GoHikeDataContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder builder) => builder.UseSqlServer(
        "Server=localhost;Database=MetadataOnly;Integrated Security=true", o => o.UseNetTopologySuite());
}
