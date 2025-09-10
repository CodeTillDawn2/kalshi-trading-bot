using System;
using System.Collections.Generic;

namespace KalshiBotOverseer.Models
{
    public class BrainPersistence
    {
        public string BrainInstanceName { get; set; }
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
        public HashSet<string> CurrentMarketTickers { get; set; } = new HashSet<string>();
        public HashSet<string> TargetMarketTickers { get; set; } = new HashSet<string>();
    }

    public class BrainStatusData
    {
        // Basic brain info
        public string BrainInstanceName { get; set; } = "";
        public Guid? BrainLock { get; set; }

        // Configuration flags
        public bool WatchPositions { get; set; }
        public bool WatchOrders { get; set; }
        public bool ManagedWatchList { get; set; }
        public bool CaptureSnapshots { get; set; }

        // Performance and limits
        public int TargetWatches { get; set; }
        public double MinimumInterest { get; set; }
        public double UsageMin { get; set; }
        public double UsageMax { get; set; }

        // Status information
        public DateTime? LastSeen { get; set; }
        public bool IsStartingUp { get; set; }
        public bool IsShuttingDown { get; set; }
        public bool IsWebSocketConnected { get; set; }

        // Performance metrics
        public double CurrentCpuUsage { get; set; }
        public double EventQueueAvg { get; set; }
        public double TickerQueueAvg { get; set; }
        public double NotificationQueueAvg { get; set; }
        public double OrderbookQueueAvg { get; set; }

        // Market data
        public int MarketCount { get; set; }
        public long ErrorCount { get; set; }
        public DateTime? LastSnapshot { get; set; }
        public DateTime? LastCheckIn { get; set; }

        // Market watch data (aggregated from MarketWatch entries)
        public List<MarketWatchData> WatchedMarkets { get; set; } = new List<MarketWatchData>();

        // Current and target tickers
        public List<string> CurrentMarketTickers { get; set; } = new List<string>();
        public List<string> TargetMarketTickers { get; set; } = new List<string>();
    }

    public class MarketWatchData
    {
        public string MarketTicker { get; set; } = "";
        public Guid? BrainLock { get; set; }
        public double? InterestScore { get; set; }
        public DateTime? InterestScoreDate { get; set; }
        public DateTime? LastWatched { get; set; }
        public double? AverageWebsocketEventsPerMinute { get; set; }
    }
}