using BacklashBot.Configuration;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashCommon.Configuration;
using BacklashDTOs;
using BacklashDTOs.Data;
using Microsoft.Extensions.Options;

namespace BacklashBot.Management
{
    /// <summary>
    /// Main market management service that coordinates between managed and unmanaged market management strategies.
    /// Acts as a facade that delegates watch list monitoring to the appropriate implementation based on
    /// the brain instance's ManagedWatchList configuration. Provides a unified interface for market
    /// management operations while allowing different strategies for different brain configurations.
    /// Now includes configurable queue limits and extracted target calculation service for improved testability.
    /// </summary>
    public class MarketManagerService : BaseMarketManagerService
    {
        private IMarketManagerService _managedService;
        private IMarketManagerService _unmanagedService;

        /// <summary>
        /// Initializes a new instance of the MarketManagerService class.
        /// Creates instances of both managed and unmanaged market management services
        /// to provide flexible market management strategies based on brain configuration.
        /// </summary>
        /// <param name="serviceFactory">Factory for accessing various bot services</param>
        /// <param name="logger">Logger for recording market management operations</param>
        /// <param name="scopeFactory">Factory for creating service scopes</param>
        /// <param name="performanceMonitor">Monitor for tracking system performance metrics</param>
        /// <param name="instanceNameConfig">Configuration options for execution parameters</param>
        /// <param name="centralBrainConfig">Configuration options for central brain parameters</param>
        /// <param name="scopeManagerService">Service for managing dependency injection scopes</param>
        /// <param name="statusTrackerService">Service for tracking operation status and cancellation</param>
        /// <param name="brainStatus">Service providing brain instance status information</param>
        /// <param name="targetCalculationService">Service for calculating optimal market targets</param>
        public MarketManagerService(IServiceFactory serviceFactory,
            ILogger<IMarketManagerService> logger,
            IServiceScopeFactory scopeFactory,
            ICentralPerformanceMonitor performanceMonitor,
            IOptions<InstanceNameConfig> instanceNameConfig,
            IOptions<CentralBrainConfig> centralBrainConfig,
            IScopeManagerService scopeManagerService,
            IStatusTrackerService statusTrackerService,
            IBrainStatusService brainStatus,
            ITargetCalculationService targetCalculationService)
            : base(serviceFactory, logger, scopeFactory, performanceMonitor, instanceNameConfig, centralBrainConfig, scopeManagerService, statusTrackerService, brainStatus, targetCalculationService)
        {
            _managedService = new ManagedMarketManagerService(serviceFactory, logger, scopeFactory, performanceMonitor, instanceNameConfig, centralBrainConfig, scopeManagerService, statusTrackerService, brainStatus, targetCalculationService);
            _unmanagedService = new UnmanagedMarketManagerService(serviceFactory, logger, scopeFactory, performanceMonitor, instanceNameConfig, centralBrainConfig, scopeManagerService, statusTrackerService, brainStatus, targetCalculationService);
        }

        /// <summary>
        /// Clears the lists of markets to add after reset and markets to reset.
        /// This method is thread-safe using a lock to prevent concurrent modifications.
        /// </summary>
        public new void ClearMarketsToReset()
        {
            lock (_resetLock)
            {
                MarketsToAddAfterReset.Clear();
                MarketsToReset.Clear();
            }
        }


        /// <summary>
        /// Handles market resets by refreshing markets and resetting them asynchronously.
        /// Catches and logs any exceptions that occur during the reset process.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public new async Task HandleMarketResets()
        {
            try
            {
                await RefreshMarkets();
                await ResetMarkets();
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("HandleMarketResets was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to handle market resets: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Triggers a market reset for the specified market ticker by adding it to the reset list.
        /// This method is thread-safe using a lock to prevent concurrent modifications.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market to reset</param>
        public new void TriggerMarketReset(string marketTicker)
        {
            lock (_resetLock)
            {
                MarketsToReset.Add(marketTicker);
            }
        }


        /// <summary>
        /// Monitors the watch list by delegating to the appropriate market management strategy.
        /// Routes the monitoring request to either the managed or unmanaged service based on
        /// the brain instance's ManagedWatchList configuration setting.
        /// </summary>
        /// <param name="brain">The brain instance configuration containing watch list settings</param>
        /// <param name="metrics">Current performance metrics for decision making</param>
        /// <returns>A task representing the asynchronous monitoring operation</returns>
        public override async Task MonitorWatchList(BrainInstanceDTO brain, PerformanceMetrics metrics)
        {
            if (brain.ManagedWatchList)
            {
                await _managedService.MonitorWatchList(brain, metrics);
            }
            else
            {
                await _unmanagedService.MonitorWatchList(brain, metrics);
            }
        }







    }
}
