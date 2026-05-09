namespace SIMPE.Agent.Models
{
    public class SecurityScanResult
    {
        public string generatedAt { get; set; } = "";
        public string overallStatus { get; set; } = "unknown";
        public List<SecurityScanItem> items { get; set; } = new();
    }

    public class SecurityScanItem
    {
        public string id { get; set; } = "";
        public string title { get; set; } = "";
        public string status { get; set; } = "unknown";
        public string summary { get; set; } = "";
        public List<SecurityScanDetail> details { get; set; } = new();
    }

    public class SecurityScanDetail
    {
        public string label { get; set; } = "";
        public string value { get; set; } = "";
    }
}
