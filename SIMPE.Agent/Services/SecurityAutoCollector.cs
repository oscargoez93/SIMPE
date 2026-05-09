using System.Text.Json;

namespace SIMPE.Agent.Services
{
    public class SecurityAutoCollector : BackgroundService
    {
        private readonly ILogger<SecurityAutoCollector> _logger;
        private readonly DatabaseService _dbService;
        private readonly SecurityCollectorService _collector;

        public SecurityAutoCollector(
            ILogger<SecurityAutoCollector> logger, 
            DatabaseService dbService,
            SecurityCollectorService collector)
        {
            _logger = logger;
            _dbService = dbService;
            _collector = collector;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Security Auto Collector is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    string idEquipo = Environment.MachineName;

                    var scan = _collector.GatherSecurityInfo();
                    
                    // Solo guardamos un evento general resumiendo el estado para no llenar la BD
                    await _dbService.InsertEventoSeguridadAsync(
                        idEquipo, 
                        "SecurityScan", 
                        $"Status: {scan.overallStatus}", 
                        JsonSerializer.Serialize(scan.items));

                    _logger.LogInformation($"Security data collected and saved. Status: {scan.overallStatus}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error gathering security info");
                }

                // Run every 60 minutes
                await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
            }
        }
    }
}
