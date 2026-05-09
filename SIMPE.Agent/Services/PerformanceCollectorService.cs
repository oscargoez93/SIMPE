using System.Diagnostics;
using System.Management;
using System.Text;
using System.Text.Json;
using SIMPE.Agent.Models;

namespace SIMPE.Agent.Services
{
    public class PerformanceCollectorService
    {
        public PerformanceMetrics GatherPerformanceMetrics()
        {
            return new PerformanceMetrics
            {
                generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                cpu = GatherCpuMetrics(),
                memory = GatherMemoryMetrics(),
                disks = GatherDiskMetrics(),
                networks = GatherNetworkMetrics()
            };
        }

        private CpuMetrics GatherCpuMetrics()
        {
            var metrics = new CpuMetrics();

            // Get static CPU info from WMI
            try
            {
                using var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT Name, NumberOfCores, MaxClockSpeed FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    metrics.name = (obj["Name"]?.ToString() ?? "Desconocido").Trim();
                    metrics.coreCount = int.TryParse(obj["NumberOfCores"]?.ToString(), out var cores) ? cores : 0;
                    var speedMhz = int.TryParse(obj["MaxClockSpeed"]?.ToString(), out var mhz) ? mhz : 0;
                    metrics.clockSpeed = $"{speedMhz} MHz";
                }
            }
            catch { }

            // Get CPU usage via PowerShell Get-Counter (needs two samples, so we request it with a small interval)
            var cpuCounter = GetPowerShellOutput("(Get-Counter '\u005c\u005cProcessor(_Total)\u005c\u005c% Processor Time' -SampleInterval 1 -MaxSamples 1).CounterSamples.CookedValue");
            if (double.TryParse(cpuCounter, out var cpuValue))
            {
                metrics.usagePercent = (int)Math.Round(Math.Min(cpuValue, 100));
            }

