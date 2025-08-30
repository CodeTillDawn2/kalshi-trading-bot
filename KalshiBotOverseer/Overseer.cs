// New class: EventSubscriber.cs (place in appropriate namespace, e.g., SmokehouseBot.Services)
using System;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.Logging;
using SmokehouseDTOs; // Assuming logging is used

namespace SmokehouseBot.Services
{
    public class Overseer
    {
        private readonly IKalshiWebSocketClient _webSocketClient;
        private readonly ILogger<Overseer> _logger; // Optional, for logging events

        public Overseer(IKalshiWebSocketClient webSocketClient, ILogger<Overseer> logger)
        {
            _webSocketClient = webSocketClient;
            _logger = logger;
        }

        public void Start()
        {
            // Subscribe to the specific events
            _webSocketClient.FillReceived += OnFillReceived;
            _webSocketClient.MarketLifecycleReceived += OnMarketLifecycleReceived;
            _webSocketClient.EventLifecycleReceived += OnEventLifecycleReceived;

            _logger?.LogInformation("Subscribed to Fill, MarketLifecycle, and EventLifecycle events.");
        }

        private void OnFillReceived(object sender, FillEventArgs e)
        {
            // Handle the fill event (e.g., log or process)
            _logger?.LogInformation("Received Fill event: {EventData}", e); // Placeholder handling
        }

        private void OnMarketLifecycleReceived(object sender, MarketLifecycleEventArgs e)
        {
            // Handle the market lifecycle event
            _logger?.LogInformation("Received MarketLifecycle event: {EventData}", e);
        }

        private void OnEventLifecycleReceived(object sender, EventLifecycleEventArgs e)
        {
            // Handle the event lifecycle event
            _logger?.LogInformation("Received EventLifecycle event: {EventData}", e);
        }
    }
}