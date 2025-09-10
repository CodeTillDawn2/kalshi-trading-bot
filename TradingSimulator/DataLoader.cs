using KalshiBotData.Data.Interfaces;
using BacklashDTOs.Data;
using TradingStrategies;
using System.Text.RegularExpressions;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;

namespace TradingSimulator
{
    public class DataLoader
    {
        private readonly ITradingSnapshotService _snapshotService;

        public DataLoader(ITradingSnapshotService snapshotService)
        {
            _snapshotService = snapshotService;
        }

        public async Task<List<SnapshotGroupDTO>> GetFilteredSnapshotGroupsAsync(
            IKalshiBotContext context, List<string>? marketsToRun)
        {
            var groups = await context.GetSnapshotGroups_cached().ConfigureAwait(false);
            var filtered = new List<SnapshotGroupDTO>();
            foreach (var g in groups)
            {
                var recorded = g.EndTime - g.StartTime;
                if (recorded.TotalHours < 1) continue;  // Skip if insufficient duration

                if (marketsToRun != null)
                {
                    var baseTicker = Regex.Replace(g.MarketTicker ?? "", @"_(\d+)$", "");
                    if (!marketsToRun.Contains(baseTicker, StringComparer.OrdinalIgnoreCase)) continue;
                }

                filtered.Add(g);
            }
            return filtered;
        }

        public async Task<List<MarketSnapshot>> LoadSnapshotsForMarketAsync(
            IKalshiBotContext context, string marketName)
        {
            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, marketsToRun: new() { marketName }).ConfigureAwait(false);
            var marketGroups = filteredGroups.Where(g => g.MarketTicker == marketName).ToList();

            var allSnapshotData = new List<SnapshotDTO>();
            foreach (var g in marketGroups)
            {
                var snaps = await context.GetSnapshots_cached(
                    marketTicker: g.MarketTicker,
                    startDate: g.StartTime,
                    endDate: g.EndTime).ConfigureAwait(false);
                allSnapshotData.AddRange(snaps);
            }
            allSnapshotData = allSnapshotData.OrderBy(x => x.SnapshotDate).ToList();

            var cache = await _snapshotService.LoadManySnapshots(allSnapshotData).ConfigureAwait(false);
            var marketSnapshots = cache
                .SelectMany(kvp => kvp.Value)
                .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue)
                .OrderBy(ms => ms.Timestamp)
                .ToList();

            return marketSnapshots;
        }

        public async Task<Dictionary<string, List<MarketSnapshot>>> LoadSnapshotsForMarketsAsync(
            IKalshiBotContext context, List<string> markets)
        {
            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, markets).ConfigureAwait(false);
            var uniqueMarkets = filteredGroups.Select(g => g.MarketTicker).Distinct().ToList();
            var dataset = new Dictionary<string, List<MarketSnapshot>>();

            foreach (var market in uniqueMarkets)
            {
                var marketGroups = filteredGroups.Where(g => g.MarketTicker == market).ToList();
                var allSnapshotData = new List<SnapshotDTO>();
                foreach (var g in marketGroups)
                {
                    var snaps = await context.GetSnapshots_cached(
                        marketTicker: g.MarketTicker,
                        startDate: g.StartTime,
                        endDate: g.EndTime).ConfigureAwait(false);
                    allSnapshotData.AddRange(snaps);
                }
                allSnapshotData = allSnapshotData.OrderBy(x => x.SnapshotDate).ToList();

                var cache = await _snapshotService.LoadManySnapshots(allSnapshotData).ConfigureAwait(false);
                var marketSnapshots = cache
                    .SelectMany(kvp => kvp.Value)
                    .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue)
                    .OrderBy(ms => ms.Timestamp)
                    .ToList();

                if (marketSnapshots.Any())
                {
                    dataset[market] = marketSnapshots;
                }
            }

            return dataset;
        }
    }
}