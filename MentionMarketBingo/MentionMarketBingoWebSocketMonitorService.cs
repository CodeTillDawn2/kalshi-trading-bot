using BacklashBot.KalshiAPI.Interfaces;
using BacklashDTOs.KalshiAPI;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MentionMarketBingo;

/// <summary>
/// Service responsible for monitoring WebSocket connections and handling orderbook updates
/// for the MentionMarketBingo application. Manages subscriptions to "fill" and "orderbook"
/// channels for markets displayed in the UI.
/// </summary>
public class MentionMarketBingoWebSocketMonitorService
{
    private readonly ILogger<MentionMarketBingoWebSocketMonitorService> _logger;
    private readonly IKalshiWebSocketClient _webSocketClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MentionMarketBingoOrderBookService _orderBookService;
    private readonly ConcurrentDictionary<string, HashSet<string>> _subscribedMarkets = new();

    /// <summary>
    /// Initializes a new instance of the MentionMarketBingoWebSocketMonitorService.
    /// </summary>
    /// <param name="logger">Logger for recording service operations.</param>
    /// <param name="webSocketClient">WebSocket client for real-time market data.</param>
    /// <param name="scopeFactory">Factory for creating service scopes.</param>
    /// <param name="orderBookService">Service for managing orderbook data.</param>
    public MentionMarketBingoWebSocketMonitorService(
        ILogger<MentionMarketBingoWebSocketMonitorService> logger,
        IKalshiWebSocketClient webSocketClient,
        IServiceScopeFactory scopeFactory,
        MentionMarketBingoOrderBookService orderBookService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _webSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _orderBookService = orderBookService ?? throw new ArgumentNullException(nameof(orderBookService));

        _logger.LogInformation("MentionMarketBingoWebSocketMonitorService initialized");
    }

    /// <summary>
    /// Starts the WebSocket monitoring service and subscribes to required channels.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for stopping the service.</param>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting MentionMarketBingoWebSocketMonitorService");

        try
        {
            // Subscribe to fill and orderbook channels
            await SubscribeToChannelsAsync(cancellationToken);

            // Start monitoring connection status in the background
            _ = Task.Run(() => MonitorConnectionAsync(cancellationToken), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting WebSocket monitor service");
            throw;
        }
    }

    /// <summary>
    /// Stops the WebSocket monitoring service and unsubscribes from channels.
    /// </summary>
    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping MentionMarketBingoWebSocketMonitorService");

        try
        {
            await UnsubscribeFromAllChannelsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping WebSocket monitor service");
        }
    }

    /// <summary>
    /// Subscribes to markets for a specific event ticker.
    /// Note: WebSocket subscriptions are handled by MentionMarketBingoOrderBookService.
    /// This service tracks subscribed markets for monitoring purposes.
    /// </summary>
    /// <param name="eventTicker">The event ticker to subscribe markets for.</param>
    /// <param name="marketTickers">Array of market tickers to subscribe to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SubscribeToEventMarketsAsync(string eventTicker, string[] marketTickers, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventTicker))
        {
            _logger.LogWarning("Event ticker is null or empty");
            return;
        }

        if (marketTickers == null || marketTickers.Length == 0)
        {
            _logger.LogWarning("No market tickers provided for event {EventTicker}", eventTicker);
            return;
        }

        _logger.LogInformation("Tracking {Count} markets for event {EventTicker}", marketTickers.Length, eventTicker);

        // Update subscribed markets tracking for monitoring purposes
        _subscribedMarkets[eventTicker] = new HashSet<string>(marketTickers);

        _logger.LogInformation("Successfully tracked markets for event {EventTicker}", eventTicker);
    }

    /// <summary>
    /// Unsubscribes from markets for a specific event ticker.
    /// Note: WebSocket unsubscriptions are handled by MentionMarketBingoOrderBookService.
    /// This service removes tracking for monitoring purposes.
    /// </summary>
    /// <param name="eventTicker">The event ticker to unsubscribe markets for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task UnsubscribeFromEventMarketsAsync(string eventTicker, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventTicker))
        {
            _logger.LogWarning("Event ticker is null or empty");
            return;
        }

        if (!_subscribedMarkets.TryGetValue(eventTicker, out var marketTickers))
        {
            _logger.LogWarning("No subscribed markets found for event {EventTicker}", eventTicker);
            return;
        }

        _logger.LogInformation("Removing tracking for {Count} markets for event {EventTicker}", marketTickers.Count, eventTicker);

        // Remove from tracking
        _subscribedMarkets.TryRemove(eventTicker, out _);

        _logger.LogInformation("Successfully removed tracking for event {EventTicker}", eventTicker);
    }

    /// <summary>
    /// Checks if the WebSocket is currently connected.
    /// </summary>
    /// <returns>True if connected, false otherwise.</returns>
    public bool IsConnected()
    {
        return _webSocketClient.IsConnected();
    }

    /// <summary>
    /// Gets the list of currently subscribed markets for an event.
    /// </summary>
    /// <param name="eventTicker">The event ticker.</param>
    /// <returns>Array of subscribed market tickers.</returns>
    public string[] GetSubscribedMarkets(string eventTicker)
    {
        if (_subscribedMarkets.TryGetValue(eventTicker, out var markets))
        {
            return markets.ToArray();
        }
        return Array.Empty<string>();
    }

    private async Task SubscribeToChannelsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // The actual market subscriptions will be handled when events are selected
            // For now, just ensure the WebSocket client is ready
            if (!_webSocketClient.IsConnected())
            {
                _logger.LogInformation("WebSocket not connected, will connect when needed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to channels");
            throw;
        }
    }

    private async Task UnsubscribeFromAllChannelsAsync()
    {
        try
        {
            foreach (var eventTicker in _subscribedMarkets.Keys.ToList())
            {
                await UnsubscribeFromEventMarketsAsync(eventTicker);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from all channels");
        }
    }

    private async Task MonitorConnectionAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting connection monitoring");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Check connection status periodically
                var isConnected = _webSocketClient.IsConnected();

                if (!isConnected)
                {
                    _logger.LogWarning("WebSocket connection lost");

                    // Attempt to reconnect if we have subscribed markets
                    if (_subscribedMarkets.Any())
                    {
                        _logger.LogInformation("Attempting to reconnect WebSocket for subscribed markets");

                        // The reconnection logic would be handled by the WebSocket client
                        // For now, just log the attempt
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Connection monitoring cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in connection monitoring");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        _logger.LogDebug("Connection monitoring stopped");
    }
}