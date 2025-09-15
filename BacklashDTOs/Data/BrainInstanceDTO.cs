namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for brain instance configuration.
    /// </summary>
    public class BrainInstanceDTO
    {
        /// <summary>
        /// Gets or sets the name of the brain instance.
        /// </summary>
        public string? BrainInstanceName { get; set; }

        /// <summary>
        /// Gets or sets the brain lock identifier.
        /// </summary>
        public Guid? BrainLock { get; set; }

        /// <summary>
        /// Gets or sets whether to watch positions.
        /// </summary>
        public bool WatchPositions { get; set; }

        /// <summary>
        /// Gets or sets whether to watch orders.
        /// </summary>
        public bool WatchOrders { get; set; }

        /// <summary>
        /// Gets or sets whether the watch list is managed.
        /// </summary>
        public bool ManagedWatchList { get; set; }

        /// <summary>
        /// Gets or sets whether to capture snapshots.
        /// </summary>
        public bool CaptureSnapshots { get; set; }

        /// <summary>
        /// Gets or sets the target number of watches.
        /// </summary>
        public int TargetWatches { get; set; }

        /// <summary>
        /// Gets or sets the minimum interest threshold.
        /// </summary>
        public double MinimumInterest { get; set; }

        /// <summary>
        /// Gets or sets the minimum usage threshold.
        /// </summary>
        public double UsageMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum usage threshold.
        /// </summary>
        public double UsageMax { get; set; }

        /// <summary>
        /// Gets or sets the last seen timestamp.
        /// </summary>
        public DateTime? LastSeen { get; set; }

        /// <summary>
        /// Gets the target usage calculated as the average of UsageMin and UsageMax.
        /// </summary>
        public double UsageTarget => (UsageMax + UsageMin) / 2;

    }
}
