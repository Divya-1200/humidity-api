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
    private readonly string apiUrl = "https://www.randomnumberapi.com/api/v1.0/random?min=40&max=100&count=30"; 
    private readonly HttpClient httpClient;
    //private readonly HumidityDb _dbContext;
    //private readonly HumidityDb _dbContext;
    private readonly IServiceProvider _serviceProvider;

    public ApiCallerService(HttpClient httpClient,  IServiceProvider serviceProvider)
    {
         this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        //_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));


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

                            if (humidityValue > 70)
                            {
                                Console.WriteLine($" Humidity is {humidityValue} greater than threshold");
                            }

                            await _dbContext.HumidityDatas.AddAsync(apiResponseData);
                        }
                        await _dbContext.SaveChangesAsync();
                    }
                }
                else
                {

                    Console.WriteLine("Error Found");


                }

            }
            else
            {
                Console.WriteLine($"API call failed. Status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex}");
        }
    }
}
