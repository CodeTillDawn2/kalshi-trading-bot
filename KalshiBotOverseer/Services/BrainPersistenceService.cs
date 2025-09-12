using KalshiBotOverseer.Models;
using System.Collections.Concurrent;

namespace KalshiBotOverseer.Services
{
    /// <summary>
    /// Service responsible for managing the in-memory persistence of brain instance states.
    /// This service provides thread-safe operations for storing, retrieving, and updating brain
    /// configuration, market watch lists, and performance metrics history. It serves as the
    /// central data store for brain instances in the Kalshi trading bot overseer system.
    /// </summary>
    public class BrainPersistenceService
    {
        private readonly ConcurrentDictionary<string, BrainPersistence> _brains = new();

        /// <summary>
        /// Retrieves or creates a brain instance by name. If the brain doesn't exist,
        /// a new instance is created with the specified name and added to the collection.
        /// </summary>
        /// <param name="brainInstanceName">The unique name identifier for the brain instance.</param>
        /// <returns>The BrainPersistence object for the specified brain instance.</returns>
        public BrainPersistence GetBrain(string brainInstanceName)
        {
            return _brains.GetOrAdd(brainInstanceName, name => new BrainPersistence { BrainInstanceName = name });
        }

        /// <summary>
        /// Saves or updates a brain instance in the persistence store.
        /// This method replaces any existing brain instance with the same name.
        /// </summary>
        /// <param name="brain">The BrainPersistence object to save.</param>
        public void SaveBrain(BrainPersistence brain)
        {
            _brains[brain.BrainInstanceName] = brain;
        }

        /// <summary>
        /// Retrieves the target market tickers for a specific brain instance.
        /// Target tickers represent the markets the brain should be monitoring.
        /// </summary>
        /// <param name="brainInstanceName">The name of the brain instance.</param>
        /// <returns>An enumerable collection of target market ticker symbols.</returns>
        public IEnumerable<string> GetTargetMarketTickers(string brainInstanceName)
        {
            var brain = GetBrain(brainInstanceName);
            return brain.TargetMarketTickers;
        }

        /// <summary>
        /// Retrieves all brain instances currently stored in the persistence service.
        /// This provides access to the complete set of brain configurations and states.
        /// </summary>
        /// <returns>An enumerable collection of all BrainPersistence objects.</returns>
        public IEnumerable<BrainPersistence> GetAllBrains()
        {
            return _brains.Values;
        }

        /// <summary>
        /// Updates the current market tickers for a brain instance.
        /// This reflects the markets the brain is actively watching at runtime.
        /// </summary>
        /// <param name="brainInstanceName">The name of the brain instance to update.</param>
        /// <param name="tickers">The collection of market ticker symbols currently being watched.</param>
        public void UpdateCurrentMarketTickers(string brainInstanceName, IEnumerable<string> tickers)
        {
            var brain = GetBrain(brainInstanceName);
            brain.CurrentMarketTickers = new List<string>(tickers);
            SaveBrain(brain);
        }

        /// <summary>
        /// Updates the historical metrics for a specific brain instance.
        /// Metrics are stored in rolling lists with a maximum of 50 entries to prevent memory issues.
        /// </summary>
        /// <param name="brainInstanceName">The name of the brain instance.</param>
        /// <param name="metricName">The name of the metric to update (e.g., "CpuUsage", "EventQueue").</param>
        /// <param name="value">The metric value to record.</param>
        public void UpdateMetricHistory(string brainInstanceName, string metricName, double value)
        {
            var brain = GetBrain(brainInstanceName);
            var history = GetMetricHistoryList(brain, metricName);
            history.Add(new Models.MetricHistory { Timestamp = DateTime.UtcNow, Value = value });

            // Keep only last 50 entries to prevent memory issues
            if (history.Count > 50)
            {
                history.RemoveRange(0, history.Count - 50);
            }

            SaveBrain(brain);
        }

        /// <summary>
        /// Retrieves the appropriate metric history list for a given brain and metric name.
        /// This method maps metric names to their corresponding history collections.
        /// </summary>
        /// <param name="brain">The BrainPersistence object containing the history lists.</param>
        /// <param name="metricName">The name of the metric to retrieve history for.</param>
        /// <returns>The list of MetricHistory entries for the specified metric.</returns>
        /// <exception cref="ArgumentException">Thrown when an unknown metric name is provided.</exception>
        private List<Models.MetricHistory> GetMetricHistoryList(BrainPersistence brain, string metricName)
        {
            return metricName switch
            {
                "CpuUsage" => brain.CpuUsageHistory,
                "EventQueue" => brain.EventQueueHistory,
                "TickerQueue" => brain.TickerQueueHistory,
                "NotificationQueue" => brain.NotificationQueueHistory,
                "OrderbookQueue" => brain.OrderbookQueueHistory,
                "MarketCount" => brain.MarketCountHistory,
                "Error" => brain.ErrorHistory,
                _ => throw new ArgumentException($"Unknown metric: {metricName}")
            };
        }
    }
}