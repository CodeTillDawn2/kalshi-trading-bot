using KalshiBotOverseer.Models;
using System.Collections.Concurrent;

namespace KalshiBotOverseer.Services
{
    public class BrainPersistenceService
    {
        private readonly ConcurrentDictionary<string, BrainPersistence> _brains = new();

        public BrainPersistence GetBrain(string brainInstanceName)
        {
            return _brains.GetOrAdd(brainInstanceName, name => new BrainPersistence { BrainInstanceName = name });
        }

        public void SaveBrain(BrainPersistence brain)
        {
            _brains[brain.BrainInstanceName] = brain;
        }

        public IEnumerable<string> GetTargetMarketTickers(string brainInstanceName)
        {
            var brain = GetBrain(brainInstanceName);
            return brain.TargetMarketTickers;
        }

        public IEnumerable<BrainPersistence> GetAllBrains()
        {
            return _brains.Values;
        }

        public void UpdateCurrentMarketTickers(string brainInstanceName, IEnumerable<string> tickers)
        {
            var brain = GetBrain(brainInstanceName);
            brain.CurrentMarketTickers = new List<string>(tickers);
            SaveBrain(brain);
        }

        public void UpdateMetricHistory(string brainInstanceName, string metricName, double value)
        {
            var brain = GetBrain(brainInstanceName);
            var history = GetHistoryList(brain, metricName);
            history.Add(new Models.MetricHistory { Timestamp = DateTime.UtcNow, Value = value });

            // Keep only last 50 entries to prevent memory issues
            if (history.Count > 50)
            {
                history.RemoveRange(0, history.Count - 50);
            }

            SaveBrain(brain);
        }

        private List<Models.MetricHistory> GetHistoryList(BrainPersistence brain, string metricName)
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