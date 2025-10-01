using BacklashBot.State.Interfaces;
using BacklashInterfaces.Constants;
using BacklashInterfaces.PerformanceMetrics;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KalshiBotAPI.Websockets
{
    /// <summary>
    /// Subscription manager specifically for the Overseer application.
    /// Only subscribes to fill and lifecycle channels, unlike the main bot which subscribes to all market channels.
    /// </summary>
    public class OverseerSubscriptionManager : BaseSubscriptionManager
    {
        /// <summary>
        /// Initializes a new instance of the OverseerSubscriptionManager with required dependencies.
        /// </summary>
        /// <param name="logger">Logger for recording subscription activities and errors.</param>
        /// <param name="connectionManager">Manages WebSocket connection lifecycle and communication.</param>
        /// <param name="statusTrackerService">Provides system status and cancellation token management.</param>
        /// <param name="config">Configuration options for subscription manager settings.</param>
        /// <param name="performanceMetrics">Optional service for posting performance metrics.</param>
        public OverseerSubscriptionManager(
            ILogger<BaseSubscriptionManager> logger,
            IWebSocketConnectionManager connectionManager,
            IStatusTrackerService statusTrackerService,
            IOptions<SubscriptionManagerConfig> config,
            IPerformanceMonitor performanceMetrics)
            : base(logger, connectionManager, statusTrackerService, config, performanceMetrics)
        {
        }

        /// <summary>
        /// Subscribes to watched markets for Overseer-specific channels only.
        /// The Overseer only needs fill and lifecycle channels, not orderbook, ticker, or trade channels.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task SubscribeToWatchedMarketsAsync()
        {
            try
            {
                _processingCancellationToken.ThrowIfCancellationRequested();
                if (!_connectionManager.IsConnected())
                {
                    _logger.LogWarning("WebSocket not connected, cannot subscribe to watched markets");
                    return;
                }

                if (!WatchedMarkets.Any())
                {
                    _logger.LogDebug("No watched markets to subscribe to");
                    return;
                }

                _logger.LogInformation("Subscribing to watched markets for Overseer: {Markets}", string.Join(", ", WatchedMarkets));

                // Overseer only subscribes to fill and lifecycle channels
                // Note: lifecycle subscription covers both market and event lifecycle events
                var overseerChannels = new[] {
                    KalshiConstants.ScriptType_Feed_Fill,
                    KalshiConstants.ScriptType_Feed_Lifecycle
                };

                foreach (var action in overseerChannels)
                {
                    _processingCancellationToken.ThrowIfCancellationRequested();
                    var marketsToSubscribe = WatchedMarkets
                        .Where(m => !IsSubscribed(m, action) && CanSubscribeToMarket(m, GetChannelName(action)))
                        .ToArray();
                    if (marketsToSubscribe.Any())
                    {
                        _logger.LogDebug("Subscribing to {Action} for markets: {Markets}", action, string.Join(", ", marketsToSubscribe));
                        await SubscribeToChannelAsync(action, marketsToSubscribe);
                    }
                    else
                    {
                        _logger.LogDebug("No new markets to subscribe for {Action}", action);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("SubscribeToWatchedMarketsAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to watched markets");
            }
        }
    }
}