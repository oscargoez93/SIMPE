namespace SIMPE.Agent.Models
{
    public class PerformanceMetrics
    {
        public string generatedAt { get; set; } = "";
        public CpuMetrics cpu { get; set; } = new();
        public MemoryMetrics memory { get; set; } = new();
        public List<DiskMetrics> disks { get; set; } = new();
        public List<NetworkMetrics> networks { get; set; } = new();
    }

    public class CpuMetrics
    {
        public string name { get; set; } = "";
        public int usagePercent { get; set; }
        public int coreCount { get; set; }
        public string clockSpeed { get; set; } = "";
        public string uptime { get; set; } = "";
        public int processCount { get; set; }
        public int threadCount { get; set; }
    }

    public class MemoryMetrics
    {
        public double totalGB { get; set; }
        public double usedGB { get; set; }
        public double freeGB { get; set; }
        public int usagePercent { get; set; }
        public double cachedGB { get; set; }
        public double availableGB { get; set; }
    }

    public class DiskMetrics
    {
        public string drive { get; set; } = "";
        public string label { get; set; } = "";
        public string fileSystem { get; set; } = "";
        public double totalGB { get; set; }
        public double usedGB { get; set; }
        public double freeGB { get; set; }
        public int usagePercent { get; set; }
        public double readSpeedMB { get; set; }
        public double writeSpeedMB { get; set; }
        public double queueLength { get; set; }
    }

    public class NetworkMetrics
    {
        public string name { get; set; } = "";
        public string macAddress { get; set; } = "";
        public string ipAddress { get; set; } = "";
        public long bytesReceived { get; set; }
        public long bytesSent { get; set; }
        public double receiveSpeedMbps { get; set; }
        public double sendSpeedMbps { get; set; }
        public string status { get; set; } = "";
        public int linkSpeedMbps { get; set; }
    }
}
