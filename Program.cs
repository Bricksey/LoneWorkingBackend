using System.Diagnostics;
using LoneWorkingBackend.Models;
using LoneWorkingBackend.Services;


// Ensure the DB directory exists to allow MongoDB to run
Console.WriteLine("Creating DB Directory if necessary");
System.IO.Directory.CreateDirectory("./db/");

// Set parameters to start MongoDB silently
Console.WriteLine("Attempring to start MongoDB");
ProcessStartInfo mongodConfig = new ProcessStartInfo("mongod", "--dbpath ./db/");
mongodConfig.UseShellExecute = false;
mongodConfig.CreateNoWindow = true;
mongodConfig.RedirectStandardOutput = true;

// Run MongoDB with these parameters.
Process mongod = new Process();
mongod.StartInfo = mongodConfig;
mongod.Start();


// Add services to app with dependency injection
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.Configure<LoneWorkingDatabaseSettings>(
    builder.Configuration.GetSection("LoneWorkingDatabase")
);
builder.Services.AddSingleton<AccountsService>();
var app = builder.Build();

// Map root to simple page for debug
app.MapGet("/", () => "Hello World!");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();