using System.Text.Json;

namespace SIMPE.Agent.Services
{
    public class DataRetentionService : BackgroundService
    {
        private readonly ILogger<DataRetentionService> _logger;
        private readonly DatabaseService _dbService;

        public DataRetentionService(ILogger<DataRetentionService> logger, DatabaseService dbService)
        {
            _logger = logger;
            _dbService = dbService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Data Retention Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Executing Data Retention Policy (120 days)...");
                    await _dbService.DeleteOldDataAsync(120);
                    _logger.LogInformation("Data Retention Policy executed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing Data Retention Policy.");
                }

                // Run once every 24 hours
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
