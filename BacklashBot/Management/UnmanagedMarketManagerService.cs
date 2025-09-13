using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Options;
using BacklashDTOs.Configuration;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
using BacklashInterfaces.Constants;
using BacklashBot.State.Interfaces;
using TradingStrategies.Configuration;

namespace BacklashBot.Management
{
    /// <summary>
    /// Implements unmanaged market management strategy for the Kalshi trading bot.
    /// Uses fixed target watch counts rather than dynamic calculation based on performance metrics.
    /// Focuses on maintaining a stable number of markets while removing ended markets and
    /// replacing uninteresting markets with higher-interest alternatives. Includes input validation
    /// for brain configuration parameters with warning logs for invalid values.
    /// </summary>
    public class UnmanagedMarketManagerService : BaseMarketManagerService
    {

        /// <summary>
        /// Initializes a new instance of the UnmanagedMarketManagerService class.
        /// Sets up the unmanaged market management strategy with all required dependencies.
        /// </summary>
        /// <param name="serviceFactory">Factory for accessing various bot services</param>
        /// <param name="logger">Logger for recording market management operations</param>
        /// <param name="scopeFactory">Factory for creating service scopes</param>
        /// <param name="performanceMonitor">Monitor for tracking system performance metrics</param>
        /// <param name="executionConfig">Configuration options for execution parameters</param>
        /// <param name="tradingConfig">Configuration options for trading parameters</param>
        /// <param name="scopeManagerService">Service for managing dependency injection scopes</param>
        /// <param name="statusTrackerService">Service for tracking operation status and cancellation</param>
        /// <param name="brainStatus">Service providing brain instance status information</param>
        /// <param name="targetCalculationService">Service for calculating optimal market targets</param>
        public UnmanagedMarketManagerService(IServiceFactory serviceFactory,
            ILogger<IMarketManagerService> logger,
            IServiceScopeFactory scopeFactory,
            ICentralPerformanceMonitor performanceMonitor,
            IOptions<ExecutionConfig> executionConfig,
            IOptions<TradingConfig> tradingConfig,
            IScopeManagerService scopeManagerService,
            IStatusTrackerService statusTrackerService,
            IBrainStatusService brainStatus,
            ITargetCalculationService targetCalculationService)
            : base(serviceFactory, logger, scopeFactory, performanceMonitor, executionConfig, tradingConfig, scopeManagerService, statusTrackerService, brainStatus, targetCalculationService)
        {
        }


