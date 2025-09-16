namespace BacklashOverseer.Config
{
    public class PerformanceConfig
    {
        public SnapshotAggregationConfig SnapshotAggregation { get; set; } = new();

        public class SnapshotAggregationConfig
        {
            public bool EnableMetrics { get; set; } = true;
        }
    }
}