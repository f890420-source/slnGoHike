using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//string connectionString = builder.Configuration
//    .GetConnectionString("GoHikeConnection");

string connectionString = builder.Configuration
    .GetConnectionString("GoHikeConnection")
    ?? throw new InvalidOperationException(
        "找不到資料庫連線字串 GoHikeConnection");

builder.Services.AddDbContext<GoHikeData40Context>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddSession();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
