using System.Diagnostics;
using LoneWorkingBackend.Models;
using LoneWorkingBackend.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

// Add services to app with dependency injection
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => 
    {
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.AccessDeniedPath = "/Forbidden";
    });

var cookiePolicyOptions = new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Strict,
};

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.Configure<LoneWorkingDatabaseSettings>(
    builder.Configuration.GetSection("LoneWorkingDatabase")
);

builder.Services.AddHostedService<AccountsWorker>();
builder.Services.AddSingleton<AccountsService>();
builder.Services.AddSingleton<SensorService>();
var app = builder.Build();

// Map root to simple page for debug
app.MapGet("/", () => "Hello World!");

app.UseCookiePolicy(cookiePolicyOptions);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();