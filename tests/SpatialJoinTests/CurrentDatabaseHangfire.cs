using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using Hangfire;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using prjGoHike.APIControllers.GoHikeSafe;
using prjGoHike.Models;
using prjGoHike.Services.SpatialJoins;

static partial class CurrentDatabaseChecks
{
    private static async Task VerifyCurrentStoreAndHangfireAsync(SqlConnection connection)
    {
        var connectionString = ConnectionString();
        await using var firstContext = new SqlTestContext(connectionString);
        await using var secondContext = new SqlTestContext(connectionString);
        var options = Options.Create(new SpatialJoinOptions { Enabled = true, LockTimeoutMilliseconds = 300 });
        var firstStore = new SpatialJoinStore(firstContext, options);
        var secondStore = new SpatialJoinStore(secondContext, options);
        var admitted = await Task.WhenAll(firstStore.CreateAsync(100m, CancellationToken.None), secondStore.CreateAsync(50m, CancellationToken.None));
        Check(admitted.Count(r => r.Created) == 1 && admitted[0].Run.Id == admitted[1].Run.Id, "Real concurrent admission creates one Run.");
        var admissionRun = admitted[0].Run;
        try
        {
            Check(await firstStore.BindAsync(admissionRun.Id, "current-admission-test", true, CancellationToken.None), "Actual store binds first executor.");
            Check(!await firstStore.BindAsync(admissionRun.Id, "duplicate-executor", true, CancellationToken.None), "Actual store rejects another executor.");
        }
        finally { await firstStore.ReconcileAsync(admissionRun.Id, "current-admission-test", () => "Failed", CancellationToken.None); }
        await new SpatialJoinJob(firstStore, NullLogger<SpatialJoinJob>.Instance).ExecuteRunAsync(admissionRun.Id, "current-admission-test", CancellationToken.None);
        Check((await firstStore.GetAsync(admissionRun.Id, CancellationToken.None))!.Status == "Failed", "Final Failed Run reentry does not execute SQL.");
        await using (var transaction = (SqlTransaction)await connection.BeginTransactionAsync())
        {
            await ValueAsync<int>(connection, transaction, "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource='GoHike:SpatialJoin',@LockMode='Exclusive',@LockOwner='Transaction',@DbPrincipal='public'; SELECT @r;");
            try { await secondStore.CreateAsync(100m, CancellationToken.None); Check(false, "A blocked admission cannot continue."); }
            catch (SpatialJoinLockException ex) { Check(ex.ReturnCode < 0, "Actual store enforces finite application-lock wait."); }
            await transaction.RollbackAsync();
        }
        Check((await firstStore.GetAssociationsAsync(new(), CancellationToken.None)).Count == 0, "Actual candidate query compiles against current SQL names.");

        var before = await ValueAsync<string>(connection, null, "SELECT * FROM dbo.TrailIndicators ORDER BY Trail_Id,IndicatorId FOR JSON PATH,INCLUDE_NULL_VALUES;");
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:GoHikeDataContext"] = connectionString, ["ConnectionStrings:Hangfire"] = connectionString,
            ["Hangfire:Enabled"] = "true", ["Hangfire:PrepareSchemaIfNecessary"] = "true"
        });
        builder.Services.AddDbContext<GoHikeDataContext>(o => o.UseSqlServer(connectionString, sql => sql.UseNetTopologySuite()));
        builder.Services.AddSpatialJoins(builder.Configuration);
        builder.Services.AddHangfire((_, config) => config.UseFilter(new ImmediateRetryFilter()));
        builder.Services.AddControllers().AddApplicationPart(typeof(SpatialJoinsApiController).Assembly);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("current-db-tests-only-signing-key-123456789012"));
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new()
        {
            ValidateIssuer = true, ValidIssuer = "current-db-tests", ValidateAudience = true, ValidAudience = "current-db-tests",
            ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = key
        });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            "current-db-tests", "current-db-tests", [new Claim(ClaimTypes.Role, "Admin")], expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256))));
        try
        {
            // Current sources have known missing Segments. Do not auto-repair or hide them to force success.
            var accepted = await SendAsync(client, HttpMethod.Post, "/api/admin/spatial-joins", "{\"distanceMeters\":100}", HttpStatusCode.Accepted);
            var id = accepted!["data"]!["id"]!.GetValue<long>();
            Console.WriteLine($"Current DB real Hangfire RunId: {id}");
            var failed = await WaitForRunAsync(client, id, "Failed");
            Check(failed["data"]!["errorMessage"]!.GetValue<string>().Contains("缺少空間"), "Real API exposes controlled missing-source failure.");
            Check(failed["data"]!["finishedAt"] is not null && failed["data"]!["startedAt"] is not null, "Terminal failure includes UTC execution times.");
            await using var scope = app.Services.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<ISpatialJoinStore>();
            var service = scope.ServiceProvider.GetRequiredService<SpatialJoinService>();
            var run = (await store.GetAsync(id, CancellationToken.None))!;
            using (var storage = scope.ServiceProvider.GetRequiredService<JobStorage>().GetConnection())
            {
                Check(storage.GetStateData(run.HangfireJobId!)!.Name == "Failed", "Actual Hangfire applied final failure.");
                Check(storage.GetJobParameter(run.HangfireJobId!, "RetryCount") == "3", "Actual production job performs exactly three retries.");
            }
            Check(await ValueAsync<string>(connection, null, "SELECT * FROM dbo.TrailIndicators ORDER BY Trail_Id,IndicatorId FOR JSON PATH,INCLUDE_NULL_VALUES;") == before,
                "Actual failing worker leaves all current associations unchanged.");
            var associations = await SendAsync(client, HttpMethod.Get, "/api/admin/spatial-joins/associations", null, HttpStatusCode.OK);
            Check(associations!["data"]!["items"]!.AsArray().Count == 0, "Actual Admin candidate endpoint returns current results.");
            await SendAsync(client, HttpMethod.Get, "/api/admin/spatial-joins?pageSize=1", null, HttpStatusCode.OK);

            var strandedService = new SpatialJoinService(store, Options.Create(new SpatialJoinOptions { Enabled = true }),
                NullLogger<SpatialJoinService>.Instance, new UnavailableClient(), scope.ServiceProvider.GetRequiredService<JobStorage>());
            var stranded = await strandedService.CreateAsync(50m, CancellationToken.None);
            Check(stranded.Run.Status == "Pending", "Current business DB persists Pending during enqueue outage.");
            await ExecuteAsync(connection, null, "UPDATE dbo.BackgroundJobRuns SET CreatedAt=DATEADD(minute,-2,SYSUTCDATETIME()) WHERE Id=@Id;",
                new SqlParameter("@Id", SqlDbType.BigInt) { Value = stranded.Run.Id });
            await SendAsync(client, HttpMethod.Post, "/api/admin/spatial-joins", "{\"distanceMeters\":75}", HttpStatusCode.Conflict);
            await service.RecoverAsync(CancellationToken.None);
            await WaitForRunAsync(client, stranded.Run.Id, "Failed");
            Check((await store.GetAsync(stranded.Run.Id, CancellationToken.None))!.HangfireJobId is not null, "Actual recovery binds and executes stranded Pending.");
            var recurring = scope.ServiceProvider.GetRequiredService<JobStorage>().GetConnection();
            using (recurring) Check(recurring.GetRecurringJobs().Any(r => r.Id == SpatialJoinRules.RecoveryJobId), "Minutely recovery is registered in actual Hangfire storage.");
        }
        finally { await app.StopAsync(); }
    }

    private static async Task<JsonNode> WaitForRunAsync(HttpClient client, long id, string status)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        string? startedAt = null;
        while (true)
        {
            using var response = await client.GetAsync($"/api/admin/spatial-joins/{id}", timeout.Token);
            Check(response.StatusCode == HttpStatusCode.OK, "Actual Run polling returns 200.");
            var document = JsonNode.Parse(await response.Content.ReadAsStringAsync(timeout.Token))!;
            var run = document["data"]!;
            var observed = run["startedAt"]?.GetValue<string>();
            if (observed is not null)
            {
                startedAt ??= observed;
                Check(startedAt == observed, "Actual retries preserve first start timestamp.");
            }
            var currentStatus = run["status"]!.GetValue<string>();
            if (currentStatus == status) return document;
            Check(!SpatialJoinRules.IsTerminal(currentStatus), $"Expected {status}, got {currentStatus}.");
            await Task.Delay(250, timeout.Token);
        }
    }

    private static async Task<JsonNode?> SendAsync(HttpClient client, HttpMethod method, string path, string? body, HttpStatusCode status)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        Check(response.StatusCode == status, $"Expected {status}, got {response.StatusCode}: {text}");
        if (status == HttpStatusCode.Accepted) Check(response.Headers.Location is not null, "Real POST returns Location.");
        return string.IsNullOrEmpty(text) ? null : JsonNode.Parse(text);
    }
}