            // Fallback to WMI LoadPercentage if PowerShell returns 0
            if (metrics.usagePercent == 0)
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT LoadPercentage FROM Win32_Processor");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (int.TryParse(obj["LoadPercentage"]?.ToString(), out var load))
                        {
                            metrics.usagePercent = load;
                            break;
                        }
                    }
                }
                catch { }
            }

            // Get process and thread counts
            try
            {
                metrics.processCount = Process.GetProcesses().Length;
                metrics.threadCount = Process.GetProcesses().Select(p => { try { return p.Threads.Count; } catch { return 0; } }).Sum();
            }
            catch { }

            // Get system uptime
            try
            {
                var uptimeTicks = Environment.TickCount64;
                var uptimeSpan = TimeSpan.FromMilliseconds(uptimeTicks);
                metrics.uptime = $"{uptimeSpan.Days}d {uptimeSpan.Hours}h {uptimeSpan.Minutes}m";
            }
            catch { }

            return metrics;
        }

        private MemoryMetrics GatherMemoryMetrics()
        {
            var metrics = new MemoryMetrics();

            try
            {
                using var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var totalKb = double.TryParse(obj["TotalVisibleMemorySize"]?.ToString(), out var t) ? t : 0;
                    var freeKb = double.TryParse(obj["FreePhysicalMemory"]?.ToString(), out var f) ? f : 0;

                    metrics.totalGB = Math.Round(totalKb / 1024 / 1024, 2);
                    metrics.freeGB = Math.Round(freeKb / 1024 / 1024, 2);
                    metrics.usedGB = Math.Round(metrics.totalGB - metrics.freeGB, 2);
                    metrics.usagePercent = metrics.totalGB > 0 ? (int)Math.Round((metrics.usedGB / metrics.totalGB) * 100) : 0;
                }
            }
            catch { }

            // Get available memory via WMI for more accurate reading
            try
            {
                using var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT AvailableBytes FROM Win32_PerfRawData_PerfOS_Memory");
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (double.TryParse(obj["AvailableBytes"]?.ToString(), out var availBytes))
                    {
                        metrics.availableGB = Math.Round(availBytes / 1024 / 1024 / 1024, 2);
                    }
                }
            }
            catch { }

            // Get cached memory
            try
            {
                using var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT CacheBytes FROM Win32_PerfRawData_PerfOS_Memory");
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (double.TryParse(obj["CacheBytes"]?.ToString(), out var cached))
                    {
                        metrics.cachedGB = Math.Round(cached / 1024 / 1024 / 1024, 2);
                    }
                }
            }
            catch { }

            return metrics;
        }

        private List<DiskMetrics> GatherDiskMetrics()
        {
            var metrics = new List<DiskMetrics>();

            try
            {
                // Get logical disk space info
                using var searcher = new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT DeviceID, VolumeName, FileSystem, Size, FreeSpace FROM Win32_LogicalDisk WHERE DriveType=3");

                foreach (ManagementObject obj in searcher.Get())
                {
                    var drive = obj["DeviceID"]?.ToString() ?? "";
                    var size = double.TryParse(obj["Size"]?.ToString(), out var s) ? s : 0;
                    var free = double.TryParse(obj["FreeSpace"]?.ToString(), out var f) ? f : 0;

                    var disk = new DiskMetrics
                    {
                        drive = drive,
                        label = obj["VolumeName"]?.ToString() ?? "",
                        fileSystem = obj["FileSystem"]?.ToString() ?? "",
                        totalGB = Math.Round(size / 1024 / 1024 / 1024, 2),
                        freeGB = Math.Round(free / 1024 / 1024 / 1024, 2),
                        usedGB = Math.Round((size - free) / 1024 / 1024 / 1024, 2),
                        usagePercent = size > 0 ? (int)Math.Round(((size - free) / size) * 100) : 0
                    };

                    // Get disk performance via WMI with rate calculation (two samples)
                    var stats = GetDiskStats(drive);
                    disk.queueLength = stats.queueLength;
                    disk.readSpeedMB = stats.readSpeedMB;
                    disk.writeSpeedMB = stats.writeSpeedMB;

                    metrics.Add(disk);
                }
            }
            catch { }

            return metrics;
        }

        private (uint queueLength, double readSpeedMB, double writeSpeedMB) GetDiskStats(string drive)
        {
            uint queueLength = 0;
            double readSpeedMB = 0, writeSpeedMB = 0;

            try
            {
                // First sample
                ulong rb1 = 0, wb1 = 0;
                uint ql1 = 0;
                using (var searcher = new ManagementObjectSearcher("root\\CIMV2",
                    $"SELECT CurrentDiskQueueLength, DiskReadBytesPersec, DiskWriteBytesPersec FROM Win32_PerfRawData_PerfDisk_LogicalDisk WHERE Name='{drive}'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        uint.TryParse(obj["CurrentDiskQueueLength"]?.ToString(), out ql1);
                        ulong.TryParse(obj["DiskReadBytesPersec"]?.ToString(), out rb1);
                        ulong.TryParse(obj["DiskWriteBytesPersec"]?.ToString(), out wb1);
                    }
                }

                Thread.Sleep(800);

                // Second sample
                ulong rb2 = 0, wb2 = 0;
                uint ql2 = 0;
                using (var searcher = new ManagementObjectSearcher("root\\CIMV2",
                    $"SELECT CurrentDiskQueueLength, DiskReadBytesPersec, DiskWriteBytesPersec FROM Win32_PerfRawData_PerfDisk_LogicalDisk WHERE Name='{drive}'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        uint.TryParse(obj["CurrentDiskQueueLength"]?.ToString(), out ql2);
                        ulong.TryParse(obj["DiskReadBytesPersec"]?.ToString(), out rb2);
                        ulong.TryParse(obj["DiskWriteBytesPersec"]?.ToString(), out wb2);
                    }
                }

                queueLength = ql2;

                // Calculate speed (difference over ~0.8 seconds)
                if (rb2 >= rb1)
                    readSpeedMB = Math.Round((rb2 - rb1) / 1024.0 / 1024.0 / 0.8, 2);
                if (wb2 >= wb1)
                    writeSpeedMB = Math.Round((wb2 - wb1) / 1024.0 / 1024.0 / 0.8, 2);
            }
            catch { }

            return (queueLength, readSpeedMB, writeSpeedMB);
        }

        private List<NetworkMetrics> GatherNetworkMetrics()
        {
            var metrics = new List<NetworkMetrics>();

            try
            {
                // Get active network adapters
                using var searcher = new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT Name, MACAddress, NetConnectionID, Speed, NetEnabled FROM Win32_NetworkAdapter WHERE NetConnectionStatus=2");

                foreach (ManagementObject obj in searcher.Get())
                {
                    var name = obj["Name"]?.ToString() ?? "";
                    var netConnId = obj["NetConnectionID"]?.ToString() ?? name;
                    var mac = obj["MACAddress"]?.ToString() ?? "";
                    var speedRaw = obj["Speed"]?.ToString() ?? "0";
                    var enabled = obj["NetEnabled"]?.ToString() ?? "True";

                    var net = new NetworkMetrics
                    {
                        name = netConnId,
                        macAddress = mac,
                        status = enabled.Equals("True", StringComparison.OrdinalIgnoreCase) ? "Conectado" : "Desconectado",
                        linkSpeedMbps = long.TryParse(speedRaw, out var spd) ? (int)(spd / 1000000) : 0
                    };

                    // Get IP address for this adapter
                    try
                    {
                        using var ipSearcher = new ManagementObjectSearcher("root\\CIMV2",
                            $"SELECT IPAddress FROM Win32_NetworkAdapterConfiguration WHERE MACAddress='{mac}' AND IPEnabled=True");
                        foreach (ManagementObject ipObj in ipSearcher.Get())
                        {
                            var ips = ipObj["IPAddress"] as string[];
                            if (ips != null && ips.Length > 0)
                            {
                                net.ipAddress = ips[0];
                            }
                        }
                    }
                    catch { }

                    // Get network stats via WMI with rate calculation (two samples)
                    // Use hardware name (not NetConnectionID) for WMI perf counter lookup
                    var stats = GetNetworkStats(name);
                    net.bytesReceived = stats.bytesReceived;
                    net.bytesSent = stats.bytesSent;
                    net.receiveSpeedMbps = stats.receiveSpeedMbps;
                    net.sendSpeedMbps = stats.sendSpeedMbps;

                    metrics.Add(net);
                }
            }
            catch { }

            return metrics;
        }

        private (long bytesReceived, long bytesSent, double receiveSpeedMbps, double sendSpeedMbps) GetNetworkStats(string netConnId)
        {
            long bytesReceived = 0, bytesSent = 0;
            double receiveSpeedMbps = 0, sendSpeedMbps = 0;

            try
            {
                // First sample
                ulong rx1 = 0, tx1 = 0;
                using (var searcher = new ManagementObjectSearcher("root\\CIMV2",
                    $"SELECT BytesReceivedPersec, BytesSentPersec FROM Win32_PerfRawData_Tcpip_NetworkInterface WHERE Name LIKE '%{netConnId.Replace("[", "[[").Replace("]", "]]")}%'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        ulong.TryParse(obj["BytesReceivedPersec"]?.ToString(), out rx1);
                        ulong.TryParse(obj["BytesSentPersec"]?.ToString(), out tx1);
                    }
                }

                Thread.Sleep(800);

                // Second sample
                ulong rx2 = 0, tx2 = 0;
                using (var searcher = new ManagementObjectSearcher("root\\CIMV2",
                    $"SELECT BytesReceivedPersec, BytesSentPersec FROM Win32_PerfRawData_Tcpip_NetworkInterface WHERE Name LIKE '%{netConnId.Replace("[", "[[").Replace("]", "]]")}%'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        ulong.TryParse(obj["BytesReceivedPersec"]?.ToString(), out rx2);
                        ulong.TryParse(obj["BytesSentPersec"]?.ToString(), out tx2);
                    }
                }

                bytesReceived = (long)rx2;
                bytesSent = (long)tx2;

                // Calculate speed (difference over ~0.8 seconds)
                if (rx2 >= rx1)
                    receiveSpeedMbps = Math.Round((rx2 - rx1) * 8.0 / 1000000.0 / 0.8, 2);
                if (tx2 >= tx1)
                    sendSpeedMbps = Math.Round((tx2 - tx1) * 8.0 / 1000000.0 / 0.8, 2);
            }
            catch { }

            return (bytesReceived, bytesSent, receiveSpeedMbps, sendSpeedMbps);
        }

        private string GetPowerShellOutput(string command)
        {
            if (!OperatingSystem.IsWindows())
                return "";

            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                process.Start();
                if (!process.WaitForExit(15000))
                {
                    process.Kill(true);
                    return "";
                }

                return process.StandardOutput.ReadToEnd().Trim();
            }
            catch
            {
                return "";
            }
        }

        private string GetPowerShellJson(string command)
        {
            return GetPowerShellOutput(command);
        }
    }
}
