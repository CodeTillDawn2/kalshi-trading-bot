using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using BacklashBot.Services.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashDTOs;
using BacklashInterfaces.SmokehouseBot.Services;
using System.Net;
using System.Net.Http;

namespace BacklashBot.Services
{
    public class OverseerClientService : IOverseerClientService
    {
        private readonly ILogger<OverseerClientService> _logger;
        private readonly IServiceFactory _serviceFactory;
        private HubConnection? _hubConnection;
        private Timer? _checkInTimer;
        private bool _isConnected = false;
        private string _clientId;
        private string _clientName = "BacklashBot";
        private string _clientType = "TradingBot";
        private BacklashDTOs.Data.OverseerInfo? _currentOverseer;
        private string? _currentOverseerUrl;

        public OverseerClientService(
            ILogger<OverseerClientService> logger,
            IServiceFactory serviceFactory)
        {
            _logger = logger;
            _serviceFactory = serviceFactory;
            _clientId = Guid.NewGuid().ToString();
        }

        public async Task StartAsync()
        {
            try
            {
                // Find overseer IP from database or configuration
                var overseerUrl = await GetOverseerUrlAsync();

                if (string.IsNullOrEmpty(overseerUrl))
                {
                    _logger.LogDebug("No overseer URL found. Overseer client will not start - this is normal in testing mode.");
                    return;
                }

                _logger.LogInformation("Connecting to overseer at: {Url}", overseerUrl);

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(overseerUrl)
                    .WithAutomaticReconnect()
                    .Build();

                // Set up event handlers
                _hubConnection.On<string>("HandshakeResponse", HandleHandshakeResponse);
                _hubConnection.On<string>("CheckInResponse", HandleCheckInResponse);
                _hubConnection.On<string>("MessageResponse", HandleMessageResponse);

                // Handle connection events
                _hubConnection.Closed += HandleConnectionClosed;
                _hubConnection.Reconnected += HandleReconnected;

                try
                {
                    await _hubConnection.StartAsync();
                    _isConnected = true;

                    _logger.LogInformation("Connected to overseer successfully");

                    // Perform handshake
                    await PerformHandshakeAsync();

                    // Start CheckIn timer (every 30 seconds)
                    _checkInTimer = new Timer(async _ => await SendCheckInAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
                }
                catch (HttpRequestException httpEx) when (httpEx.Message.Contains("actively refused") || httpEx.Message.Contains("connection"))
                {
                    // Overseer is not running - this is expected and not an error
                    _logger.LogInformation("Overseer is not available at {Url}. This is optional and the bot will continue without oversight.", overseerUrl);
                    _isConnected = false;
                    // Don't throw - this is expected when overseer is not running
                }
                catch (Exception connectionEx)
                {
                    // Other connection issues - log as warning but don't fail
                    _logger.LogWarning(connectionEx, "Failed to connect to overseer at {Url}. This is optional and the bot will continue.", overseerUrl);
                    _isConnected = false;
                    // Don't throw - continue without overseer
                }
            }
            catch (Exception ex)
            {
                // General startup issues - log as information since overseer is optional
                _logger.LogInformation(ex, "Failed to initialize overseer client. This is optional and the bot will continue without oversight.");
                // Don't throw - overseer is optional
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (_checkInTimer != null)
                {
                    await _checkInTimer.DisposeAsync();
                    _checkInTimer = null;
                }

                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                }

                _isConnected = false;
                _currentOverseer = null;
                _currentOverseerUrl = null;
                _logger.LogInformation("Overseer client stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping overseer client");
            }
        }

        private async Task<string?> GetOverseerUrlAsync()
        {
            try
            {
                // 1. Try to get from configuration first
                var overseerUrl = await GetOverseerUrlFromConfigurationAsync();
                if (!string.IsNullOrEmpty(overseerUrl))
                {
                    _logger.LogInformation("Found overseer URL from configuration: {Url}", overseerUrl);
                    return overseerUrl;
                }

                // 2. Try to get from database
                overseerUrl = await GetOverseerUrlFromDatabaseAsync();
                if (!string.IsNullOrEmpty(overseerUrl))
                {
                    _logger.LogInformation("Found overseer URL from database: {Url}", overseerUrl);
                    return overseerUrl;
                }

                // 3. Try network discovery
                overseerUrl = await DiscoverOverseerViaNetworkAsync();
                if (!string.IsNullOrEmpty(overseerUrl))
                {
                    _logger.LogInformation("Discovered overseer via network: {Url}", overseerUrl);
                    return overseerUrl;
                }

                // 4. Final fallback to localhost
                _logger.LogDebug("No overseer found via configuration, database, or network discovery. Using localhost fallback for testing.");
                return "http://localhost:5000/chartHub";
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to discover overseer URL, using localhost fallback for testing");
                return "http://localhost:5000/chartHub";
            }
        }

