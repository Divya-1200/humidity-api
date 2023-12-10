using Microsoft.EntityFrameworkCore;
using Humidity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AspNetCoreRateLimit;
using Microsoft.Extensions.Configuration;
using Serilog;


var builder = WebApplication.CreateBuilder(args);


// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day) // Log to a text file
    .CreateLogger();

builder.Host.UseSerilog(); // Use Serilog as the logger


var connectionString = builder.Configuration.GetConnectionString("Humidity") ?? "Data Source=Humidity.db";

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSqlite<HumidityDb>(connectionString);
builder.Services.AddDbContext<HumidityDb>(options => options.UseSqlite(connectionString));

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();




builder.Services.AddHttpClient<ApiCallerService>(); // Register HttpClient
builder.Services.AddHostedService<ApiCallerService>(); // Register the background service


//  Rate limiting Configuration
builder.Services.AddMemoryCache();
builder.Services.AddOptions();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();

builder.Services.AddMetrics();
builder.Services.AddMetricsTrackingMiddleware();
builder.Services.AddMetricsEndpoints();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMetricsEndpoint();
app.UseHttpsRedirection();
// Apply rate limiting middleware
app.UseIpRateLimiting();


app.MapGet("/humidity", async (HumidityDb db) =>
{
    try {
        var humidityData = await db.HumidityDatas.ToListAsync();

        if (humidityData.Count == 0)
        {
            return Results.NotFound("No humidity data found.");
        }

        return Results.Ok(humidityData);
    }
    catch (Exception ex){
        Log.Error($"An error occurred: {ex}");
        return Results.NotFound("No humidity data table found.");

    }


});

app.Run();


