using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Humidity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class ApiCallerService : BackgroundService
{
    // Random external Api to generate data
    private readonly string apiUrl = "https://www.randomnumberapi.com/api/v1.0/random?min=40&max=100&count=30"; 
    private readonly HttpClient httpClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApiCallerService> _logger; // Add this field

    public ApiCallerService(HttpClient httpClient,  IServiceProvider serviceProvider, ILogger<ApiCallerService> logger)
    {
         this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        // Service provider scope to access the DB inside the scheduler
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Initialize logger



    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<HumidityDb>();
                await CallExternalApi(dbContext);
            } 
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); // Delay for 30 minutes
        }
    }

    private async Task CallExternalApi(HumidityDb _dbContext)
    {
        try
        {
            // Make the HTTP request to the external API
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
           
            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"inside here here");

                string responseData = await response.Content.ReadAsStringAsync();
                //HumidityData dataObject = JsonSerializer.Deserialize<HumidityData>(responseData);
                
                if (_dbContext != null)
                {
                    if (JsonSerializer.Deserialize<int[]>(responseData) is int[] humidityArray && humidityArray.Length > 0)
                    {
                        for (int i = 0; i < humidityArray.Length; i++)
                        {
                            int humidityValue = humidityArray[i];
                            
                            var apiResponseData = new HumidityData
                            {
                                sensorId = i + 1, 
                                humidity = humidityValue,
                                dateTime = DateTime.Now
                            };
                            // Alert Notification if humidity is higher than 75
                            if (humidityValue > 75)
                            {
                                _logger.LogWarning($" Humidity is {humidityValue} greater than threshold");
                            }

                            await _dbContext.HumidityDatas.AddAsync(apiResponseData);
                        }
                        await _dbContext.SaveChangesAsync();
                    }
                }
                else
                {

                    _logger.LogWarning("Error Found");


                }

            }
            else
            {
                _logger.LogWarning($"API call failed. Status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"An error occurred: {ex}");
        }
    }
}
