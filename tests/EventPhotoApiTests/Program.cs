using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using prjGoHike.Hubs;
using prjGoHike.Models;
using prjGoHike.Services.forum;

// dotnet run --project tests/EventPhotoApiTests
// Optional: -- --live-cloudinary uploads one temporary event image using backend
// configuration, reads it over HTTPS and removes it. All activity data is in memory.
var builder = WebApplication.CreateBuilder();
builder.WebHost.UseUrls("http://127.0.0.1:0");
var webRoot = Path.Combine(Path.GetTempPath(), "gohike-event-photos-" + Guid.NewGuid());
Directory.CreateDirectory(Path.Combine(webRoot, "assets", "JoinGroup_Images"));
builder.Environment.WebRootPath = webRoot;
builder.Logging.ClearProviders();
var context = new EventPhotoContext();
var cloud = new RecordingCloudinary(() => context.Saves);
var logger = new RecordingEventLogger();
var hub = new EventPhotoHub();
CloudinaryService? liveCloud = null;
builder.Services.AddSingleton<GoHikeDataContext>(context);
builder.Services.AddScoped<CloudinaryService>(_ => liveCloud ?? cloud);
builder.Services.AddSingleton<ILogger<EventDataAPIController>>(logger);
builder.Services.AddSingleton<IHubContext<EventHub>>(hub);
builder.Services.AddControllers().AddApplicationPart(typeof(EventDataAPIController).Assembly);
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Event-photo-tests-only-signing-key-123456789"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = "tests", ValidateAudience = true, ValidAudience = "tests",
        ValidateIssuerSigningKey = true, IssuerSigningKey = key, ValidateLifetime = true
    });
builder.Services.AddAuthorization();
await using var app = builder.Build();
// Keep original database exceptions out of test HTTP responses.
app.UseExceptionHandler(handler => handler.Run(http =>
{
    http.Response.StatusCode = 500;
    return http.Response.WriteAsJsonAsync(new { message = "test database failure" });
}));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
await app.StartAsync();
var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
using var client = new HttpClient { BaseAddress = new Uri(address) };
var checks = 0;
const string path = "/api/EventDataAPI";
const string oldCloudUrl = "https://res.cloudinary.com/test-cloud/image/upload/v122/gohike/events/old.png";

