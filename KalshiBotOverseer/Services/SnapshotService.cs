using KalshiBotData.Data.Interfaces;
using SmokehouseDTOs.Data;

namespace KalshiBotOverseer.Services
{
    public class SnapshotService
    {
        private readonly IKalshiBotContext _context;

        public SnapshotService(IKalshiBotContext context)
        {
            _context = context;
        }

        public async Task<List<object>> GetSnapshotGroupsDataAsync()
        {
            // Get all snapshot groups with their related markets
            var snapshotGroups = await _context.GetSnapshotGroups_cached();

            var relatedMarkets = await _context.GetMarkets_cached(includedMarkets: snapshotGroups.Select(x => x.MarketTicker).Distinct().ToHashSet());

            return GetSnapshotGroupsData(snapshotGroups, relatedMarkets);
        }

        public List<object> GetSnapshotGroupsData(List<SnapshotGroupDTO> snapshotGroups)
        {
            // Group by MarketTicker and calculate aggregated data
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

            return groupedSnapshots.Cast<object>().ToList();
        }

        public List<object> GetSnapshotGroupsData(List<SnapshotGroupDTO> snapshotGroups, List<MarketDTO> relatedMarkets)
        {
            // Create a lookup for markets for faster access
            var marketLookup = relatedMarkets.ToDictionary(m => m.market_ticker, m => m);

            // Group by MarketTicker and calculate aggregated data
            var groupedSnapshots = snapshotGroups
                .GroupBy(sg => sg.MarketTicker)
                .Select(group =>
                {
                    marketLookup.TryGetValue(group.Key, out var market);
                    var firstSnapshot = group.First(); // Use first snapshot for individual fields

                    // Calculate total recorded hours for this market
                    var totalRecordedHours = group.Sum(sg =>
                        (decimal)(sg.EndTime - sg.StartTime).TotalSeconds / 3600.0m);

                    // Calculate market hours
                    var marketHours = market != null ?
                        (decimal)(market.close_time - market.open_time).TotalSeconds / 3600.0m : 0;

                    // Calculate recorded hours percentage
                    var recordedHoursPercentage = marketHours > 0 ?
                        Math.Round((totalRecordedHours / marketHours) * 100, 2) : (decimal?)null;

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

            return groupedSnapshots.Cast<object>().ToList();
        }
    }
}