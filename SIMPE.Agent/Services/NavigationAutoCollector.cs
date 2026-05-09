using System.Text.Json;

namespace SIMPE.Agent.Services
{
    public class NavigationAutoCollector : BackgroundService
    {
        private readonly ILogger<NavigationAutoCollector> _logger;
        private readonly DatabaseService _dbService;
        private readonly NavigationHistoryCollectorService _collector;

        public NavigationAutoCollector(
            ILogger<NavigationAutoCollector> logger, 
            DatabaseService dbService,
            NavigationHistoryCollectorService collector)
        {
            _logger = logger;
            _dbService = dbService;
            _collector = collector;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Navigation Auto Collector is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    string idEquipo = Environment.MachineName; // O la lógica que usen para ID

                    // Obtener las últimas 500 navegaciones
                    var history = _collector.GatherNavigationHistory(500);

                    foreach (var entry in history.entries)
                    {
                        await _dbService.InsertHistorialNavegacionAsync(idEquipo, entry);
                    }

                    _logger.LogInformation($"Navigation data collected and saved. Entries: {history.entries.Count}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error gathering navigation info");
                }

                // Run every 10 minutes
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
