using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;
//using prjGoHike.Models;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("GoHikeData40Context") ?? throw new InvalidOperationException("Connection string 'GoHikeData40Context' not found.");

//builder.Services.AddDbContext<GoHikeDataContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContext<GoHikeDataContext>(options =>
    options.UseSqlServer(connectionString, x => x.UseNetTopologySuite()));

//builder.Services.AddDbContext<GoHikeData40Context>(options => options.UseSqlServer(connectionString));
// Add services to the container.
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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
