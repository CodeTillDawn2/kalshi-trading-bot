namespace BacklashDTOs.Configuration
{
    public class ExecutionConfig
    {
        public int MarketUpdateTimeout { get; set; }
        public bool LaunchDataDashboard { get; set; }
        public string? BrainInstance { get; set; }
        public bool RunOvernightActivities { get; set; }
        public int MaxMarketsPerSubscriptionAction { get; set; }
        public required string HardDataStorageLocation { get; set; }
        public int QueuesTargetCount { get; set; }
        public double QueuesTargetPercentage { get; set; }

    }
}