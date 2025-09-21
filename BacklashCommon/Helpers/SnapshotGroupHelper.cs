using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBotData.Data.Interfaces;
using BacklashCommon.Configuration;
using BacklashDTOs.Data;
using BacklashInterfaces.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using TradingStrategies.Classification.Interfaces;

namespace BacklashCommon.Helpers
{
    /// <summary>
    /// Helper service for performing market analysis operations, specifically generating snapshot groups
    /// from raw market snapshots by filtering markets and processing them into valid time periods.
    /// </summary>
    public class SnapshotGroupHelper : ISnapshotGroupHelper
    {
        private readonly DataStorageConfig _dataStorageConfig;
        private readonly SnapshotGroupHelperConfig _snapshotGroupHelperConfig;
        private readonly ISnapshotPeriodHelper _snapshotPeriodHelper;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITradingSnapshotService _snapshotService;
        private readonly ILogger<SnapshotGroupHelper> _logger;
        private readonly ICentralPerformanceMonitor? _centralPerformanceMonitor;
        private readonly bool _metricsEnabled;
        private int _totalMarketsProcessed = 0;
        private long _totalProcessingTimeMs = 0;
        private int _errorCount = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotGroupHelper"/> class.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes to access database context.</param>
        /// <param name="snapshotPeriodHelper">Helper for processing snapshot periods into valid groups.</param>
        /// <param name="snapshotService">Service for managing trading snapshots.</param>
        /// <param name="dataStorageConfig">Configuration options for general execution settings.</param>
        /// <param name="marketAnalysisHelperConfig">Configuration options for market analysis helper settings.</param>
        /// <param name="centralPerformanceMonitor">Central performance monitor for recording metrics. Can be null for environments without central monitoring.</param>
        /// <param name="logger">Logger for recording analysis operations and errors.</param>
        public SnapshotGroupHelper(IServiceScopeFactory scopeFactory, ISnapshotPeriodHelper snapshotPeriodHelper,
            ITradingSnapshotService snapshotService, IOptions<DataStorageConfig> dataStorageConfig,
            IOptions<SnapshotGroupHelperConfig> marketAnalysisHelperConfig, ICentralPerformanceMonitor centralPerformanceMonitor, ILogger<SnapshotGroupHelper> logger)
        {
            ArgumentNullException.ThrowIfNull(scopeFactory);
            ArgumentNullException.ThrowIfNull(snapshotPeriodHelper);
            ArgumentNullException.ThrowIfNull(snapshotService);
            ArgumentNullException.ThrowIfNull(dataStorageConfig);
            ArgumentNullException.ThrowIfNull(marketAnalysisHelperConfig);
            ArgumentNullException.ThrowIfNull(logger);

            _snapshotService = snapshotService;
            _scopeFactory = scopeFactory;
            _snapshotPeriodHelper = snapshotPeriodHelper;
            _dataStorageConfig = dataStorageConfig.Value ?? throw new ArgumentNullException(nameof(dataStorageConfig.Value));
            _snapshotGroupHelperConfig = marketAnalysisHelperConfig.Value ?? throw new ArgumentNullException(nameof(marketAnalysisHelperConfig.Value));
            _centralPerformanceMonitor = centralPerformanceMonitor;

            _logger = logger;
            _metricsEnabled = _snapshotGroupHelperConfig.EnablePerformanceMetrics;
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
            var totalStopwatch = _metricsEnabled ? Stopwatch.StartNew() : null;
            int totalMarketsProcessed = 0;
            long totalProcessingTime = 0;

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

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
                var stopwatch = _metricsEnabled ? Stopwatch.StartNew() : null;
                List<SnapshotDTO> rawSnapshots;
                totalMarketsProcessed++;

                try
                {
                    rawSnapshots = await context.GetSnapshots(marketTicker: marketTicker);
                    rawSnapshots = rawSnapshots.OrderBy(x => x.SnapshotDate).ToList();
                }
                catch (Exception ex)
                {
                    if (_metricsEnabled) Interlocked.Increment(ref _errorCount);
                    _logger.LogWarning(ex, "Failed to retrieve snapshots for market {MarketTicker}. Retrying after delay.", marketTicker);
                    if (stopwatch != null)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning("Initial failure for market {MarketTicker} in {ElapsedMs} ms.", marketTicker, stopwatch.ElapsedMilliseconds);
                        stopwatch.Restart();
                    }
                    else
                    {
                        _logger.LogWarning("Initial failure for market {MarketTicker}.", marketTicker);
                    }
                    try
                    {
                        await Task.Delay(_dataStorageConfig.RetryDelayMs);
                        rawSnapshots = await context.GetSnapshotsFiltered(marketTicker: marketTicker);
                        rawSnapshots = rawSnapshots.OrderBy(x => x.SnapshotDate).ToList();
                    }
                    catch (Exception retryEx)
                    {
                        if (_metricsEnabled) Interlocked.Increment(ref _errorCount);
                        _logger.LogError(retryEx, "Failed to retrieve snapshots for market {MarketTicker} after retry. Skipping.", marketTicker);
                        if (stopwatch != null)
                        {
                            stopwatch.Stop();
                            _logger.LogWarning("Failed to process market {MarketTicker} in {ElapsedMs} ms.", marketTicker, stopwatch.ElapsedMilliseconds);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to process market {MarketTicker}.", marketTicker);
                        }
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

                string snapshotDirectory = Path.Combine(_dataStorageConfig.HardDataStorageLocation, "Preprocessed", "SnapshotGroups");

                // Process valid snapshots into snapshot groups
                var validPeriods = await _snapshotPeriodHelper.SplitIntoValidGroups(validSnapshots, snapshotDirectory);

                await context.AddOrUpdateSnapshotGroups(validPeriods);
                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    totalProcessingTime += stopwatch.ElapsedMilliseconds;
                    if (_metricsEnabled)
                    {
                        Interlocked.Increment(ref _totalMarketsProcessed);
                        Interlocked.Add(ref _totalProcessingTimeMs, stopwatch.ElapsedMilliseconds);
                    }
                    _logger.LogInformation("Successfully processed {Count} snapshot groups for market {MarketTicker} in {ElapsedMs} ms.", validPeriods.Count, marketTicker, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    if (_metricsEnabled)
                    {
                        Interlocked.Increment(ref _totalMarketsProcessed);
                    }
                    _logger.LogInformation("Successfully processed {Count} snapshot groups for market {MarketTicker}.", validPeriods.Count, marketTicker);
                }
            }

            if (totalStopwatch != null)
            {
                totalStopwatch.Stop();
                double averageTimePerMarket = totalMarketsProcessed > 0 ? (double)totalProcessingTime / totalMarketsProcessed : 0;
                _logger.LogInformation("Completed snapshot group generation process. Total markets processed: {TotalMarkets}, Total processing time: {TotalTime} ms, Average time per market: {AverageTime} ms, Overall duration: {OverallDuration} ms.",
                    totalMarketsProcessed, totalProcessingTime, averageTimePerMarket, totalStopwatch.ElapsedMilliseconds);

                // Post metrics to central performance monitor if available
                _centralPerformanceMonitor?.RecordMarketAnalysisHelperMetrics(totalMarketsProcessed, totalProcessingTime, averageTimePerMarket, _errorCount);
            }
            else
            {
                _logger.LogInformation("Completed snapshot group generation process. Total markets processed: {TotalMarkets}.", totalMarketsProcessed);
            }
        }

        /// <summary>
        /// Gets current performance metrics for the market analysis helper.
        /// Returns processing statistics and error counts.
        /// </summary>
        /// <returns>A tuple containing total markets processed, total processing time in milliseconds, average time per market, and error count.</returns>
        public (int TotalMarketsProcessed, long TotalProcessingTimeMs, double AverageTimePerMarketMs, int ErrorCount) GetPerformanceMetrics()
        {
            if (!_metricsEnabled)
            {
                return (0, 0, 0.0, 0);
            }
            var total = _totalMarketsProcessed;
            var time = _totalProcessingTimeMs;
            var avg = total > 0 ? (double)time / total : 0.0;
            var errors = _errorCount;
            return (total, time, avg, errors);
        }
    }
}
