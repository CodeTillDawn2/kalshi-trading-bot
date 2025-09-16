namespace BacklashOverseer.Config
{
    public class SubscriptionManagerConfig
    {
        public bool EnableMetrics { get; set; } = true;
        public int SubscriptionTimeoutMs { get; set; } = 60000;
        public int ConfirmationTimeoutSeconds { get; set; } = 60;
        public int RetryDelayMs { get; set; } = 1000;
        public int MaxQueueSize { get; set; } = 1000;
        public int BatchSize { get; set; } = 10;
        public int HealthCheckIntervalMs { get; set; } = 30000;
    }
}