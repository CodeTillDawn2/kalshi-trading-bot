namespace KalshiBotData.Models
{
    /// <summary>
    /// Represents a brain instance configuration and operational state in the trading bot system.
    /// This entity manages the settings and runtime state for individual brain instances that
    /// execute trading strategies, monitor markets, and capture snapshots. Each brain instance
    /// can have different configurations for market watching, position tracking, and resource usage.
    /// </summary>
    public class BrainInstance
    {
        /// <summary>
        /// Gets or sets the unique name identifier for this brain instance.
        /// This serves as the primary identifier for the brain in the system.
        /// </summary>
        public string BrainInstanceName { get; set; }

        /// <summary>
        /// Gets or sets the brain lock GUID that prevents multiple instances from
        /// operating on the same markets simultaneously. When set, this brain has
        /// exclusive access to certain market operations.
        /// </summary>
        public Guid? BrainLock { get; set; }

        /// <summary>
        /// Gets or sets whether this brain instance should monitor and track trading positions.
        /// When enabled, the brain will maintain position data and execute position-based strategies.
        /// </summary>
        public bool WatchPositions { get; set; }

        /// <summary>
        /// Gets or sets whether this brain instance should monitor and track trading orders.
        /// When enabled, the brain will maintain order data and execute order-based strategies.
        /// </summary>
        public bool WatchOrders { get; set; }

        /// <summary>
        /// Gets or sets whether this brain instance uses a managed watch list.
        /// When enabled, the system automatically manages which markets this brain monitors
        /// based on interest scores and performance metrics.
        /// </summary>
        public bool ManagedWatchList { get; set; }

        /// <summary>
        /// Gets or sets whether this brain instance should capture market snapshots.
        /// When enabled, the brain will periodically save comprehensive market state
        /// for analysis and backtesting purposes.
        /// </summary>
        public bool CaptureSnapshots { get; set; }

        /// <summary>
        /// Gets or sets the target number of markets this brain instance should watch.
        /// This is used by the managed watch list system to determine how many markets
        /// to allocate to this brain instance.
        /// </summary>
        public int TargetWatches { get; set; }

        /// <summary>
        /// Gets or sets the minimum interest score threshold for markets that this brain
        /// instance will consider watching. Markets below this threshold will be ignored.
        /// </summary>
        public double MinimumInterest { get; set; }

        /// <summary>
        /// Gets or sets the minimum resource usage threshold for this brain instance.
        /// This helps in load balancing and preventing resource exhaustion.
        /// </summary>
        public double UsageMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum resource usage threshold for this brain instance.
        /// This prevents any single brain from consuming too many system resources.
        /// </summary>
        public double UsageMax { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this brain instance was last seen active.
        /// This is used for monitoring brain health and detecting failed instances.
        /// </summary>
        public DateTime? LastSeen { get; set; }

    }
}
