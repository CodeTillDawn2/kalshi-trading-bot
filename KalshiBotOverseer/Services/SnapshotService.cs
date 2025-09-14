using KalshiBotData.Data.Interfaces;
using BacklashDTOs.Data;
using System;
using System.Diagnostics;

namespace KalshiBotOverseer.Services
{
    /// <summary>
    /// Provides services for retrieving and processing snapshot group data from the database.
    /// This service aggregates snapshot groups by market ticker, calculates recorded hours,
    /// market hours, and recorded hours percentages, and returns structured data for analysis.
    /// </summary>
    public class SnapshotService
    {
        private readonly IKalshiBotContext _context;

        // Performance metrics storage
        private readonly List<long> _aggregationTimes = new List<long>();
        private long _totalAggregationTime;
        private int _aggregationCount;

        /// <summary>
        /// Initializes a new instance of the SnapshotService class.
        /// </summary>
        /// <param name="context">The database context interface for accessing snapshot and market data.</param>
        public SnapshotService(IKalshiBotContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Asynchronously retrieves all snapshot groups from the database and processes them
        /// into aggregated market data without related market information.
        /// </summary>
        /// <returns>A list of anonymous objects containing aggregated snapshot data per market.</returns>
        public async Task<List<object>> GetSnapshotGroupsDataAsync()
        {
            // Get all snapshot groups with their related markets
            var snapshotGroups = await _context.GetSnapshotGroups();

            var relatedMarkets = await _context.GetMarketsFiltered(includedMarkets: snapshotGroups.Where(x => x.MarketTicker != null).Select(x => x.MarketTicker!).Distinct().ToHashSet());

            return await GetSnapshotGroupsDataAsync(snapshotGroups, relatedMarkets);
        }

        /// <summary>
        /// Asynchronously processes a list of snapshot groups into aggregated market data without related market information.
        /// Groups snapshots by market ticker and calculates total recorded hours and other metrics.
        /// </summary>
        /// <param name="snapshotGroups">The list of snapshot groups to process.</param>
        /// <returns>A task representing the asynchronous operation, containing a list of anonymous objects with aggregated snapshot data per market.</returns>
        public async Task<List<object>> GetSnapshotGroupsDataAsync(List<SnapshotGroupDTO> snapshotGroups)
        {
            if (snapshotGroups == null)
                throw new ArgumentNullException(nameof(snapshotGroups));

            // Group by MarketTicker and calculate aggregated data
            var stopwatch = Stopwatch.StartNew();
            var groupedSnapshots = snapshotGroups
                .GroupBy(sg => sg.MarketTicker)
                .Select(group =>
                {
                    var firstSnapshot = group.First(); // Use first snapshot for individual fields

                    // Calculate total recorded hours for this market
                    var totalRecordedHours = group.Sum(sg =>
                        (decimal)(sg.EndTime - sg.StartTime).TotalSeconds / 3600.0m);

                    // Since we don't have market data, we can't calculate market hours or percentage
                    // Use the individual snapshot duration as a fallback
                    var recordedHoursPercentage = (decimal?)null;

                    // Check if any snapshot in the group recorded the end (YesEnd=0 or NoEnd=0)
                    var recordedEnd = group.Any(sg => sg.YesEnd == 0 || sg.NoEnd == 0);

                    return new
                    {
                        MarketTicker = group.Key,
                        Title = "Unknown Market", // No market data available
                        RecordedHours = Math.Round((decimal)(firstSnapshot.EndTime - firstSnapshot.StartTime).TotalSeconds / 3600.0m, 2),
                        MarketHours = (decimal)0, // No market data available
                        RecordedHoursPercentage = recordedHoursPercentage,
                        GroupCount = group.Count(),
                        RecordedEnd = recordedEnd,
                        StartTime = firstSnapshot.StartTime,
                        EndTime = firstSnapshot.EndTime,
                        YesStart = firstSnapshot.YesStart,
                        NoStart = firstSnapshot.NoStart,
                        YesEnd = firstSnapshot.YesEnd,
                        NoEnd = firstSnapshot.NoEnd,
                        AverageLiquidity = firstSnapshot.AverageLiquidity,
                        ProcessedDttm = firstSnapshot.ProcessedDttm
                    };
                })
                .OrderBy(s => s.MarketTicker)
                .ToList();

            stopwatch.Stop();
            // Store performance metrics
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            _aggregationTimes.Add(elapsedMs);
            _totalAggregationTime += elapsedMs;
            _aggregationCount++;

            return await Task.FromResult(groupedSnapshots.Cast<object>().ToList());
        }

        /// <summary>
        /// Asynchronously processes a list of snapshot groups into aggregated market data with related market information.
        /// Groups snapshots by market ticker, calculates recorded hours, market hours, and percentages
        /// using the provided market data for accurate calculations.
        /// </summary>
        /// <param name="snapshotGroups">The list of snapshot groups to process.</param>
        /// <param name="relatedMarkets">The list of related market data for percentage calculations.</param>
        /// <returns>A task representing the asynchronous operation, containing a list of anonymous objects with aggregated snapshot data per market.</returns>
        public async Task<List<object>> GetSnapshotGroupsDataAsync(List<SnapshotGroupDTO> snapshotGroups, List<MarketDTO> relatedMarkets)
        {
            if (snapshotGroups == null)
                throw new ArgumentNullException(nameof(snapshotGroups));
            if (relatedMarkets == null)
                throw new ArgumentNullException(nameof(relatedMarkets));

            // Create a lookup for markets for faster access
            var marketLookup = relatedMarkets.Where(m => m.market_ticker != null).ToDictionary(m => m.market_ticker!, m => m);

            // Group by MarketTicker and calculate aggregated data
            var stopwatch = Stopwatch.StartNew();
            var groupedSnapshots = snapshotGroups
                .GroupBy(sg => sg.MarketTicker)
                .Select(group =>
                {
                    marketLookup.TryGetValue(group.Key!, out var market);
                    var firstSnapshot = group.First(); // Use first snapshot for individual fields

                    // Calculate total recorded hours for this market
                    var totalRecordedHours = group.Sum(sg =>
                        (decimal)(sg.EndTime - sg.StartTime).TotalSeconds / 3600.0m);

                    // Calculate market hours
                    var marketHours = market != null ?
                        (decimal)(market.close_time - market.open_time).TotalSeconds / 3600.0m : 0;

                    // Calculate recorded hours percentage
                    var recordedHoursPercentage = (decimal?)null;
                    try
                    {
                        recordedHoursPercentage = marketHours > 0 ?
                            Math.Round((totalRecordedHours / marketHours) * 100, 2) : (decimal?)null;
                    }
                    catch (DivideByZeroException)
                    {
                        // This should not occur due to the > 0 check, but added for robustness
                        recordedHoursPercentage = null;
                    }

                    // Check if any snapshot in the group recorded the end (YesEnd=0 or NoEnd=0)
                    var recordedEnd = group.Any(sg => sg.YesEnd == 0 || sg.NoEnd == 0);

                    return new
                    {
                        MarketTicker = group.Key,
                        Title = market?.title ?? "Unknown Market",
                        RecordedHours = Math.Round((decimal)(firstSnapshot.EndTime - firstSnapshot.StartTime).TotalSeconds / 3600.0m, 2),
                        MarketHours = Math.Round(marketHours, 2),
                        RecordedHoursPercentage = recordedHoursPercentage,
                        GroupCount = group.Count(),
                        RecordedEnd = recordedEnd,
                        StartTime = firstSnapshot.StartTime,
                        EndTime = firstSnapshot.EndTime,
                        YesStart = firstSnapshot.YesStart,
                        NoStart = firstSnapshot.NoStart,
                        YesEnd = firstSnapshot.YesEnd,
                        NoEnd = firstSnapshot.NoEnd,
                        AverageLiquidity = firstSnapshot.AverageLiquidity,
                        ProcessedDttm = firstSnapshot.ProcessedDttm
                    };
                })
                .OrderBy(s => s.MarketTicker)
                .ToList();

            stopwatch.Stop();
            // Store performance metrics
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            _aggregationTimes.Add(elapsedMs);
            _totalAggregationTime += elapsedMs;
            _aggregationCount++;

            return await Task.FromResult(groupedSnapshots.Cast<object>().ToList());
        }

        /// <summary>
        /// Gets all recorded aggregation times in milliseconds.
        /// </summary>
        /// <returns>An array of aggregation times in milliseconds.</returns>
        /// <remarks>
        /// This method provides access to the performance metrics collected during snapshot aggregation operations.
        /// Each value represents the time taken for a single aggregation operation.
        /// </remarks>
        public long[] GetAggregationTimes()
        {
            return _aggregationTimes.ToArray();
        }

        /// <summary>
        /// Gets performance statistics for snapshot aggregation operations.
        /// </summary>
        /// <returns>A tuple containing count, average time, min time, and max time in milliseconds.</returns>
        /// <remarks>
        /// Returns comprehensive statistics about the performance of snapshot aggregation operations.
        /// If no aggregations have been performed, returns (0, 0, 0, 0).
        /// </remarks>
        public (int Count, double AverageMs, long MinMs, long MaxMs) GetAggregationStatistics()
        {
            if (_aggregationTimes.Count == 0)
                return (0, 0, 0, 0);

            return (
                _aggregationTimes.Count,
                _aggregationTimes.Average(),
                _aggregationTimes.Min(),
                _aggregationTimes.Max()
            );
        }

        /// <summary>
        /// Gets the total time spent on all aggregation operations.
        /// </summary>
        /// <returns>The total aggregation time in milliseconds.</returns>
        public long GetTotalAggregationTime()
        {
            return _totalAggregationTime;
        }

        /// <summary>
        /// Gets the number of aggregation operations performed.
        /// </summary>
        /// <returns>The count of aggregation operations.</returns>
        public int GetAggregationCount()
        {
            return _aggregationCount;
        }

        /// <summary>
        /// Clears all recorded aggregation times and resets counters.
        /// </summary>
        /// <remarks>
        /// This method resets the performance metrics collection, useful for starting
        /// fresh measurements or clearing accumulated data.
        /// </remarks>
        public void ClearAggregationMetrics()
        {
            _aggregationTimes.Clear();
            _totalAggregationTime = 0;
            _aggregationCount = 0;
        }
    }
}