        /// <summary>
        /// Monitors and manages the watch list using unmanaged strategy with fixed target counts.
        /// Performs comprehensive market management including removing ended markets, checking for
        /// market settlement stability, and adjusting market counts based on fixed targets rather
        /// than dynamic performance calculations. Handles both adding high-interest markets and
        /// removing uninteresting ones to maintain optimal market coverage.
        /// </summary>
        /// <param name="brain">The brain instance configuration containing target watch counts and settings</param>
        /// <param name="metrics">Current performance metrics for decision making</param>
        /// <returns>A task representing the asynchronous monitoring operation</returns>
        public override async Task MonitorWatchList(BrainInstanceDTO brain, PerformanceMetrics metrics)
        {
            // Validate brain configuration parameters
            if (brain == null)
            {
                _logger.LogWarning("Brain configuration is null, skipping watch list monitoring");
                MonitoringWatchList = false;
                return;
            }

            if (brain.TargetWatches < 0)
            {
                _logger.LogWarning("Invalid TargetWatches in brain configuration: {TargetWatches}. Must be non-negative", brain.TargetWatches);
                MonitoringWatchList = false;
                return;
            }

            if (brain.UsageTarget <= 0)
            {
                _logger.LogWarning("Invalid UsageTarget in brain configuration: {UsageTarget}. Must be positive", brain.UsageTarget);
                MonitoringWatchList = false;
                return;
            }

            if (MonitoringWatchList) return;
            MonitoringWatchList = true;
            try
            {
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                int removedCount = await RemoveEndedMarkets(context);
                if (removedCount > 0)
                    _logger.LogInformation("BRAIN: Removed {Count} markets due to them being ended.", removedCount);

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var token = cts.Token;

                if (_performanceMonitor.LastPerformanceSampleDate == null)
                {
                    _logger.LogInformation("Stats: No performance metric available for MarketRefreshService.");
                    MonitoringWatchList = false;
                    return;
                }

                var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
                var marketDataService = _serviceFactory.GetMarketDataService();
                if (marketDataService == null)
                {
                    MonitoringWatchList = false;
                    return;
                }

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                var watchedMarkets = await marketDataService.FetchWatchedMarketsAsync();
                int actualMarketCount = watchedMarkets.Count();

                double maxDif = watchedMarkets.Count() * .05;

                if ((Math.Abs(metrics.CurrentCount - watchedMarkets.Count()) <= maxDif) && metrics.CurrentUsage > 0)
                {
                    _recentMarketAdjustment = false;
                    _logger.LogDebug("Stats: Markets settled, reset adjustment flag.");
                }

                if (_recentMarketAdjustment
                    || (Math.Abs(metrics.CurrentCount - watchedMarkets.Count()) > maxDif)
                    || MarketsToAddAfterReset.Count() > 0)
                {
                    _logger.LogDebug("Stats: Waiting for markets to settle. Percentage={Percentage:F2}%, CurrentCount={CurrentCount}, ActualCount={ActualCount}, RefreshUsage={RefreshUsage}, QueueUsage={QueueUsage}, MarketsToAddAfterReset={MarketsToAdd}, RecentAdjustment={RecentAdjustment}",
                        metrics.CurrentUsage, metrics.CurrentCount, actualMarketCount, Math.Round(metrics.CurrentUsage, 2), _performanceMonitor.GetQueueHighCountPercentage(), MarketsToAddAfterReset.Count(), _recentMarketAdjustment);
                    MonitoringWatchList = false;
                    return;
                }

                int actualTarget = brain.TargetWatches;
                brain.TargetWatches = actualTarget;
                await context.AddOrUpdateBrainInstance(brain);

                _recentMarketAdjustment = true;
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                if (actualTarget < actualMarketCount)
                {
                    int toRemove = actualMarketCount - actualTarget;
                    _logger.LogInformation("BRAIN: Usage is too high - attempting to remove up to {ToRemove} markets.", toRemove);

                    int removed = await RemoveLowestInterestMarkets(context, apiService, brain, toRemove, token);
                    _logger.LogInformation("BRAIN: Usage too high: {Percentage:F2}%. Removed {Count} markets to target {ActualTarget} markets",
                        metrics.CurrentUsage, removed, actualTarget);
                }
                else if (actualTarget > actualMarketCount)
                {
                    BrainInstanceDTO? dto = await context.GetBrainInstance(_executionConfig.BrainInstance ?? "");
                    _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
                    List<string> addedMarkets = await AddHighInterestMarkets(context, apiService, actualTarget - actualMarketCount, dto?.MinimumInterest ?? 0);
                    var dataCache = _serviceFactory.GetDataCache();
                    if (addedMarkets.Count > 0 && dataCache?.WatchedMarkets != null)
                    {
                        _logger.LogInformation("BRAIN: Usage too low: {Percentage:F2}% with {ActualMarketCount} markets. Added up to {Count} markets.",
                            metrics.CurrentUsage, actualMarketCount, actualTarget - actualMarketCount);
                        foreach (var ticker in addedMarkets)
                            _logger.LogInformation("BRAIN: Added {Market} to watch list during Monitor Watch List", ticker);
                    }
                }
                else
                {
                    BrainInstanceDTO? dto = await context.GetBrainInstance(_executionConfig.BrainInstance ?? "");
                    int removed = await RemoveUninterestingMarkets(context, apiService, brain, dto?.MinimumInterest ?? 0);
                    if (removed > 0)
                    {
                        List<string> addedMarkets = await AddHighInterestMarkets(context, apiService, removed, dto?.MinimumInterest ?? 0);
                        _logger.LogInformation("BRAIN: In unmanaged mode, replaced {Removed} uninteresting markets with {Added} high interest markets.", removed, addedMarkets.Count);
                    }
                }
                _firstWatchUpdate = false;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("MonitorWatchList was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to monitor watch list");
            }
            MonitoringWatchList = false;
        }







    }
}