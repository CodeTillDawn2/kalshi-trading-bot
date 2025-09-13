namespace BacklashDTOs.Data
{
    public class BrainInstanceDTO
    {
        public string? BrainInstanceName { get; set; }
        public Guid? BrainLock { get; set; }
        public bool WatchPositions { get; set; }
        public bool WatchOrders { get; set; }
        public bool ManagedWatchList { get; set; }
        public bool CaptureSnapshots { get; set; }
        public int TargetWatches { get; set; }
        public double MinimumInterest { get; set; }
        public double UsageMin { get; set; }
        public double UsageMax { get; set; }
        public DateTime? LastSeen { get; set; }

        public double UsageTarget => (UsageMax + UsageMin) / 2;

    }
}
