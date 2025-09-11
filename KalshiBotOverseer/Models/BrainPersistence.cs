// BrainPersistence.cs
using System;
using System.Collections.Generic;

namespace KalshiBotOverseer.Models
{
    public class BrainPersistence
    {
        public string BrainInstanceName { get; set; }
        public Guid? Brain { get; set; }
        public bool WatchPositions { get; set; }
        public bool WatchOrders { get; set; }
        public bool ManagedWatchList { get; set; }
        public bool CaptureSnapshots { get; set; }
        public int TargetWatches { get; set; }
        public double MinimumInterest { get; set; }
        public double UsageMin { get; set; }
        public double UsageMax { get; set; }
        public DateTime? LastSeen { get; set; }
        public List<string> CurrentMarketTickers { get; set; } = new List<string>();
        public List<string> TargetMarketTickers { get; set; } = new List<string>();
        public string Mode { get; set; } = "Autonomous";
        public bool IsStartingUp { get; set; }
        public bool IsShuttingDown { get; set; }
        public long ErrorCount { get; set; }
        public DateTime? LastSnapshot { get; set; }
        public bool IsWebSocketConnected { get; set; }

        // Historical performance metrics
        public List<MetricHistory> CpuUsageHistory { get; set; } = new List<MetricHistory>();
        public List<MetricHistory> EventQueueHistory { get; set; } = new List<MetricHistory>();
        public List<MetricHistory> TickerQueueHistory { get; set; } = new List<MetricHistory>();
        public List<MetricHistory> NotificationQueueHistory { get; set; } = new List<MetricHistory>();
        public List<MetricHistory> OrderbookQueueHistory { get; set; } = new List<MetricHistory>();
        public List<MetricHistory> MarketCountHistory { get; set; } = new List<MetricHistory>();
        public List<MetricHistory> ErrorHistory { get; set; } = new List<MetricHistory>();
        public List<MetricHistory> RefreshCycleSecondsHistory { get; set; } = new List<MetricHistory>();
        public List<MetricHistory> RefreshCycleIntervalHistory { get; set; } = new List<MetricHistory>();
        public List<MetricHistory> RefreshMarketCountHistory { get; set; } = new List<MetricHistory>();
        public List<MetricHistory> RefreshUsagePercentageHistory { get; set; } = new List<MetricHistory>();
        public List<MetricHistory> PerformanceSampleDateHistory { get; set; } = new List<MetricHistory>();
        public bool LastRefreshTimeAcceptable { get; set; }
    }

    public class BrainStatusData
    {
        // Basic brain info
        public string? brainInstanceName { get; set; }

        // Basic market data
        public List<string>? markets { get; set; }
        public long errorCount { get; set; }
        public DateTime? lastSnapshot { get; set; }
        public DateTime? lastCheckIn { get; set; }
        public bool isStartingUp { get; set; }
        public bool isShuttingDown { get; set; }

        // Brain configuration
        public bool watchPositions { get; set; }
        public bool watchOrders { get; set; }
        public bool managedWatchList { get; set; }
        public bool captureSnapshots { get; set; }
        public int targetWatches { get; set; }
        public double minimumInterest { get; set; }
        public double usageMin { get; set; }
        public double usageMax { get; set; }

        // Performance metrics
        public double currentCpuUsage { get; set; }
        public double eventQueueAvg { get; set; }
        public double tickerQueueAvg { get; set; }
        public double notificationQueueAvg { get; set; }
        public double orderbookQueueAvg { get; set; }
        public double lastRefreshCycleSeconds { get; set; }
        public double lastRefreshCycleInterval { get; set; }
        public double lastRefreshMarketCount { get; set; }
        public double lastRefreshUsagePercentage { get; set; }
        public bool lastRefreshTimeAcceptable { get; set; }
        public DateTime? lastPerformanceSampleDate { get; set; }

        // Connection status
        public bool isWebSocketConnected { get; set; }

        // Market watch data
        public List<MarketWatchData>? watchedMarkets { get; set; }
    }

    public class MetricHistory
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }

    public class MarketWatchData
    {
        public string MarketTicker { get; set; } = "";
        public Guid? Brain { get; set; }
        public double? InterestScore { get; set; }
        public DateTime? InterestScoreDate { get; set; }
        public DateTime? LastWatched { get; set; }
        public double? AverageWebsocketEventsPerMinute { get; set; }
    }
}