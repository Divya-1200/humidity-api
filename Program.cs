using Microsoft.EntityFrameworkCore;
using Humidity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Humidity") ?? "Data Source=Humidity.db";

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSqlite<HumidityDb>(connectionString);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Inside ConfigureServices method in Startup.cs
builder.Services.AddHttpClient<ApiCallerService>(); // Register HttpClient
builder.Services.AddHostedService<ApiCallerService>(); // Register the background service


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.MapGet("/humidity", async (HumidityDb db) => await db.HumidityDatas.ToListAsync());


app.Run();