        private async Task<string?> GetOverseerUrlFromConfigurationAsync()
        {
            try
            {
                // Check for environment variables first
                var overseerHost = Environment.GetEnvironmentVariable("OVERSEER_HOST");
                var overseerPort = Environment.GetEnvironmentVariable("OVERSEER_PORT") ?? "5000";
                var overseerUrl = Environment.GetEnvironmentVariable("OVERSEER_URL");

                if (!string.IsNullOrEmpty(overseerUrl))
                {
                    // Full URL provided
                    return overseerUrl.EndsWith("/chartHub") ? overseerUrl : $"{overseerUrl.TrimEnd('/')}/chartHub";
                }
                else if (!string.IsNullOrEmpty(overseerHost))
                {
                    // Host and port provided
                    return $"http://{overseerHost}:{overseerPort}/chartHub";
                }

                // Could also check appsettings.json here if needed
                // For example: Configuration.GetValue<string>("Overseer:Url")

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get overseer URL from configuration");
                return null;
            }
        }

        private async Task<string?> GetOverseerUrlFromDatabaseAsync()
        {
            try
            {
                using var scope = _serviceFactory.GetScopeManager().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<KalshiBotData.Data.Interfaces.IKalshiBotContext>();

                // Get active overseers from database, ordered by most recent heartbeat
                var activeOverseers = await context.GetActiveOverseers();

                if (activeOverseers.Any())
                {
                    var overseer = activeOverseers.First();
                    var url = $"http://{overseer.IPAddress}:{overseer.Port}/chartHub";

                    // Store current overseer information for validation during check-ins
                    _currentOverseer = overseer;
                    _currentOverseerUrl = url;

                    // Check for multiple active overseers (this shouldn't happen)
                    if (activeOverseers.Count > 1)
                    {
                        _logger.LogWarning("Multiple active overseers detected! This should not happen. Active overseers:");
                        foreach (var activeOverseer in activeOverseers)
                        {
                            _logger.LogWarning("  - {HostName} at {IPAddress}:{Port} (Heartbeat: {Heartbeat})",
                                activeOverseer.HostName, activeOverseer.IPAddress, activeOverseer.Port,
                                activeOverseer.LastHeartbeat?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never");
                        }
                    }

                    _logger.LogInformation("Found active overseer in database: {HostName} at {IPAddress}:{Port}",
                        overseer.HostName, overseer.IPAddress, overseer.Port);
                    return url;
                }

                _logger.LogDebug("No active overseers found in database - this is normal in testing mode");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get overseer URL from database - this is normal in testing mode");
                return null;
            }
        }

        private async Task<string?> DiscoverOverseerViaNetworkAsync()
        {
            try
            {
                // Simple network discovery - try common ports on local network
                var commonPorts = new[] { 5000, 5001, 8080, 3000 };
                var localIP = GetLocalIPAddress();

                if (string.IsNullOrEmpty(localIP))
                {
                    return null;
                }

                // Get subnet (e.g., 192.168.1.0/24)
                var subnet = GetSubnet(localIP);

                // Try to connect to potential overseer instances
                foreach (var port in commonPorts)
                {
                    var potentialUrl = $"http://{subnet}.1:{port}/chartHub";
                    if (await TestOverseerConnectionAsync(potentialUrl))
                    {
                        return potentialUrl;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to discover overseer via network");
                return null;
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private string GetSubnet(string ipAddress)
        {
            try
            {
                var parts = ipAddress.Split('.');
                if (parts.Length == 4)
                {
                    return $"{parts[0]}.{parts[1]}.{parts[2]}";
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<bool> TestOverseerConnectionAsync(string url)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(2);

                // Try multiple endpoints to test if overseer is available
                var testUrls = new[]
                {
                    url.Replace("/chartHub", "/health"),
                    url.Replace("/chartHub", "/"),
                    url.Replace("/chartHub", ""),
                    url
                };

                foreach (var testUrl in testUrls)
                {
                    try
                    {
                        var response = await client.GetAsync(testUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // Continue to next URL
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task PerformHandshakeAsync()
        {
            if (_hubConnection == null || !_isConnected) return;

            try
            {
                await _hubConnection.InvokeAsync("Handshake", _clientId, _clientName, _clientType);
                _logger.LogInformation("Handshake sent to overseer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send handshake");
            }
        }

        private async Task ValidateCurrentOverseerAsync()
        {
            if (_currentOverseer == null || string.IsNullOrEmpty(_currentOverseerUrl))
            {
                return; // No current overseer to validate
            }

            try
            {
                using var scope = _serviceFactory.GetScopeManager().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<KalshiBotData.Data.Interfaces.IKalshiBotContext>();

                // Get the most recent active overseer
                var activeOverseers = await context.GetActiveOverseers();

                if (!activeOverseers.Any())
                {
                    _logger.LogDebug("No active overseers found in database during validation - this is normal in testing mode");
                    return;
                }

                var mostRecentOverseer = activeOverseers.First();

                // Check if we're still connected to the most recent overseer
                if (_currentOverseer.Id != mostRecentOverseer.Id)
                {
                    _logger.LogWarning("Connected to outdated overseer! Current: {CurrentHost} ({CurrentHeartbeat}), Most Recent: {RecentHost} ({RecentHeartbeat})",
                        _currentOverseer.HostName,
                        _currentOverseer.LastHeartbeat?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never",
                        mostRecentOverseer.HostName,
                        mostRecentOverseer.LastHeartbeat?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never");

                    // TODO: Implement automatic reconnection to newer overseer if desired
                    // For now, just log the warning
                }

                // Check for multiple active overseers (this shouldn't happen)
                if (activeOverseers.Count > 1)
                {
                    _logger.LogWarning("Multiple active overseers detected during check-in! This should not happen:");
                    foreach (var overseer in activeOverseers)
                    {
                        var isCurrent = overseer.Id == _currentOverseer.Id ? " (CURRENT)" : "";
                        _logger.LogWarning("  - {HostName} at {IPAddress}:{Port} (Heartbeat: {Heartbeat}){CurrentFlag}",
                            overseer.HostName, overseer.IPAddress, overseer.Port,
                            overseer.LastHeartbeat?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never",
                            isCurrent);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to validate current overseer connection - this is normal in testing mode");
            }
        }

        private async Task SendCheckInAsync()
        {
            if (_hubConnection == null || !_isConnected)
            {
                _logger.LogDebug("Overseer not connected, skipping CheckIn");
                return;
            }

            try
            {
                // Validate that we're still connected to the most recent overseer
                await ValidateCurrentOverseerAsync();

                var markets = await _serviceFactory.GetMarketDataService().FetchWatchedMarketsAsync();
                var errorHandler = _serviceFactory.GetBacklashErrorHandler();

                var checkInData = new
                {
                    Markets = markets,
                    ErrorCount = errorHandler.ErrorCount,
                    LastSnapshot = errorHandler.LastSuccessfulSnapshot == DateTime.MinValue
                        ? (DateTime?)null
                        : errorHandler.LastSuccessfulSnapshot
                };

                await _hubConnection.InvokeAsync("CheckIn", checkInData);
                _logger.LogDebug("CheckIn sent: {MarketCount} markets, ErrorCount: {ErrorCount}",
                    markets.Count, errorHandler.ErrorCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send CheckIn to overseer. This is optional and will be retried.");
            }
        }

        private void HandleHandshakeResponse(string response)
        {
            _logger.LogInformation("Handshake response received: {Response}", response);
        }

        private void HandleCheckInResponse(string response)
        {
            _logger.LogDebug("CheckIn response received: {Response}", response);
        }

        private void HandleMessageResponse(string response)
        {
            _logger.LogInformation("Message response received: {Response}", response);
        }

        private Task HandleConnectionClosed(Exception? exception)
        {
            _isConnected = false;
            _logger.LogWarning(exception, "Connection to overseer closed");

            // Could implement reconnection logic here
            return Task.CompletedTask;
        }

        private Task HandleReconnected(string? connectionId)
        {
            _isConnected = true;
            _logger.LogInformation("Reconnected to overseer with connection ID: {ConnectionId}", connectionId);

            // Re-perform handshake on reconnection
            Task.Run(async () => await PerformHandshakeAsync());

            return Task.CompletedTask;
        }
    }
}