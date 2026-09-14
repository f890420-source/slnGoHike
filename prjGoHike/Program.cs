using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Hubs;
using prjGoHike.Models;
using prjGoHike.Services;

using prjGoHike.Services.forum;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("GoHikeDataContext") ?? throw new InvalidOperationException("Connection string 'GoHikeDataContext' not found.");

builder.Services.AddDbContext<GoHikeDataContext>(options => options.UseSqlServer(connectionString));
#region 討論區用的 Service
builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddScoped<SensitiveWordService>();
builder.Services.AddScoped<CommentValidationService>();

builder.Services.AddHttpClient<GeminiModerationService>();
#endregion
// Add services to the container.

// OpenAPI uses HTTP JSON options; match the existing MVC GeoJSON converter.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(
        new NetTopologySuite.IO.Converters.GeoJsonConverterFactory()));
builder.Services.AddOpenApi("v1", options =>
{
    options.ShouldInclude = description =>
        description.RelativePath?.StartsWith("api/v1/", StringComparison.OrdinalIgnoreCase) == true;
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

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";              // 未登入時重定向到登入頁面
        options.LogoutPath = "/Login/Logout";      // 登出路徑
        options.AccessDeniedPath = "/Login";       // 無權限時重定向
        options.ExpireTimeSpan = TimeSpan.FromHours(8);  // Cookie 預設有效期
        options.SlidingExpiration = true;          // 滑動過期時間（每次請求延長）

        if (builder.Environment.IsDevelopment())
        {
            options.Cookie.SameSite =
                SameSiteMode.None;

            options.Cookie.SecurePolicy =
                CookieSecurePolicy.Always;
        }
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});
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
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<CommentHub>("/commentHub");
app.Run();
