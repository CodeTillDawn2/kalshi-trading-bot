using BacklashBot.Configuration;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashBotData.Data.Interfaces;
using BacklashCommon.Configuration;
using BacklashDTOs;
using BacklashDTOs.Data;
using Microsoft.Extensions.Options;

namespace BacklashBot.Management
{
    /// <summary>
    /// Implements managed market management strategy for the Kalshi trading bot.
    /// Uses dynamic target calculation based on performance metrics rather than fixed counts.
    /// Automatically adjusts market watch counts based on system usage, queue depths, and
    /// performance indicators to optimize resource utilization and market coverage.
    /// Includes input validation for brain configuration parameters with warning logs for invalid values.
    /// </summary>
    public class ManagedMarketManagerService : BaseMarketManagerService
    {

        /// <summary>
        /// Initializes a new instance of the ManagedMarketManagerService class.
        /// Sets up the managed market management strategy with all required dependencies.
        /// </summary>
        /// <param name="serviceFactory">Factory for accessing various bot services</param>
        /// <param name="logger">Logger for recording market management operations</param>
        /// <param name="scopeFactory">Factory for creating service scopes</param>
        /// <param name="performanceMonitor">Monitor for tracking system performance metrics</param>
        /// <param name="instanceName">Configuration options for execution parameters</param>
        /// <param name="centralBrainConfig">Configuration options for central brain parameters</param>
        /// <param name="scopeManagerService">Service for managing dependency injection scopes</param>
        /// <param name="statusTrackerService">Service for tracking operation status and cancellation</param>
        /// <param name="brainStatus">Service providing brain instance status information</param>
        /// <param name="targetCalculationService">Service for calculating optimal market targets</param>
        public ManagedMarketManagerService(IServiceFactory serviceFactory,
            ILogger<IMarketManagerService> logger,
            IServiceScopeFactory scopeFactory,
            ICentralPerformanceMonitor performanceMonitor,
            IOptions<InstanceNameConfig> instanceName,
            IOptions<CentralBrainConfig> centralBrainConfig,
            IScopeManagerService scopeManagerService,
            IStatusTrackerService statusTrackerService,
            IBrainStatusService brainStatus,
            ITargetCalculationService targetCalculationService)
            : base(serviceFactory, logger, scopeFactory, performanceMonitor, instanceName, centralBrainConfig, scopeManagerService, statusTrackerService, brainStatus, targetCalculationService)
        {
        }




        /// <summary>
        /// Monitors and manages the watch list using managed strategy with dynamic target calculation.
        /// Uses performance metrics to dynamically calculate optimal market counts rather than fixed targets.
        /// Automatically adjusts market watch counts based on system usage patterns, queue depths,
        /// and performance indicators to maintain optimal resource utilization and market coverage.
        /// </summary>
        /// <param name="brain">The brain instance configuration containing usage limits and settings</param>
        /// <param name="metrics">Current performance metrics for dynamic target calculation</param>
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

            if (brain.UsageTarget <= 0)
            {
                _logger.LogWarning("Invalid UsageTarget in brain configuration: {UsageTarget}. Must be positive", brain.UsageTarget);
                MonitoringWatchList = false;
                return;
            }

            if (brain.UsageMin < 0 || brain.UsageMax <= brain.UsageMin)
            {
                _logger.LogWarning("Invalid UsageMin/UsageMax in brain configuration: Min={UsageMin}, Max={UsageMax}. Min must be non-negative and Max must be greater than Min", brain.UsageMin, brain.UsageMax);
                MonitoringWatchList = false;
                return;
            }

            if (MonitoringWatchList) return;
            MonitoringWatchList = true;
            try
            {
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
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

                int actualTarget = CalculateTarget(metrics, brain);
                brain.TargetWatches = actualTarget;
                await context.AddOrUpdateBrainInstance(brain);

                _recentMarketAdjustment = true;
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                if (actualTarget < actualMarketCount && metrics.CurrentUsage > brain.UsageMax)
                {
                    int toRemove = actualMarketCount - actualTarget;
                    _logger.LogInformation("BRAIN: Usage is too high - attempting to remove up to {ToRemove} markets.", toRemove);

                    int removed = await RemoveLowestInterestMarkets(context, apiService, brain, toRemove, token);
                    _logger.LogInformation("BRAIN: Usage too high: {Percentage:F2}%. Removed {Count} markets to target {ActualTarget} markets",
                        metrics.CurrentUsage, removed, actualTarget);
                }
                else if (actualTarget > actualMarketCount && metrics.CurrentUsage < brain.UsageMin)
                {
                    BrainInstanceDTO? dto = await context.GetBrainInstance(_instanceNameConfig.Name ?? "");
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
                    BrainInstanceDTO? dto = await context.GetBrainInstance(_instanceNameConfig.Name ?? "");
                    _logger.LogInformation("BRAIN: Usage within acceptable range: {Percentage:F2}%. Checking for uninteresting markets.", metrics.CurrentUsage);
                    int removed = await RemoveUninterestingMarkets(context, apiService, brain, dto?.MinimumInterest ?? 0);
                    _logger.LogInformation("BRAIN: Removed {Removed} uninteresting markets.", removed);
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
