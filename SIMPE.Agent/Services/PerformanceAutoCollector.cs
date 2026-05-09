using System.Text.Json;

namespace SIMPE.Agent.Services
{
    public class PerformanceAutoCollector : BackgroundService
    {
        private readonly ILogger<PerformanceAutoCollector> _logger;
        private readonly DatabaseService _dbService;
        private readonly PerformanceCollectorService _collector;

        public PerformanceAutoCollector(
            ILogger<PerformanceAutoCollector> logger, 
            DatabaseService dbService,
            PerformanceCollectorService collector)
        {
            _logger = logger;
            _dbService = dbService;
            _collector = collector;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Performance Auto Collector is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    string idEquipo = Environment.MachineName;

                    var metrics = _collector.GatherPerformanceMetrics();
                    await _dbService.InsertMetricasRendimientoAsync(idEquipo, metrics);

                    _logger.LogInformation("Performance data collected and saved.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error gathering performance info");
                }

                // Run every 2 minutes
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }
    }
}
