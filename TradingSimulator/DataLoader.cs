using BacklashDTOs.Data;
using TradingStrategies;
using System.Text.RegularExpressions;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Configuration;
using Microsoft.Extensions.Options;
using BacklashBotData.Data.Interfaces;
using TradingStrategies.Configuration;

namespace TradingSimulator
{
    /// <summary>
    /// Provides functionality for loading and filtering market snapshot data for the trading simulator.
    /// </summary>
    public class DataLoader
    {
        private readonly ITradingSnapshotService _snapshotService;
        private readonly IOptions<DataLoaderConfig> _dataLoaderConfig;

        /// <summary>
        /// Initializes a new instance of the DataLoader class.
        /// </summary>
        /// <param name="snapshotService">The trading snapshot service.</param>
        /// <param name="dataLoaderOptions">The simulator configuration options.</param>
        public DataLoader(ITradingSnapshotService snapshotService, IOptions<DataLoaderConfig> dataLoaderOptions)
        {
            _snapshotService = snapshotService;
            _dataLoaderConfig = dataLoaderOptions;
        }

        /// <summary>
        /// Gets filtered snapshot groups asynchronously based on the provided markets.
        /// </summary>
        /// <param name="context">The backlash bot context.</param>
        /// <param name="marketsToRun">The list of markets to filter by, or null for all.</param>
        /// <returns>A task that represents the asynchronous operation, containing the filtered snapshot groups.</returns>
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

        /// <summary>
        /// Loads snapshots for a specific market asynchronously.
        /// </summary>
        /// <param name="context">The backlash bot context.</param>
        /// <param name="marketName">The name of the market.</param>
        /// <returns>A task that represents the asynchronous operation, containing the list of market snapshots.</returns>
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

        /// <summary>
        /// Loads snapshots for multiple markets asynchronously.
        /// </summary>
        /// <param name="context">The backlash bot context.</param>
        /// <param name="markets">The list of market names.</param>
        /// <returns>A task that represents the asynchronous operation, containing a dictionary of market snapshots.</returns>
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
                if (_dataLoaderConfig.Value.EnableSnapshotValidation && marketSnapshots.Any())
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
            int minCount = _dataLoaderConfig.Value.MinSnapshotCountForValidation;

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
