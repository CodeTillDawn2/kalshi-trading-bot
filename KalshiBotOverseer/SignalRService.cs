using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace KalshiBotOverseer
{
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
                _logger.LogError(ex, "Error starting SignalR connection.");
            }
        }

        public async Task StopAsync()
        {
            try
            {
                await _connection.StopAsync();
                _logger.LogInformation("SignalR connection stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping SignalR connection.");
            }
        }

        public async Task SendMessageAsync(string method, string message)
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync(method, message);
                _logger.LogInformation("Sent message: {Message}", message);
            }
            else
            {
                _logger.LogWarning("Cannot send message: SignalR connection not established.");
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