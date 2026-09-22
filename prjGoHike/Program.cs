using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Hubs;
using Microsoft.IdentityModel.Tokens;
using prjGoHike.Models;
using prjGoHike.Services;
using System.Text;

using prjGoHike.Services.forum;

string GroupJoinRoute = "http://localhost:4200";

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("GoHikeDataContext") ?? throw new InvalidOperationException("Connection string 'GoHikeDataContext' not found.");

builder.Services.AddDbContext<GoHikeDataContext>(options => options.UseSqlServer(connectionString));
#region 討論區用的 Service
builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddScoped<SensitiveWordService>();
builder.Services.AddScoped<CommentValidationService>();
builder.Services.AddScoped<NotificationRealtimeService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<GoogleAuthSettings>(
    builder.Configuration.GetSection(GoogleAuthSettings.SectionName));

builder.Services.AddHttpClient<GeminiModerationService>();

#endregion
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings 設定遺失。");

if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey) ||
    string.IsNullOrWhiteSpace(jwtSettings.Issuer) ||
    string.IsNullOrWhiteSpace(jwtSettings.Audience))
{
    throw new InvalidOperationException("JwtSettings 的 SecretKey、Issuer 、 Audience 都必填。");
}

if (Encoding.UTF8.GetByteCount(jwtSettings.SecretKey) < 32)
{
    throw new InvalidOperationException(
        "JwtSettings:SecretKey 至少需要 32 bytes，用 User Secrets 或部署環境變數設定安全的隨機金鑰。");
}

// Add services to the container.

// OpenAPI uses HTTP JSON options; match the existing MVC GeoJSON converter.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(
        new NetTopologySuite.IO.Converters.GeoJsonConverterFactory()));
builder.Services.AddOpenApi("v1", options =>
{
    options.ShouldInclude = description =>
        description.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true;
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "GoHike API";
        document.Info.Version = "v1";
        return Task.CompletedTask;
    });
    options.AddSchemaTransformer<GeoJsonSchemaTransformer>();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDevelopment", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new NetTopologySuite.IO.Converters.GeoJsonConverterFactory());
    });
builder.Services.AddSignalR();
builder.Services.AddSession();
builder.Services.AddSignalR();
builder.Services.AddCors(option =>
{
    option.AddPolicy("GroupJoin", policy => {
        policy.WithOrigins(GroupJoinRoute)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("../openapi/v1.json", "GoHike API v1");
    });
}
app.UseRouting();
app.UseCors("AngularDevelopment");
app.UseStaticFiles();

app.UseAuthentication();
app.UseSession();
app.UseCors("GroupJoin");
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<CommentHub>("/commentHub");
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<prjGoHike.Hubs.EventHub>("/eventHub");

app.Run();
