using BacklashDTOs.Data;
using TradingStrategies;
using System.Text.RegularExpressions;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Configuration;
using Microsoft.Extensions.Options;
using BacklashBotData.Data.Interfaces;

namespace TradingSimulator
{
    public class DataLoader
    {
        private readonly ITradingSnapshotService _snapshotService;
        private readonly IOptions<SimulatorConfig> _simulatorOptions;

        public DataLoader(ITradingSnapshotService snapshotService, IOptions<SimulatorConfig> simulatorOptions)
        {
            _snapshotService = snapshotService;
            _simulatorOptions = simulatorOptions;
        }

        public async Task<List<SnapshotGroupDTO>> GetFilteredSnapshotGroupsAsync(
            IBacklashBotContext context, List<string>? marketsToRun)
        {
            var groups = await context.GetSnapshotGroups().ConfigureAwait(false);
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
            IBacklashBotContext context, string marketName)
        {
            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, marketsToRun: new() { marketName }).ConfigureAwait(false);
            var marketGroups = filteredGroups.Where(g => g.MarketTicker == marketName).ToList();

            var allSnapshotData = new List<SnapshotDTO>();
            foreach (var g in marketGroups)
            {
                var snaps = await context.GetSnapshots(
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
            IBacklashBotContext context, List<string> markets)
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
                    var snaps = await context.GetSnapshots(
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

                // Validate snapshots if enabled
                if (_simulatorOptions.Value.EnableSnapshotValidation && marketSnapshots.Any())
                {
                    marketSnapshots = ValidateSnapshots(marketSnapshots, market);
                }

                if (marketSnapshots.Any())
                {
                    dataset[market] = marketSnapshots;
                }
            }

            return dataset;
        }

        private List<MarketSnapshot> ValidateSnapshots(List<MarketSnapshot> snapshots, string marketName)
        {
            var validSnapshots = new List<MarketSnapshot>();
            int minCount = _simulatorOptions.Value.MinSnapshotCountForValidation;

            if (snapshots.Count < minCount)
            {
                // Log warning but still return snapshots
                Console.WriteLine($"Warning: Market {marketName} has only {snapshots.Count} snapshots, minimum required is {minCount}. Proceeding with available data.");
                return snapshots;
            }

            foreach (var snapshot in snapshots)
            {
                if (IsValidSnapshot(snapshot))
                {
                    validSnapshots.Add(snapshot);
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid snapshot detected for market {marketName} at {snapshot.Timestamp}. Skipping.");
                }
            }

            return validSnapshots;
        }

        private bool IsValidSnapshot(MarketSnapshot snapshot)
        {
            // Basic validation checks
            if (snapshot == null) return false;
            if (string.IsNullOrWhiteSpace(snapshot.MarketTicker)) return false;
            if (snapshot.Timestamp == DateTime.MinValue || snapshot.Timestamp > DateTime.UtcNow.AddMinutes(1)) return false;
            if (snapshot.BestYesBid < 0 || snapshot.BestNoBid < 0) return false;
            if (snapshot.SnapshotSchemaVersion < 1) return false;

            // Additional integrity checks
            if (snapshot.OrderbookData == null || !snapshot.OrderbookData.Any()) return false;

            return true;
        }
    }
}