using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Hubs;
using prjGoHike.Models;


string GroupJoinRoute = "http://localhost:4200";
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
builder.Services.AddControllersWithViews();
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
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseSession();
app.UseCors("GroupJoin");
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<CommentHub>("/commentHub");
app.MapHub<prjGoHike.Hubs.EventHub>("/eventHub");

app.Run();