try
{
    Authenticate(null);
    await Send(HttpMethod.Post, path, Form(), HttpStatusCode.Unauthorized);
    Check(cloud.Uploads.Count == 0, "Unauthenticated create cannot upload.");
    Authenticate(42);

    Reset();
    var created = await Send(HttpMethod.Post, path, Form(), HttpStatusCode.OK);
    Check(created!["data"]!["activityPhoto"]!.GetValue<string>() == "", "Create without a file preserves the empty image/default-photo contract.");
    Check(cloud.Uploads.Count == 0, "Create without an image never uploads.");

    foreach (var extension in new[] { ".jpg", ".jpeg", ".png", ".GIF" })
    {
        Reset();
        created = await Send(HttpMethod.Post, path, Form("event" + extension), HttpStatusCode.OK);
        Check(created!["data"]!["activityPhoto"]!.GetValue<string>() == RecordingCloudinary.PhotoUrl,
            "Create returns the Cloudinary URL for " + extension);
        Check(context.EventData.Single().ActivityPhoto == RecordingCloudinary.PhotoUrl,
            "Create stores the HTTPS URL rather than file bytes or a local path.");
        Check(cloud.Uploads.Single().Folder == "gohike/events", "Upload uses the event folder.");
        Check(!Directory.EnumerateFiles(webRoot, "*", SearchOption.AllDirectories).Any(), "Uploads do not create local image files.");
        // The database double does not populate EF navigation properties automatically.
        context.EventData.Single().Mountain = new Mountain { MountainId = 1, MountainName = "測試山岳" };
        var list = await Send(HttpMethod.Get, path, null, HttpStatusCode.OK);
        Check(list!["data"]![0]!["activityPhoto"]!.GetValue<string>() == RecordingCloudinary.PhotoUrl,
            "Reloading the activity API returns the same Cloudinary image URL.");
    }

    foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put })
    {
        foreach (var invalid in new[] { ("event.webp", 1), ("event.png", 10 * 1024 * 1024 + 1) })
        {
            Reset();
            context.Seed(oldCloudUrl);
            var before = context.Saves;
            await Send(method, method == HttpMethod.Post ? path : path + "/100", Form(invalid.Item1, invalid.Item2), HttpStatusCode.BadRequest);
            Check(cloud.Uploads.Count == 0 && context.Saves == before, "Invalid format/size is rejected before upload or save.");
        }

        Reset();
        context.Seed(oldCloudUrl);
        cloud.FailUpload = true;
        var failure = await Send(method, method == HttpMethod.Post ? path : path + "/100", Form("event.png"), HttpStatusCode.ServiceUnavailable);
        Check(failure!["message"]!.GetValue<string>() == "圖片上傳失敗，請稍後再試", "Cloudinary failure returns a useful, sanitized 503 error.");
        Check(context.EventData.Single().ActivityPhoto == oldCloudUrl && cloud.Deletions.Count == 0,
            "Upload failure keeps the original image.");

        Reset();
        context.Seed(oldCloudUrl);
        cloud.Url = "https://res.cloudinary.com/test-cloud/image/upload/gohike/events/" + new string('x', 220) + ".png";
        await Send(method, method == HttpMethod.Post ? path : path + "/100", Form("event.png"), HttpStatusCode.InternalServerError);
        Check(cloud.Deletions.Single().Url == cloud.Url, "An overlong uploaded URL is cleaned up before database persistence.");

        foreach (var failCleanup in new[] { false, true })
        {
            Reset();
            context.Seed(oldCloudUrl);
            context.FailSave = true;
            cloud.FailDelete = failCleanup;
            var before = context.Saves;
            var notifications = ((EventPhotoClients)hub.Clients).Sends;
            var warnings = logger.Levels.Count(x => x == LogLevel.Warning);
            await Send(method, method == HttpMethod.Post ? path : path + "/100", Form("event.png"), HttpStatusCode.InternalServerError);
            Check(context.Saves == before && cloud.Deletions.Single().Url == RecordingCloudinary.PhotoUrl,
                "Failed save cleans up only the newly uploaded image.");
            Check(((EventPhotoClients)hub.Clients).Sends == notifications, "Failed save does not publish an activity change.");
            if (failCleanup) Check(logger.Levels.Count(x => x == LogLevel.Warning) == warnings + 1,
                "Cleanup failure is logged while retaining the database failure response.");
        }
    }

    Reset();
    context.Seed(oldCloudUrl);
    await Send(HttpMethod.Put, path + "/100", Form(), HttpStatusCode.OK);
    Check(context.EventData.Single().ActivityPhoto == oldCloudUrl && cloud.Uploads.Count == 0 && cloud.Deletions.Count == 0,
        "Editing without a new file keeps the image and does not call Cloudinary.");

    foreach (var method in new[] { HttpMethod.Put, HttpMethod.Delete })
    {
        foreach (var failCleanup in new[] { false, true })
        {
            Reset();
            context.Seed(oldCloudUrl);
            cloud.FailDelete = failCleanup;
            var before = context.Saves;
            var warnings = logger.Levels.Count(x => x == LogLevel.Warning);
            await Send(method, path + "/100", method == HttpMethod.Put ? Form("event.png") : null, HttpStatusCode.OK);
            Check(cloud.Deletions.Single() == (oldCloudUrl, before + 1), "Old Cloudinary image is deleted only after a successful save.");
            if (failCleanup) Check(logger.Levels.Count(x => x == LogLevel.Warning) == warnings + 1,
                "Failed old-image deletion logs a warning without failing the committed activity operation.");
        }

        Reset();
        var legacyFile = Path.Combine(webRoot, "assets", "JoinGroup_Images", "legacy.png");
        File.WriteAllText(legacyFile, "legacy test image");
        context.Seed("https://old-backend.example/assets/JoinGroup_Images/legacy.png");
        context.FailSave = true;
        await Send(method, path + "/100", method == HttpMethod.Put ? Form("event.png") : null, HttpStatusCode.InternalServerError);
        Check(File.Exists(legacyFile), "Failed save retains the old local image.");
        Reset();
        context.Seed("https://old-backend.example/assets/JoinGroup_Images/legacy.png");
        await Send(method, path + "/100", method == HttpMethod.Put ? Form("event.png") : null, HttpStatusCode.OK);
        Check(!File.Exists(legacyFile), "Successful replacement/deletion cleans up a legacy local event image.");
        Check(cloud.Deletions.Count == 0, "Local cleanup never asks Cloudinary to delete the old local URL.");
    }

    var unrelatedFile = Path.Combine(webRoot, "assets", "JoinGroup_Images", "unrelated.png");
    File.WriteAllText(unrelatedFile, "must be retained");
    foreach (var photo in new[] { "https://other.example/unrelated.png", "/uploads/unrelated.png", "/assets/JoinGroup_Images/%2e%2e%2funrelated.png", "/assets/JoinGroup_Images/subdir/unrelated.png" })
    {
        Reset();
        context.Seed(photo);
        await Send(HttpMethod.Delete, path + "/100", null, HttpStatusCode.OK);
        Check(File.Exists(unrelatedFile) && cloud.Deletions.Count == 0, "Unrelated URLs and directory traversal never delete event files.");
    }
    File.Delete(unrelatedFile);

    Reset();
    context.Seed(oldCloudUrl);
    Authenticate(99);
    await Send(HttpMethod.Put, path + "/100", Form("event.png"), HttpStatusCode.Forbidden);
    await Send(HttpMethod.Delete, path + "/100", null, HttpStatusCode.Forbidden);
    Check(cloud.Uploads.Count == 0 && cloud.Deletions.Count == 0, "Only the event owner can replace/delete images.");
    Authenticate(42);

    if (args.Contains("--live-cloudinary"))
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.GetFullPath("prjGoHike/appsettings.json"), optional: true)
            .AddJsonFile(Path.GetFullPath("prjGoHike/appsettings.Development.json"), optional: true)
            .AddUserSecrets("e6952d28-d928-4cae-abb3-2473a043b41d")
            .AddEnvironmentVariables().Build();
        var missing = new[] { "CloudName", "ApiKey", "ApiSecret" }
            .Where(name => string.IsNullOrWhiteSpace(configuration["Cloudinary:" + name])).ToArray();
        if (missing.Length != 0) throw new InvalidOperationException("Missing backend Cloudinary configuration: " + string.Join(", ", missing));
        liveCloud = new CloudinaryService(configuration);
        Reset();
        string? uploaded = null;
        try
        {
            using var form = Form();
            form.Add(new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aA1sAAAAASUVORK5CYII=")), "ActivityPhoto", "cloudinary-smoke.png");
            created = await Send(HttpMethod.Post, path, form, HttpStatusCode.OK);
            uploaded = created!["data"]!["activityPhoto"]!.GetValue<string>();
            Check(uploaded.StartsWith("https://res.cloudinary.com/"), "Live upload returns a Cloudinary HTTPS URL.");
            context.EventData.Single().Mountain = new Mountain { MountainId = 1, MountainName = "測試山岳" };
            var reloaded = await Send(HttpMethod.Get, path, null, HttpStatusCode.OK);
            Check(reloaded!["data"]![0]!["activityPhoto"]!.GetValue<string>() == uploaded, "Live event reload returns the uploaded image.");
            using var imageResponse = await client.GetAsync(uploaded);
            Check(imageResponse.IsSuccessStatusCode && imageResponse.Content.Headers.ContentType?.MediaType?.StartsWith("image/") == true,
                "Live event image can be loaded over HTTPS.");
            await Send(HttpMethod.Delete, path + "/" + created["data"]!["eventId"]!.GetValue<long>(), null, HttpStatusCode.OK);
            Console.WriteLine("Live Cloudinary event upload, reload, HTTPS delivery and deletion completed (activity database is a test double).");
        }
        finally
        {
            if (uploaded != null) await liveCloud.DeleteImageAsync(uploaded);
        }
    }
    Console.WriteLine($"PASS: {checks} event photo API checks.");
}
finally
{
    await app.StopAsync();
    Directory.Delete(webRoot, recursive: true);
}

