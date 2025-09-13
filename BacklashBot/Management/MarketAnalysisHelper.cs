using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using BacklashDTOs.Configuration;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashDTOs.Data;
using BacklashInterfaces.Constants;
using TradingStrategies.Classification.Interfaces;

namespace BacklashBot.Management
{
    /// <summary>
    /// Helper service for performing market analysis operations, specifically generating snapshot groups
    /// from raw market snapshots by filtering markets and processing them into valid time periods.
    /// </summary>
    public class MarketAnalysisHelper : IMarketAnalysisHelper
    {
        private readonly ExecutionConfig _executionConfig;
        private readonly ISnapshotPeriodHelper _snapshotPeriodHelper;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITradingSnapshotService _snapshotService;
        private readonly ILogger<MarketAnalysisHelper> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketAnalysisHelper"/> class.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes to access database context.</param>
        /// <param name="snapshotPeriodHelper">Helper for processing snapshot periods into valid groups.</param>
        /// <param name="snapshotService">Service for managing trading snapshots.</param>
        /// <param name="executionConfig">Configuration options for execution settings.</param>
        /// <param name="logger">Logger for recording analysis operations and errors.</param>
        public MarketAnalysisHelper(IServiceScopeFactory scopeFactory, ISnapshotPeriodHelper snapshotPeriodHelper, ITradingSnapshotService snapshotService, IOptions<ExecutionConfig> executionConfig, ILogger<MarketAnalysisHelper> logger)
        {
            _snapshotService = snapshotService;
            _scopeFactory = scopeFactory;
            _snapshotPeriodHelper = snapshotPeriodHelper;
            _executionConfig = executionConfig.Value;
            _logger = logger;
        }

        /// <summary>
        /// Generates snapshot groups for markets that have snapshots but haven't been analyzed yet.
        /// Filters markets by status and existing analysis, retrieves raw snapshots, validates schema consistency,
        /// and processes them into valid time periods for storage.
        /// </summary>
        /// <returns>A task representing the snapshot group generation operation.</returns>
        public async Task GenerateSnapshotGroups()
        {
            _logger.LogInformation("Starting snapshot group generation process.");

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            var tickersAnalyzed = await context.GetSnapshotGroupsFiltered();
            var tickerHashSet = tickersAnalyzed
                .Select(x => x.MarketTicker)
                .Distinct()
                .ToHashSet();

            HashSet<string> tickersWithSnapshots = await context.GetMarketTickersWithSnapshots();

            HashSet<string> tickersToAnalyze = (await context.GetMarketsFiltered(
                excludedStatuses: new HashSet<string> { KalshiConstants.Status_Active },
                excludedMarkets: tickerHashSet,
                includedMarkets: tickersWithSnapshots))
                .Select(x => x.market_ticker).ToHashSet();

            _logger.LogInformation("Found {Count} markets to analyze for snapshot groups.", tickersToAnalyze.Count);

            foreach (string marketTicker in tickersToAnalyze)
            {
                List<SnapshotDTO> rawSnapshots;

                try
                {
                    rawSnapshots = await context.GetSnapshots(marketTicker: marketTicker);
                    rawSnapshots = rawSnapshots.OrderBy(x => x.SnapshotDate).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve snapshots for market {MarketTicker}. Retrying after delay.", marketTicker);
                    try
                    {
                        await Task.Delay(5000);
                        rawSnapshots = await context.GetSnapshotsFiltered(marketTicker: marketTicker);
                        rawSnapshots = rawSnapshots.OrderBy(x => x.SnapshotDate).ToList();
                    }
                    catch (Exception retryEx)
                    {
                        _logger.LogError(retryEx, "Failed to retrieve snapshots for market {MarketTicker} after retry. Skipping.", marketTicker);
                        continue;
                    }
                }

                if (rawSnapshots.Count == 0)
                {
                    _logger.LogWarning("No snapshots found for market {MarketTicker}. Skipping.", marketTicker);
                    continue;
                }

                int expectedSchema = rawSnapshots[0].JSONSchemaVersion;
                var validSnapshots = new List<SnapshotDTO>();
                foreach (var snapshot in rawSnapshots)
                {
                    if (snapshot.JSONSchemaVersion != expectedSchema)
                    {
                        _logger.LogWarning("Schema version mismatch for snapshot at {SnapshotDate} in market {MarketTicker}. Expected {Expected}, got {Actual}. Skipping snapshot.",
                            snapshot.SnapshotDate, marketTicker, expectedSchema, snapshot.JSONSchemaVersion);
                        continue;
                    }
                    validSnapshots.Add(snapshot);
                }

                if (validSnapshots.Count == 0)
                {
                    _logger.LogWarning("No valid snapshots with consistent schema found for market {MarketTicker}. Skipping.", marketTicker);
                    continue;
                }

                string snapshotDirectory = Path.Combine(_executionConfig.HardDataStorageLocation, "Preprocessed", "SnapshotGroups");

                // Process valid snapshots into snapshot groups
                var validPeriods = await _snapshotPeriodHelper.SplitIntoValidGroups(validSnapshots, snapshotDirectory);

                await context.AddOrUpdateSnapshotGroups(validPeriods);
                _logger.LogInformation("Successfully processed {Count} snapshot groups for market {MarketTicker}.", validPeriods.Count, marketTicker);
            }

            _logger.LogInformation("Completed snapshot group generation process.");
        }
    }
}
