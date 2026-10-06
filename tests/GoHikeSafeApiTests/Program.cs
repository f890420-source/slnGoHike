using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using prjGoHike.APIControllers.GoHikeSafe;
using prjGoHike.Models;
using prjGoHike.Hubs;
using prjGoHike.Services;

// Run: dotnet run --project tests/GoHikeSafeApiTests
// Exercises real routing, JSON/model validation, JWT authorization, controllers,
// response DTOs and save calls using a database test double, without new packages.
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("GoHikeSafe-tests-only-signing-key-123456789"));
var builder = WebApplication.CreateBuilder();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Logging.ClearProviders();
var context = new TestDataContext();
builder.Services.AddSingleton<GoHikeDataContext>(context);
var alertHub = new RecordingAlertHub(() => context.Saves);
var alertLogger = new RecordingAlertLogger();
builder.Services.AddSingleton<IHubContext<EventHub>>(alertHub);
builder.Services.AddSingleton<ILogger<DisasterAlertRealtimeService>>(alertLogger);
builder.Services.AddScoped<DisasterAlertRealtimeService>();
builder.Services.AddControllers().AddApplicationPart(typeof(TrailsApiController).Assembly)
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new NetTopologySuite.IO.Converters.GeoJsonConverterFactory()));
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new NetTopologySuite.IO.Converters.GeoJsonConverterFactory()));
builder.Services.AddOpenApi("v1", options =>
{
    options.ShouldInclude = description => description.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true;
    options.AddSchemaTransformer<prjGoHike.Services.GeoJsonSchemaTransformer>();
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = "tests", ValidateAudience = true, ValidAudience = "tests",
        ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = key
    };
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
var checks = 0;
GeoJsonValidationChecks.Run(Check);
var definitions = new[]
{
    new Resource("trails", "id", "trailSegDtos", "isPublished", "trailName", """
        {"trailName":" Test trail ","region":" Taiwan ","difficultyLevel":3,"distanceKm":12.34,
         "estimatedHours":5.5,"permitRequired":true,"guideRequired":true,"regulationNote":" note ","isPublished":false,
         "trailSegDtos":[{"shape":{"type":"LineString","coordinates":[[121,24],[121.1,24.1]]}}]}
        """),
    new Resource("indicators", "id", "indicatorSegments", "isActive", "indicatorName", """
        {"indicatorName":" Test indicator ","indicatorType":" Risk ","weight":1.234,"indicatorLevel":2,
         "indicatorDescription":" description ","dataSource":" source ","isActive":false,
         "indicatorSegments":[{"segmentName":" segment ","segmentLevel":3,"description":" details ",
         "sourceFeatureId":"feature","shape":{"type":"Point","coordinates":[121,24]}}]}
        """),
    new Resource("disasteralerts", "alertId", "alertSegments", "isActive", "alertTitle", """
        {"alertType":" Rain ","alertTitle":" Test alert ","alertDescription":" description ","severityLevel":3,
         "effectiveFrom":"2026-09-01T00:00:00","effectiveTo":"2026-10-01T00:00:00",
         "sourceAgency":" agency ","sourceUrl":"https://example.com/alerts","isActive":false,
         "alertSegments":[{"segmentName":" segment ","segmentLevel":3,"description":" details ",
         "sourceFeatureId":"feature","shape":{"type":"Polygon","coordinates":[[[121,24],[121.1,24],[121.1,24.1],[121,24]]]}}]}
        """)
};
try
{
    var openApi = (await Send(HttpMethod.Get, "/openapi/v1.json", null, HttpStatusCode.OK))!;
    foreach (var (route, methods) in new[]
    {
        ("/api/trailfeatures", new[] { "get", "post" }),
        ("/api/trailfeatures/{id}", new[] { "get", "put", "delete" }),
        ("/api/trailfeatures/admin", new[] { "get" }),
        ("/api/trailfeatures/admin/{id}", new[] { "get" })
    })
        foreach (var method in methods)
            Check(openApi["paths"]![route]![method]!["summary"] is not null, "OpenAPI documents every feature operation.");
    foreach (var type in new[] { "TrailFeatureReportRequestDto", "TrailFeatureUpdateRequestDto" })
    {
        var schema = openApi["components"]!["schemas"]![type]!;
        var location = schema["properties"]!["location"]!;
        if (location["$ref"] is JsonNode reference)
            location = openApi["components"]!["schemas"]![reference.GetValue<string>().Split('/').Last()]!;
        Check(location["properties"]!["type"]!["enum"]!.AsArray().Single()!.GetValue<string>() == "Point",
            "OpenAPI request location is restricted to Point.");
        Check(location["example"]!["type"]!.GetValue<string>() == "Point"
            && location["properties"]!["coordinates"]!["minItems"]!.GetValue<int>() == 2
            && location["properties"]!["coordinates"]!["maxItems"]!.GetValue<int>() == 3,
            "OpenAPI supplies a valid Point example and coordinate dimensions.");
    }
    Check(openApi["components"]!["schemas"]!["Geometry"]!["properties"]!["type"]!["enum"]!.AsArray().Count == 7,
        "Point schema does not restrict the existing generic Geometry schema.");
    var reportProperties = openApi["components"]!["schemas"]!["TrailFeatureReportRequestDto"]!["properties"]!.AsObject();
    Check(!reportProperties.ContainsKey("isAvailable") && !reportProperties.ContainsKey("reliabilityLevel")
        && !reportProperties.ContainsKey("dataSource") && !reportProperties.ContainsKey("featureId"),
        "OpenAPI report input excludes server controlled fields.");
    foreach (var resource in definitions)
    {
        var path = "/api/" + resource.Route;
        var payload = JsonNode.Parse(resource.Json)!.AsObject();
        if (resource.Route != "trails")
            payload[resource.Segments]![0]!["shape"] = JsonNode.Parse(GeoJsonValidationChecks.ReversedPolygon);
        foreach (var role in new string?[] { null, "Member" })
        {
            Authenticate(role);
            var denied = role is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
            await Send(HttpMethod.Get, path + "/admin", null, denied);
            await Send(HttpMethod.Get, path + "/admin/1", null, denied);
            await Send(HttpMethod.Post, path, payload, denied);
            await Send(HttpMethod.Put, path + "/1", payload, denied);
            await Send(HttpMethod.Delete, path + "/1", null, denied);
        }
        Authenticate("Admin");
        var saveCount = context.Saves;
        await Send(HttpMethod.Post, path, new JsonObject(), HttpStatusCode.BadRequest);
        var bad = payload.DeepClone().AsObject();
        bad[resource.Name] = " ";
        await Send(HttpMethod.Post, path, bad, HttpStatusCode.BadRequest);
        bad[resource.Name] = new string('x', 201);
        await Send(HttpMethod.Post, path, bad, HttpStatusCode.BadRequest);
        bad = payload.DeepClone().AsObject();
        bad[resource.Segments]![0]!["shape"] = JsonNode.Parse("""{"type":"Point","coordinates":[900,24]}""");
        await Send(HttpMethod.Post, path, bad, HttpStatusCode.BadRequest);
        bad[resource.Segments]![0]!["shape"] = null;
        await Send(HttpMethod.Post, path, bad, HttpStatusCode.BadRequest);
        if (resource.Route == "trails")
        {
            bad = payload.DeepClone().AsObject(); bad[resource.Segments] = new JsonArray();
            await Send(HttpMethod.Post, path, bad, HttpStatusCode.BadRequest);
            bad = payload.DeepClone().AsObject(); bad["estimatedHours"] = -1;
            await Send(HttpMethod.Post, path, bad, HttpStatusCode.BadRequest);
        }
        if (resource.Route == "indicators")
        {
            bad = payload.DeepClone().AsObject(); bad["weight"] = 1000;
            await Send(HttpMethod.Post, path, bad, HttpStatusCode.BadRequest);
        }
        if (resource.Route == "disasteralerts")
        {
            bad = payload.DeepClone().AsObject(); bad["effectiveTo"] = "2026-08-01T00:00:00";
            await Send(HttpMethod.Post, path, bad, HttpStatusCode.BadRequest);
            bad = payload.DeepClone().AsObject(); bad["sourceUrl"] = "invalid";
            await Send(HttpMethod.Post, path, bad, HttpStatusCode.BadRequest);
        }
        Check(context.Saves == saveCount, "Invalid/unauthorized input must not be saved.");
        var created = (await Send(HttpMethod.Post, path, payload, HttpStatusCode.Created))!["data"]!;
        var id = created[resource.Id]!.GetValue<long>();
        Check(id > 0 && context.Saves == saveCount + 1, "Create assigns an ID and saves once.");
        Check(created[resource.Name]!.GetValue<string>() == payload[resource.Name]!.GetValue<string>().Trim(), "Names are trimmed.");
        var segmentId = created[resource.Segments]![0]!["id"]!.GetValue<long>();
        Check(segmentId > 0, "Created segments have IDs.");
        if (resource.Route != "trails") CheckStoredPolygon(resource.Route, id);
        if (resource.Route == "trails") Check(created["estimatedHours"]!.GetValue<decimal>() == 5.5m, "EstimatedHours is persisted/returned.");
        var detail = (await Send(HttpMethod.Get, path + "/admin/" + id, null, HttpStatusCode.OK))!["data"]!;
        Check(JsonNode.DeepEquals(created, detail), "Admin detail includes every persisted DTO field and segment.");
        var list = (await Send(HttpMethod.Get, path + "/admin", null, HttpStatusCode.OK))!["data"]!.AsArray();
        Check(list.Count == 1, "Admin list includes unpublished/inactive resources.");
        Authenticate(null);
        await Send(HttpMethod.Get, path + "/" + id, null, HttpStatusCode.NotFound);
        Check((await Send(HttpMethod.Get, path, null, HttpStatusCode.OK))!["data"]!.AsArray().Count == 0, "Public list excludes inactive resources.");
        Authenticate("Admin");
        payload[resource.Id] = id;
        var savesBeforeInvalidGeometry = context.Saves;
        bad = payload.DeepClone().AsObject();
        bad[resource.Segments]![0]!["shape"] = JsonNode.Parse("""
            {"type":"LineString","coordinates":[[121,24,10,20],[121.1,24.1]]}
            """);
        foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put })
        {
            var geometryFailure = await Send(method, method == HttpMethod.Post ? path : path + "/" + id,
                bad, HttpStatusCode.BadRequest);
            var errors = geometryFailure!["errors"]!.AsObject();
            Check(errors.Any(error => error.Key.Contains("shape", StringComparison.OrdinalIgnoreCase)
                && error.Value!.AsArray().Any(message => message!.GetValue<string>().Contains("座標"))),
                "GeoJSON errors identify the shape field and include a controlled Chinese reason.");
        }
        Check(context.Saves == savesBeforeInvalidGeometry, "Invalid GeoJSON must not be saved by POST or PUT.");
        if (resource.Route != "trails")
        {
            var levelField = resource.Route == "indicators" ? "indicatorLevel" : "severityLevel";
            var savesBeforeInvalidLevels = context.Saves;
            foreach (var invalidLevel in new[] { 0, 6 })
            {
                foreach (var nested in new[] { false, true })
                {
                    bad = payload.DeepClone().AsObject();
                    var target = nested ? bad[resource.Segments]![0]!.AsObject() : bad;
                    target[nested ? "segmentLevel" : levelField] = invalidLevel;
                    await Send(HttpMethod.Post, path, bad, HttpStatusCode.BadRequest);
                    await Send(HttpMethod.Put, path + "/" + id, bad, HttpStatusCode.BadRequest);
                }
            }
            if (resource.Route == "disasteralerts")
            {
                bad = payload.DeepClone().AsObject(); bad.Remove(levelField);
                await Send(HttpMethod.Post, path, bad, HttpStatusCode.BadRequest);
                await Send(HttpMethod.Put, path + "/" + id, bad, HttpStatusCode.BadRequest);
            }
            Check(context.Saves == savesBeforeInvalidLevels, "Invalid or omitted required levels must not be saved.");
            foreach (var validLevel in new[] { 1, 5 })
            {
                var valid = payload.DeepClone().AsObject();
                valid[levelField] = validLevel;
                valid[resource.Segments]![0]!["segmentLevel"] = validLevel;
                var result = (await Send(HttpMethod.Put, path + "/" + id, valid, HttpStatusCode.OK))!["data"]!;
                Check(result[levelField]!.GetValue<int>() == validLevel
                    && result[resource.Segments]![0]!["segmentLevel"]!.GetValue<int>() == validLevel,
                    "Both level boundaries are accepted and returned.");
            }
            var nullable = payload.DeepClone().AsObject();
            if (resource.Route == "indicators") nullable[levelField] = null;
            nullable[resource.Segments]![0]!["segmentLevel"] = null;
            var nullableResult = (await Send(HttpMethod.Put, path + "/" + id, nullable, HttpStatusCode.OK))!["data"]!;
            Check((resource.Route != "indicators" || nullableResult[levelField] is null)
                && nullableResult[resource.Segments]![0]!["segmentLevel"] is null,
                "Nullable levels accept null.");
            // Level checks replaced the segments; use the current ID for the preservation check below.
            segmentId = nullableResult[resource.Segments]![0]!["id"]!.GetValue<long>();
        }
        await Send(HttpMethod.Put, path + "/99999", payload, HttpStatusCode.BadRequest);
        payload[resource.Id] = 99999;
        await Send(HttpMethod.Put, path + "/99999", payload, HttpStatusCode.NotFound);
        payload[resource.Id] = id;
        payload[resource.Active] = true;
        payload.Remove(resource.Segments);
        var updated = (await Send(HttpMethod.Put, path + "/" + id, payload, HttpStatusCode.OK))!["data"]!;
        Check(updated[resource.Segments]![0]!["id"]!.GetValue<long>() == segmentId, "Omitted segments are preserved.");
        if (resource.Route != "trails") CheckStoredPolygon(resource.Route, id);
        Authenticate(null);
        await Send(HttpMethod.Get, path + "/" + id, null, HttpStatusCode.OK);
        Check((await Send(HttpMethod.Get, path, null, HttpStatusCode.OK))!["data"]!.AsArray().Count == 1, "Public list includes active resources.");
        Authenticate("Admin");
        payload[resource.Segments] = JsonNode.Parse(resource.Json)![resource.Segments]!.DeepClone();
        updated = (await Send(HttpMethod.Put, path + "/" + id, payload, HttpStatusCode.OK))!["data"]!;
        Check(updated[resource.Segments]!.AsArray().Count == 1 && updated[resource.Segments]![0]!["id"]!.GetValue<long>() != segmentId, "Provided segments replace old segments.");
        if (resource.Route != "trails")
        {
            payload[resource.Segments] = new JsonArray();
            updated = (await Send(HttpMethod.Put, path + "/" + id, payload, HttpStatusCode.OK))!["data"]!;
            Check(updated[resource.Segments]!.AsArray().Count == 0, "Empty segments clear the resource's geometry.");
        }
        var link = new AlertsTrail { AlertId = id, TrailId = id };
        var indicatorLink = new TrailIndicator { IndicatorId = id, TrailId = id };
        if (resource.Route == "indicators") context.TrailIndicators.Add(indicatorLink);
        else context.AlertsTrails.Add(link);
        saveCount = context.Saves;
        await Send(HttpMethod.Delete, path + "/" + id, null, HttpStatusCode.Conflict);
        Check(context.Saves == saveCount, "Delete conflict does not save.");
        if (resource.Route == "indicators") context.TrailIndicators.Remove(indicatorLink);
        else context.AlertsTrails.Remove(link);
        if (resource.Route == "trails")
        {
            var hike = new HikeRecordDetail { TrailId = id }; context.HikeRecordDetails.Add(hike);
            await Send(HttpMethod.Delete, path + "/" + id, null, HttpStatusCode.Conflict);
            context.HikeRecordDetails.Remove(hike);
            var feature = new TrailFeature { TrailId = id }; context.TrailFeatures.Add(feature);
            await Send(HttpMethod.Delete, path + "/" + id, null, HttpStatusCode.Conflict);
            context.TrailFeatures.Remove(feature);
            context.TrailIndicators.Add(indicatorLink);
            await Send(HttpMethod.Delete, path + "/" + id, null, HttpStatusCode.Conflict);
            context.TrailIndicators.Remove(indicatorLink);
            var subscription = new TrailSubscription { TrailId = id }; context.TrailSubscriptions.Add(subscription);
            await Send(HttpMethod.Delete, path + "/" + id, null, HttpStatusCode.Conflict);
            context.TrailSubscriptions.Remove(subscription);
            var report = new TripReport { TrailId = id }; context.TripReports.Add(report);
            await Send(HttpMethod.Delete, path + "/" + id, null, HttpStatusCode.Conflict);
            context.TripReports.Remove(report);
        }
        await Send(HttpMethod.Delete, path + "/" + id, null, HttpStatusCode.OK);
        Check(context.Saves == saveCount + 1, "Successful delete saves once.");
        await Send(HttpMethod.Get, path + "/admin/" + id, null, HttpStatusCode.NotFound);
        await Send(HttpMethod.Delete, path + "/" + id, null, HttpStatusCode.NotFound);
        await Send(HttpMethod.Get, path + "/0", null, HttpStatusCode.BadRequest);
        await Send(HttpMethod.Get, path + "/admin/0", null, HttpStatusCode.BadRequest);
        await Send(HttpMethod.Delete, path + "/0", null, HttpStatusCode.BadRequest);
        context.FailSave = true;
        var failure = await Send(HttpMethod.Post, path, JsonNode.Parse(resource.Json), HttpStatusCode.InternalServerError);
        Check(!failure!.ToJsonString().Contains("private database details"), "Database errors do not leak internals.");
        context.FailSave = false;
    }
    await TrailFeatureApiChecks.RunAsync(context, Send, (role, userId) => Authenticate(role, userId), Check);
    await TrailIndicatorApiChecks.RunAsync(context, Send, (role, userId) => Authenticate(role, userId), Check, openApi);
    await AlertRealtimeChecks.RunAsync(context, alertHub, alertLogger, Send,
        role => Authenticate(role), Check);
    // Use the production EF model/provider to ensure relational query translation
    // works, including nested public segment DTO projections.
    await using var sql = new TranslationContext();
    TrailIndicatorApiChecks.CheckSqlTranslation(sql, Check);
    Check(sql.Trails.Include(x => x.TrailSegments).Where(x => x.IsPublished).ToQueryString().Contains("TrailSegments"), "SQL trail query includes segments.");
    Check(sql.Indicators.Include(x => x.IndicatorSegments).Where(x => x.IsActive).ToQueryString().Contains("IndicatorSegments"), "SQL indicator query includes segments.");
    Check(sql.DisasterAlerts.Include(x => x.AlertSegments).Where(x => x.IsActive).ToQueryString().Contains("AlertSegments"), "SQL alert query includes segments.");
    Check(sql.TrailFeatures.AsNoTracking().Where(x => x.IsAvailable && x.Trail.IsPublished)
        .Where(x => x.TrailId == 1 && x.FeatureType == "WaterSource").OrderBy(x => x.FeatureId)
        .ToQueryString().Contains("INNER JOIN"), "Public feature filters translate with the published trail join.");
    var featureModel = sql.Model.FindEntityType(typeof(TrailFeature))!;
    Check(featureModel.FindProperty(nameof(TrailFeature.Location))!.GetColumnType() == "geography",
        "Feature location uses SQL Server geography.");
    Check(sql.Indicators.Where(x => x.IsActive).Select(x => new prjGoHike.DTO.GoHikeSafe.IndicatorPublicDto
    {
        id = x.IndicatorId, IndicatorName = x.IndicatorName, IndicatorType = x.IndicatorType,
        IndiSegments = x.IndicatorSegments.Select(s => new prjGoHike.DTO.GoHikeSafe.IndicatorSegmentPublicDto { id = s.IndicatorSegmentId, Shape = s.Shape, SegmentName = s.SegmentName }).ToList()
    }).ToQueryString().Contains("IndicatorSegments"), "Public indicator DTO projection translates to SQL.");
    Console.WriteLine($"PASS: {checks} checks across four API modules, HTTP validation/auth, feature reports, segments, conflicts, alert realtime API/MVC behavior and SQL query translation.");
}
finally { await app.StopAsync(); }

