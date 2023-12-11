using System.Text.Json;
using App.Metrics;
using Humidity;
using App.Metrics.Counter;
using humidity_api_minimal;


public class ApiCallerService : BackgroundService
{
    // Random external Api to generate data
    private readonly string apiUrl = "https://www.randomnumberapi.com/api/v1.0/random?min=40&max=80&count=30"; 
    private readonly HttpClient httpClient;
    private readonly IServiceProvider _serviceProvider; // scoped service to access the database
    private readonly ILogger<ApiCallerService> _logger; //logger service
    private readonly IMetrics _metrics; // add track the metrics

    public ApiCallerService(HttpClient httpClient,  IServiceProvider serviceProvider, ILogger<ApiCallerService> logger, IMetrics metrics)
    {
         this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));    
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)); // Service provider scope to access the DB inside the scheduler
        _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Initialize logger
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics)); // Initialize metrics
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
                
                if (_dbContext != null) // Check db is present before writing it to the db
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
                            // Metrics Implementation
                            var apiCallsCounterOptions = new CounterOptions
                            {
                                Name = MetricsRegistry.ApiCallsMetric,
                                MeasurementUnit = Unit.Calls
                                
                            };
                            // Alert Notification if humidity is higher than 75
                            if (humidityValue > 75)
                            {
                                _metrics.Measure.Counter.Increment(apiCallsCounterOptions);

                                _logger.LogWarning($" Humidity is {humidityValue} greater than threshold");
                            }

                            await _dbContext.HumidityDatas.AddAsync(apiResponseData);
                        }
                        await _dbContext.SaveChangesAsync(); // Save the 30 sensor data to the database
                    }
                }
                else
                {

                    _logger.LogError("Error Found while accessing the DB");


                }

            }
            else
            {
                _logger.LogError($"API call failed. Status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"An error occurred: {ex}");
        }
    }
}
