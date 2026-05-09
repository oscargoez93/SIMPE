namespace SIMPE.Agent.Models
{
    public class NavigationHistoryResult
    {
        public string generatedAt { get; set; } = "";
        public int totalEntries { get; set; }
        public int returnedEntries { get; set; }
        public List<string> scannedBrowsers { get; set; } = new();
        public List<NavigationHistoryEntry> entries { get; set; } = new();
        public List<string> notes { get; set; } = new();
    }

    public class NavigationHistoryEntry
    {
        public string browser { get; set; } = "";
        public string profile { get; set; } = "";
        public string title { get; set; } = "";
        public string url { get; set; } = "";
        public string visitedAt { get; set; } = "";
        public string mode { get; set; } = "Normal";
        public string duration { get; set; } = "No disponible";
        public bool durationEstimated { get; set; }
    }
}