void Authenticate(string? role, string userId = "1")
{
    client.DefaultRequestHeaders.Authorization = role is null ? null : new AuthenticationHeaderValue("Bearer",
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("tests", "tests",
            [new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, role)],
            expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256))));
}
async Task<JsonNode?> Send(HttpMethod method, string path, JsonNode? body, HttpStatusCode expected)
{
    var notificationCount = alertHub.Messages.Count;
    var savesBefore = context.Saves;
    using var request = new HttpRequestMessage(method, path);
    if (body is not null) request.Content = JsonContent.Create(body);
    using var response = await client.SendAsync(request);
    var text = await response.Content.ReadAsStringAsync();
    Check(response.StatusCode == expected, $"{method} {path}: expected {expected}, got {response.StatusCode}: {text}");
    var result = string.IsNullOrEmpty(text) ? null : JsonNode.Parse(text);
    var alertWrite = path.StartsWith("/api/disasteralerts", StringComparison.Ordinal)
        && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Delete);
    if (alertWrite && (expected == HttpStatusCode.Created || expected == HttpStatusCode.OK))
    {
        var id = method == HttpMethod.Delete ? long.Parse(path.Split('/').Last())
            : result!["data"]!["alertId"]!.GetValue<long>();
        AlertRealtimeChecks.CheckPublication(alertHub, notificationCount, savesBefore, id, Check);
    }
    else
    {
        Check(alertHub.Messages.Count == notificationCount,
            "Rejected writes, reads and other API modules do not publish alert notifications.");
    }
    return result;
}
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}
void CheckStoredPolygon(string route, long id)
{
    var shape = route == "indicators"
        ? context.Indicators.Single(x => x.IndicatorId == id).IndicatorSegments.Single().Shape
        : context.DisasterAlerts.Single(x => x.AlertId == id).AlertSegments.Single().Shape;
    GeoJsonValidationChecks.CheckOrientation(shape, Check);
    var polygon = (NetTopologySuite.Geometries.Polygon)shape;
    Check(polygon.ExteriorRing.CoordinateSequence.GetZ(0) == 10
        && polygon.GetInteriorRingN(0).CoordinateSequence.GetZ(0) == 20,
        "POST/PUT store corrected polygons with original heights.");
}
record Resource(string Route, string Id, string Segments, string Active, string Name, string Json);
sealed class TranslationContext : GoHikeDataContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlServer("Server=localhost;Database=Unused;Integrated Security=true", x => x.UseNetTopologySuite());
}
