using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace KalshiBotOverseer
{
    /// <summary>
    /// Optional SignalR service for real-time communication.
    /// If the hub is not available, the service will log warnings but continue to function.
    /// </summary>
    public class SignalRService
    {
        private readonly HubConnection _connection;
        private readonly ILogger<SignalRService> _logger;

        public SignalRService(string hubUrl, ILogger<SignalRService> logger)
        {
            _logger = logger;
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _connection.On<string>("ReceiveMessage", (message) =>
            {
                _logger.LogInformation("Received message: {Message}", message);
                // Handle received message
            });

            _connection.On<string>("ReceiveError", (error) =>
            {
                _logger.LogError("Received error: {Error}", error);
            });
        }

        public async Task StartAsync()
        {
            try
            {
                await _connection.StartAsync();
                _logger.LogInformation("SignalR connection started.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR connection failed. This is optional and the application will continue without real-time messaging. Error: {Message}", ex.Message);
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (_connection.State != HubConnectionState.Disconnected)
                {
                    await _connection.StopAsync();
                    _logger.LogInformation("SignalR connection stopped.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping SignalR connection: {Message}", ex.Message);
            }
        }

        public async Task SendMessageAsync(string method, string message)
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync(method, message);
                    _logger.LogInformation("Sent message: {Message}", message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send SignalR message: {Message}", ex.Message);
                }
            }
            else
            {
                _logger.LogDebug("SignalR not connected. Message not sent: {Message}", message);
            }
        }

        public async Task SubscribeToMarketAsync(string marketTicker)
        {
            await SendMessageAsync("SubscribeToMarket", marketTicker);
        }

        public async Task UnsubscribeFromMarketAsync(string marketTicker)
        {
            await SendMessageAsync("UnsubscribeFromMarket", marketTicker);
        }
    }
}