void Reset() { context.Reset(); cloud.Reset(); }
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    checks++;
}

void Authenticate(long? id)
{
    client.DefaultRequestHeaders.Authorization = id is null ? null : new AuthenticationHeaderValue("Bearer",
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("tests", "tests",
            [new Claim(ClaimTypes.NameIdentifier, id.ToString()!)], expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256))));
}

MultipartFormDataContent Form(string? fileName = null, int size = 1)
{
    var form = new MultipartFormDataContent();
    form.Add(new StringContent("Cloudinary test activity"), "EventName");
    form.Add(new StringContent("1"), "MountainId");
    form.Add(new StringContent("5"), "MaximumNumber");
    form.Add(new StringContent("招募中"), "ActivityStatus");
    form.Add(new StringContent(DateTime.Now.AddDays(3).ToString("s")), "EventStartTime");
    form.Add(new StringContent(DateTime.Now.AddDays(4).ToString("s")), "EventEndTime");
    if (fileName != null) form.Add(new ByteArrayContent(new byte[size]), "ActivityPhoto", fileName);
    return form;
}

async Task<JsonNode?> Send(HttpMethod method, string url, HttpContent? content, HttpStatusCode expected)
{
    using var request = new HttpRequestMessage(method, url) { Content = content };
    using var response = await client.SendAsync(request);
    var body = await response.Content.ReadAsStringAsync();
    Check(response.StatusCode == expected, $"{method} {url}: expected {expected}, got {response.StatusCode}: {body}");
    return string.IsNullOrEmpty(body) ? null : JsonNode.Parse(body);
}
