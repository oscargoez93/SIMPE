using System;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIMPE.Agent.Models;
using System.Text.Json;

namespace SIMPE.Agent.Services
{
    public class HardwareCollectorService : BackgroundService
    {
        private readonly ILogger<HardwareCollectorService> _logger;
        private readonly DatabaseService _dbService;

        public HardwareCollectorService(ILogger<HardwareCollectorService> logger, DatabaseService dbService)
        {
            _logger = logger;
            _dbService = dbService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Hardware Collector Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var pcInfo = GatherHardwareInfo();
                    await _dbService.UpsertEquipoAsync(pcInfo);
                    _logger.LogInformation($"Hardware data synced for {pcInfo.id_equipo} at {DateTime.Now}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error gathering hardware info");
                }

                // Run every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private Equipo GatherHardwareInfo()
        {
            var equipo = new Equipo
            {
                id_equipo = GetMacAddress() ?? Environment.MachineName,
                nombre = Environment.MachineName,
                usuario = Environment.UserName,
                ip = GetLocalIPAddress(),
                cpu_model = GetWmiProperty("Win32_Processor", "Name"),
                ram_total = GetTotalRamGb(),
                disco_tipo = GetMainDriveInfo(),
                os_version = Environment.OSVersion.VersionString,
                antivirus_nombre = GetAntivirusName(),
                tiempo_arranque = GetBootTime(),
                tiempo_apagado = "",
                estado_seguridad = "OK",
                ultima_actualizacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                synced = 0
            };

            var hardwareDetails = new
            {
                Gpu = GetWmiProperty("Win32_VideoController", "Name"),
                Architecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit",
                LogicalProcessors = Environment.ProcessorCount
            };
            equipo.hardware_detalles = JsonSerializer.Serialize(hardwareDetails);

            return equipo;
        }

        private string GetWmiProperty(string wmiClass, string propertyName)
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {wmiClass}");
                    foreach (var obj in searcher.Get())
                    {
                        return obj[propertyName]?.ToString() ?? "Unknown";
                    }
                }
                catch { }
            }
            return "N/A";
        }

        private double GetTotalRamGb()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                    foreach (var obj in searcher.Get())
                    {
                        if (long.TryParse(obj["TotalPhysicalMemory"]?.ToString(), out long bytes))
                        {
                            return Math.Round(bytes / (1024.0 * 1024.0 * 1024.0), 2);
                        }
                    }
                }
                catch { }
            }
            return 0;
        }

        private string GetMacAddress()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT MACAddress FROM Win32_NetworkAdapter WHERE NetConnectionStatus = 2");
                    foreach (var obj in searcher.Get())
                    {
                        return obj["MACAddress"]?.ToString();
                    }
                }
                catch { }
            }
            return null;
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "Unknown";
        }

        private string GetMainDriveInfo()
        {
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name.StartsWith("C"));
            if (drive != null)
            {
                double totalSpace = Math.Round(drive.TotalSize / (1024.0 * 1024.0 * 1024.0), 2);
                return $"C: {drive.DriveFormat} ({totalSpace} GB)";
            }
            return "N/A";
        }

        private string GetBootTime()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem");
                    foreach (var obj in searcher.Get())
                    {
                        var lastBootTimeStr = obj["LastBootUpTime"]?.ToString();
                        if (!string.IsNullOrEmpty(lastBootTimeStr))
                        {
                            var dt = ManagementDateTimeConverter.ToDateTime(lastBootTimeStr);
                            return dt.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }
                }
                catch { }
            }
            return "Unknown";
        }

        private string GetAntivirusName()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    // For modern Windows versions (Windows 8 and above)
                    using var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT displayName FROM AntiVirusProduct");
                    foreach (var obj in searcher.Get())
                    {
                        return obj["displayName"]?.ToString() ?? "Windows Defender";
                    }
                }
                catch { }
            }
            return "Windows Defender / None";
        }
    }
}
