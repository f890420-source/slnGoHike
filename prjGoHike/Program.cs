using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;
//using prjGoHike.Models;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("GoHikeDataContext") ?? throw new InvalidOperationException("Connection string 'GoHikeDataContext' not found.");

builder.Services.AddDbContext<GoHikeDataContext>(options => options.UseSqlServer(connectionString));

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";              // 未登入時重定向到登入頁面
        options.LogoutPath = "/Login/Logout";      // 登出路徑
        options.AccessDeniedPath = "/Login";       // 無權限時重定向
        options.ExpireTimeSpan = TimeSpan.FromHours(8);  // Cookie 預設有效期
        options.SlidingExpiration = true;          // 滑動過期時間（每次請求延長）
    });
builder.Services.AddAuthorization();
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});
builder.Services.AddControllersWithViews();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();