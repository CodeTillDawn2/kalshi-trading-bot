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
        public string? BrainInstanceName { get; set; }

        // Basic market data
        public List<string>? Markets { get; set; }
        public long ErrorCount { get; set; }
        public DateTime? LastSnapshot { get; set; }
        public bool IsStartingUp { get; set; }
        public bool IsShuttingDown { get; set; }

        // Brain configuration
        public bool WatchPositions { get; set; }
        public bool WatchOrders { get; set; }
        public bool ManagedWatchList { get; set; }
        public bool CaptureSnapshots { get; set; }
        public int TargetWatches { get; set; }
        public double MinimumInterest { get; set; }
        public double UsageMin { get; set; }
        public double UsageMax { get; set; }

        // Performance metrics
        public double CurrentCpuUsage { get; set; }
        public double EventQueueAvg { get; set; }
        public double TickerQueueAvg { get; set; }
        public double NotificationQueueAvg { get; set; }
        public double OrderbookQueueAvg { get; set; }
        public double LastRefreshCycleSeconds { get; set; }
        public double LastRefreshCycleInterval { get; set; }
        public double LastRefreshMarketCount { get; set; }
        public double LastRefreshUsagePercentage { get; set; }
        public bool LastRefreshTimeAcceptable { get; set; }
        public DateTime? LastPerformanceSampleDate { get; set; }

        // Connection status
        public bool IsWebSocketConnected { get; set; }

        // Market watch data
        public List<MarketWatchData>? WatchedMarkets { get; set; }
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