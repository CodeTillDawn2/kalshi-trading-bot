using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BacklashDTOs;
using BacklashDTOs.Data;
using BacklashInterfaces.SmokehouseBot.Services;
using BacklashBot.Services.Interfaces;
using Microsoft.Extensions.Logging;
using BacklashPatterns.PatternDefinitions;

namespace BacklashPatterns
{
    /// <summary>
    /// Caches historical pattern instances for comparative strength calculation.
    /// Loads historical MarketSnapshots, detects patterns, and stores metrics.
    /// </summary>
    public class HistoricalPatternCache
    {
        private readonly ILogger<HistoricalPatternCache> _logger;
        private readonly ITradingSnapshotService _snapshotService;
        private readonly Dictionary<string, List<PatternMetrics>> _patternCache = new();

        /// <summary>
        /// Represents metrics for a historical pattern instance.
        /// </summary>
        public class PatternMetrics
        {
            public string PatternName { get; set; }
            public double PowerScore { get; set; } // Power component of strength
            public double MatchScore { get; set; } // Match component of strength
            public double CombinedStrength { get; set; }
            public DateTime Timestamp { get; set; }
            public string MarketTicker { get; set; }
        }

        public HistoricalPatternCache(
            ILogger<HistoricalPatternCache> logger,
            ITradingSnapshotService snapshotService)
        {
            _logger = logger;
            _snapshotService = snapshotService;
        }

        /// <summary>
        /// Loads historical data and builds pattern cache for specified markets and date range.
        /// </summary>
        public async Task BuildCacheAsync(List<string> marketTickers, DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Building historical pattern cache from {StartDate} to {EndDate} for {Count} markets",
                startDate, endDate, marketTickers.Count);

            var snapshotDTOs = await LoadSnapshotDTOsAsync(marketTickers, startDate, endDate);
            var marketSnapshots = await _snapshotService.LoadManySnapshots(snapshotDTOs, false);

            foreach (var marketEntry in marketSnapshots)
            {
                var snapshots = marketEntry.Value.OrderBy(s => s.Timestamp).ToList();
                var patterns = await DetectPatternsFromSnapshotsAsync(snapshots, marketEntry.Key);

                foreach (var pattern in patterns)
                {
                    if (!_patternCache.ContainsKey(pattern.PatternName))
                    {
                        _patternCache[pattern.PatternName] = new List<PatternMetrics>();
                    }
                    _patternCache[pattern.PatternName].Add(pattern);
                }
            }

            _logger.LogInformation("Cache built with {Count} pattern instances across {PatternCount} pattern types",
                _patternCache.Values.Sum(list => list.Count), _patternCache.Count);
        }

        /// <summary>
        /// Loads SnapshotDTOs from database (placeholder - replace with actual query).
        /// </summary>
        private async Task<List<SnapshotDTO>> LoadSnapshotDTOsAsync(List<string> marketTickers, DateTime startDate, DateTime endDate)
        {
            // TODO: Replace with actual database query using ITradingSnapshotService or direct DB access
            // Example: Query snapshots for each ticker in date range
            var snapshotDTOs = new List<SnapshotDTO>();
            foreach (var ticker in marketTickers)
            {
                // Mock data for development - replace with real query
                snapshotDTOs.Add(new SnapshotDTO { MarketTicker = ticker, SnapshotDate = startDate });
            }
            await Task.Delay(100); // Simulate async operation
            return snapshotDTOs;
        }

        /// <summary>
        /// Detects patterns from a list of MarketSnapshots for a specific market.
        /// </summary>
        private async Task<List<PatternMetrics>> DetectPatternsFromSnapshotsAsync(List<MarketSnapshot> snapshots, string marketTicker)
        {
            var patterns = new List<PatternMetrics>();
            var metricsCache = new Dictionary<int, CandleMetrics>();

            // Convert snapshots to candle data (simplified - assume 1-minute candles)
            var candles = snapshots.Select((s, i) => new CandleMids
            {
                Open = s.BestYesBidD,
                High = s.BestYesBidD + 0.01, // Mock high/low
                Low = s.BestYesBidD - 0.01,
                Close = s.BestYesBidD,
                Volume = s.TradeVolumePerMinute_Yes ?? 0,
                Timestamp = s.Timestamp
            }).ToArray();

            // Detect patterns (example for AbandonedBabyPattern - extend for others)
            for (int i = 2; i < candles.Length; i++)
            {
                var pattern = await AbandonedBabyPattern.IsPatternAsync(i, 10, metricsCache, candles, true);
                if (pattern != null)
                {
                    // Calculate metrics (simplified - use actual calculation)
                    var metrics = new PatternMetrics
                    {
                        PatternName = pattern.Name,
                        PowerScore = 0.5, // Placeholder - calculate from pattern data
                        MatchScore = 0.5,
                        CombinedStrength = 0.5,
                        Timestamp = candles[i].Timestamp,
                        MarketTicker = marketTicker
                    };
                    patterns.Add(metrics);
                }
            }

            return patterns;
        }

        /// <summary>
        /// Retrieves comparative data for a pattern type.
        /// </summary>
        public List<PatternMetrics> GetComparativeData(string patternName)
        {
            return _patternCache.ContainsKey(patternName) ? _patternCache[patternName] : new List<PatternMetrics>();
        }

        /// <summary>
        /// Calculates comparative strength for a new pattern instance.
        /// </summary>
        public double CalculateComparativeStrength(string patternName, double powerScore, double matchScore)
        {
            var historicalData = GetComparativeData(patternName);
            if (!historicalData.Any()) return (powerScore + matchScore) / 2;

            var combinedScore = (powerScore + matchScore) / 2;
            var historicalStrengths = historicalData.Select(h => h.CombinedStrength).ToList();
            var mean = historicalStrengths.Average();
            var stdDev = Math.Sqrt(historicalStrengths.Sum(s => Math.Pow(s - mean, 2)) / historicalStrengths.Count);

            // Z-score normalization
            var zScore = stdDev > 0 ? (combinedScore - mean) / stdDev : 0;
            var percentile = CalculatePercentile(historicalStrengths, combinedScore);

            // Boost if in top quartile
            var adjustedStrength = combinedScore;
            if (percentile >= 75) adjustedStrength += 0.1;
            else if (percentile <= 25) adjustedStrength -= 0.1;

            return Math.Clamp(adjustedStrength, 0, 1);
        }

        private double CalculatePercentile(List<double> values, double value)
        {
            var sorted = values.OrderBy(v => v).ToList();
            var index = sorted.FindIndex(v => v >= value);
            return (double)index / sorted.Count * 100;
        }
    }
}