namespace BacklashDTOs.Configuration
{
    public class SimulatorConfig
    {
        public required string CacheDirectory { get; set; }
        public int ProcessingTimeoutSeconds { get; set; } = 300; // 5 minutes default
    }
}