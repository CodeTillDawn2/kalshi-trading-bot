using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using BacklashBot.Services.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashBot.Configuration;
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
        private Timer? _overseerDiscoveryTimer;
        private bool _isConnected = false;
        private string _clientId;
        private string _clientName = "BacklashBot";
        private string _clientType = "TradingBot";
        private BacklashDTOs.Data.OverseerInfo? _currentOverseer;
        private string? _currentOverseerUrl;
        private DateTime _lastOverseerDiscovery = DateTime.MinValue;
        private readonly TimeSpan _overseerDiscoveryInterval = TimeSpan.FromMinutes(3); // Check for better overseer every 3 minutes

        public OverseerClientService(
            ILogger<OverseerClientService> logger,
            IServiceFactory serviceFactory,
            IOptions<ExecutionConfig> executionConfig)
        {
            _logger = logger;
            _serviceFactory = serviceFactory;
            _clientId = Guid.NewGuid().ToString();

            // Read brain instance name from configuration, fallback to hardcoded if not set
            _clientName = executionConfig.Value.BrainInstance ?? "BacklashBot";
            _logger.LogInformation("OVERSEER- Using brain instance name: {BrainInstanceName}", _clientName);
        }

        public async Task StartAsync()
        {
            try
            {
                _logger.LogInformation("OVERSEER- Starting OverseerClientService...");

                // Start Overseer Discovery timer first (every 3 minutes) - this ensures we keep trying to find overseer
                _overseerDiscoveryTimer = new Timer(async _ => await PerformOverseerDiscoveryAsync(), null, TimeSpan.Zero, _overseerDiscoveryInterval);
                _logger.LogInformation("OVERSEER- Overseer discovery timer started (every {Interval} minutes)", _overseerDiscoveryInterval.TotalMinutes);

                // Start CheckIn timer (every 30 seconds) - it will only send if connected
                _checkInTimer = new Timer(async _ => await SendCheckInAsync(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
                _logger.LogInformation("OVERSEER- CheckIn timer started (every 30 seconds, first check in 10 seconds)");

                // Find overseer IP from database or configuration
                var overseerUrl = await GetOverseerUrlAsync();

                if (!string.IsNullOrEmpty(overseerUrl))
                {
                    _logger.LogInformation("OVERSEER- Attempting initial connection to overseer at: {Url}", overseerUrl);
                    await AttemptConnectionAsync(overseerUrl, "Initial Startup");
                }
                else
                {
                    _logger.LogInformation("OVERSEER- No overseer URL found initially. Discovery will continue in background every 3 minutes.");
                }
            }
            catch (Exception ex)
            {
                // General startup issues - log as information since overseer is optional
                _logger.LogError(ex, "OVERSEER- Failed to initialize overseer client. This is optional and the bot will continue without oversight.");
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

                if (_overseerDiscoveryTimer != null)
                {
                    await _overseerDiscoveryTimer.DisposeAsync();
                    _overseerDiscoveryTimer = null;
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
                _logger.LogInformation("OVERSEER- Overseer client stopped");
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
                    _logger.LogInformation("OVERSEER- Found overseer URL from configuration: {Url}", overseerUrl);
                    return overseerUrl;
                }

                // 2. Try to get from database
                overseerUrl = await GetOverseerUrlFromDatabaseAsync();
                if (!string.IsNullOrEmpty(overseerUrl))
                {
                    _logger.LogInformation("OVERSEER- Found overseer URL from database: {Url}", overseerUrl);
                    return overseerUrl;
                }

                // 3. Try network discovery
                overseerUrl = await DiscoverOverseerViaNetworkAsync();
                if (!string.IsNullOrEmpty(overseerUrl))
                {
                    _logger.LogInformation("OVERSEER- Discovered overseer via network: {Url}", overseerUrl);
                    return overseerUrl;
                }

                // 4. No overseer found - return null to let discovery continue
                _logger.LogInformation("OVERSEER- No overseer found via configuration, database, or network discovery. Discovery will continue in background.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "OVERSEER- Failed to discover overseer URL");
                return null;
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

                // Check appsettings.json configuration
                try
                {
                    // Try to get configuration from IConfiguration if available
                    // For now, we'll use a simple approach to read from appsettings.local.json
                    var configUrl = await GetOverseerUrlFromAppSettingsAsync();
                    if (!string.IsNullOrEmpty(configUrl))
                    {
                        _logger.LogInformation("OVERSEER- Found overseer URL in appsettings: {Url}", configUrl);
                        return configUrl;
                    }
                }
                catch (Exception configEx)
                {
                    _logger.LogDebug(configEx, "OVERSEER- Failed to read overseer URL from appsettings");
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "OVERSEER- Failed to get overseer URL from configuration");
                return null;
            }
        }

        private Task<string?> GetOverseerUrlFromAppSettingsAsync()
        {
            try
            {
                // Check if there's an OVERSEER_URL environment variable that might be set from appsettings
                var configUrl = Environment.GetEnvironmentVariable("OVERSEER_URL");
                if (!string.IsNullOrEmpty(configUrl))
                {
                    return Task.FromResult(configUrl.EndsWith("/chartHub") ? configUrl : $"{configUrl.TrimEnd('/')}/chartHub");
                }

                return Task.FromResult<string?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "OVERSEER- Failed to get overseer URL from appsettings");
                return Task.FromResult<string?>(null);
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
                        _logger.LogInformation("OVERSEER- Multiple active overseers detected. Active overseers:");
                        foreach (var activeOverseer in activeOverseers)
                        {
                            _logger.LogInformation("OVERSEER-   - {HostName} at {IPAddress}:{Port} (Heartbeat: {Heartbeat})",
                                activeOverseer.HostName, activeOverseer.IPAddress, activeOverseer.Port,
                                activeOverseer.LastHeartbeat?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never");
                        }
                    }

                    _logger.LogInformation("OVERSEER- Found active overseer in database: {HostName} at {IPAddress}:{Port}",
                        overseer.HostName, overseer.IPAddress, overseer.Port);
                    return url;
                }

                _logger.LogDebug("OVERSEER- No active overseers found in database - this is normal in testing mode");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("OVERSEER- Failed to get overseer URL from database - this is normal in testing mode. Error: {Error}", ex.Message);
                return null;
            }
        }

        private async Task<string?> DiscoverOverseerViaNetworkAsync()
        {
            try
            {
                _logger.LogDebug("OVERSEER- Starting network discovery for overseer...");

                // Simple network discovery - try common ports on local network
                var commonPorts = new[] { 5000, 5001, 8080, 3000 };
                var localIP = GetLocalIPAddress();

                if (string.IsNullOrEmpty(localIP))
                {
                    _logger.LogDebug("OVERSEER- Could not determine local IP address for network discovery");
                    return null;
                }

                _logger.LogDebug("OVERSEER- Local IP: {LocalIP}", localIP);

                // Get subnet (e.g., 192.168.1.0/24)
                var subnet = GetSubnet(localIP);
                if (string.IsNullOrEmpty(subnet))
                {
                    _logger.LogDebug("OVERSEER- Could not determine subnet from local IP: {LocalIP}", localIP);
                    return null;
                }

                _logger.LogDebug("OVERSEER- Scanning subnet: {Subnet}.*", subnet);

                // Try to connect to potential overseer instances on the subnet
                // Start from .100 and go down to .2, then try .1
                for (int i = 100; i >= 1; i--)
                {
                    if (i == 1) continue; // Skip .1 as it's usually the gateway

                    foreach (var port in commonPorts)
                    {
                        var potentialUrl = $"http://{subnet}.{i}:{port}/chartHub";
                        _logger.LogDebug("OVERSEER- Testing potential overseer at: {Url}", potentialUrl);

                        if (await TestOverseerConnectionAsync(potentialUrl))
                        {
                            _logger.LogInformation("OVERSEER- Found overseer via network discovery at: {Url}", potentialUrl);
                            return potentialUrl;
                        }
                    }
                }

                // Note: We don't try localhost fallback here as we want to rely on the database discovery system
                // The KalshiBotOverseer should register itself in the database when it starts

                _logger.LogDebug("OVERSEER- Network discovery completed - no overseer found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("OVERSEER- Failed to discover overseer via network. Error: {Error}", ex.Message);
                return null;
            }
        }

        private string? GetLocalIPAddress()
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

        private string? GetSubnet(string ipAddress)
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
                client.Timeout = TimeSpan.FromSeconds(3); // Increased timeout for better reliability

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
                        _logger.LogTrace("OVERSEER- Testing connection to: {Url}", testUrl);
                        var response = await client.GetAsync(testUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogDebug("OVERSEER- Connection test successful for: {Url} (Status: {StatusCode})", testUrl, response.StatusCode);
                            return true;
                        }
                        else
                        {
                            _logger.LogTrace("OVERSEER- Connection test failed for: {Url} (Status: {StatusCode})", testUrl, response.StatusCode);
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        _logger.LogTrace("OVERSEER- HTTP error testing {Url}: {Error}", testUrl, httpEx.Message);
                        // Continue to next URL
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.LogTrace("OVERSEER- Timeout testing {Url}", testUrl);
                        // Continue to next URL
                    }
                    catch (Exception ex)
                    {
                        _logger.LogTrace("OVERSEER- Error testing {Url}: {Error}", testUrl, ex.Message);
                        // Continue to next URL
                    }
                }

                _logger.LogTrace("OVERSEER- All connection tests failed for base URL: {Url}", url);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogTrace("OVERSEER- Unexpected error during connection test for {Url}: {Error}", url, ex.Message);
                return false;
            }
        }

        private async Task AttemptConnectionAsync(string overseerUrl, string reason)
        {
            try
            {
                _logger.LogInformation("OVERSEER- Attempting connection to overseer at {Url} (Reason: {Reason})", overseerUrl, reason);

                // Check and clean up existing connection
                if (_hubConnection != null)
                {
                    var currentState = _hubConnection.State.ToString();
                    _logger.LogDebug("OVERSEER- Current HubConnection state: {State}", currentState);

                    // Only dispose if not already disposed
                    if (_hubConnection.State.ToString() != "Disconnected")
                    {
                        try
                        {
                            await _hubConnection.StopAsync();
                            _logger.LogDebug("OVERSEER- Stopped existing connection");
                        }
                        catch (Exception stopEx)
                        {
                            _logger.LogWarning("OVERSEER- Error stopping existing connection: {Error}", stopEx.Message);
                        }
                    }

                    // Always dispose and create new connection to ensure clean state
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                    _logger.LogDebug("OVERSEER- Disposed existing HubConnection");
                }

                // Reset connection state
                _isConnected = false;
                _currentOverseerUrl = null;

                // Create new connection
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(overseerUrl)
                    .WithAutomaticReconnect()
                    .Build();

                _logger.LogDebug("OVERSEER- Created new HubConnection, initial state: {State}", _hubConnection.State.ToString());

                // Set up event handlers
                _hubConnection.On<string>("HandshakeResponse", HandleHandshakeResponse);
                _hubConnection.On<string>("CheckInResponse", HandleCheckInResponse);
                _hubConnection.On<string>("MessageResponse", HandleMessageResponse);

                // Handle connection events
                _hubConnection.Closed += HandleConnectionClosed;
                _hubConnection.Reconnected += HandleReconnected;

                // Verify connection is in correct state before starting
                if (_hubConnection.State.ToString() != "Disconnected")
                {
                    _logger.LogWarning("OVERSEER- HubConnection is in {State} state, cannot start. Creating fresh connection.", _hubConnection.State.ToString());
                    await _hubConnection.DisposeAsync();
                    _hubConnection = new HubConnectionBuilder()
                        .WithUrl(overseerUrl)
                        .WithAutomaticReconnect()
                        .Build();

                    // Re-setup event handlers
                    _hubConnection.On<string>("HandshakeResponse", HandleHandshakeResponse);
                    _hubConnection.On<string>("CheckInResponse", HandleCheckInResponse);
                    _hubConnection.On<string>("MessageResponse", HandleMessageResponse);
                    _hubConnection.Closed += HandleConnectionClosed;
                    _hubConnection.Reconnected += HandleReconnected;
                }

                _logger.LogDebug("OVERSEER- Starting HubConnection...");
                await _hubConnection.StartAsync();
                _logger.LogDebug("OVERSEER- HubConnection started, state: {State}", _hubConnection.State.ToString());

                _isConnected = true;
                _currentOverseerUrl = overseerUrl;

                _logger.LogInformation("OVERSEER- Successfully connected to overseer at {Url}", overseerUrl);

                // Perform handshake
                await PerformHandshakeAsync();
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("actively refused") || httpEx.Message.Contains("connection"))
            {
                // Overseer is not running or network unreachable
                _logger.LogInformation("OVERSEER- Overseer is not available at {Url}. Will retry during next discovery cycle. Error: {Error}", overseerUrl, httpEx.Message);
                _isConnected = false;
                _currentOverseerUrl = null;
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("503") || httpEx.Message.Contains("Service Unavailable"))
            {
                // Service is running but temporarily unavailable
                _logger.LogWarning("OVERSEER- Overseer at {Url} is temporarily unavailable (503 Service Unavailable). This may indicate the service is starting up or overloaded. Will retry sooner. Error: {Error}", overseerUrl, httpEx.Message);
                _isConnected = false;
                _currentOverseerUrl = null;

                // Schedule an immediate retry for 503 errors
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(30)); // Wait 30 seconds then retry
                    if (!_isConnected)
                    {
                        _logger.LogInformation("OVERSEER- Retrying connection to {Url} after 503 error", overseerUrl);
                        await AttemptConnectionAsync(overseerUrl, "503 Retry");
                    }
                });
            }
            catch (InvalidOperationException invalidOpEx) when (invalidOpEx.Message.Contains("cannot be started if it is not in the Disconnected state"))
            {
                // HubConnection state issue - this is the error we're seeing
                _logger.LogWarning("OVERSEER- HubConnection state error for {Url}. This usually indicates a previous connection wasn't properly cleaned up. Error: {Error}", overseerUrl, invalidOpEx.Message);
                _isConnected = false;
                _currentOverseerUrl = null;

                // Force cleanup and retry
                if (_hubConnection != null)
                {
                    try
                    {
                        await _hubConnection.DisposeAsync();
                        _hubConnection = null;
                        _logger.LogDebug("OVERSEER- Force disposed HubConnection due to state error");
                    }
                    catch (Exception disposeEx)
                    {
                        _logger.LogWarning("OVERSEER- Error force disposing HubConnection: {Error}", disposeEx.Message);
                    }
                }

                // Retry after a short delay
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    if (!_isConnected)
                    {
                        _logger.LogInformation("OVERSEER- Retrying connection to {Url} after state error", overseerUrl);
                        await AttemptConnectionAsync(overseerUrl, "State Error Retry");
                    }
                });
            }
            catch (Exception connectionEx)
            {
                // Other connection issues - log as information since overseer is optional
                _logger.LogInformation("OVERSEER- Failed to connect to overseer at {Url}. Will retry during next discovery cycle. Error: {Error}", overseerUrl, connectionEx.Message);
                _isConnected = false;
                _currentOverseerUrl = null;
            }
        }

        private async Task PerformHandshakeAsync()
        {
            if (!IsConnectionActive() || _hubConnection == null) return;

            try
            {
                await _hubConnection.InvokeAsync("Handshake", _clientId, _clientName, _clientType);
                _logger.LogInformation("OVERSEER- Handshake sent to overseer");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("cannot be called if the connection is not active"))
            {
                _logger.LogWarning("OVERSEER- Connection became inactive during handshake attempt. Will mark as disconnected. Error: {Error}", ex.Message);
                _isConnected = false;
            }
            catch (Exception ex)
            {
                _logger.LogError("OVERSEER- Failed to send handshake. Error: {Error}", ex.Message);
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
                    _logger.LogDebug("OVERSEER- No active overseers found in database during validation - this is normal in testing mode");
                    return;
                }

                var mostRecentOverseer = activeOverseers.First();

                // Check if we're still connected to the most recent overseer
                if (_currentOverseer.Id != mostRecentOverseer.Id)
                {
                    _logger.LogInformation("OVERSEER- Found newer overseer available. Current: {CurrentHost} ({CurrentHeartbeat}), Available: {RecentHost} ({RecentHeartbeat})",
                        _currentOverseer.HostName,
                        _currentOverseer.LastHeartbeat?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never",
                        mostRecentOverseer.HostName,
                        mostRecentOverseer.LastHeartbeat?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never");

                    // Attempt to switch to the better overseer
                    await SwitchToBetterOverseerAsync(mostRecentOverseer);
                }

                // Check for multiple active overseers (this shouldn't happen)
                if (activeOverseers.Count > 1)
                {
                    _logger.LogInformation("OVERSEER- Multiple active overseers detected during validation:");
                    foreach (var overseer in activeOverseers)
                    {
                        var isCurrent = overseer.Id == _currentOverseer.Id ? " (CURRENT)" : "";
                        _logger.LogInformation("OVERSEER-   - {HostName} at {IPAddress}:{Port} (Heartbeat: {Heartbeat}){CurrentFlag}",
                            overseer.HostName, overseer.IPAddress, overseer.Port,
                            overseer.LastHeartbeat?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never",
                            isCurrent);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("OVERSEER- Failed to validate current overseer connection - this is normal in testing mode. Error: {Error}", ex.Message);
            }
        }

        private async Task PerformOverseerDiscoveryAsync()
        {
            try
            {
                _logger.LogDebug("OVERSEER- Performing overseer discovery cycle...");

                // Skip if we recently performed discovery
                if (DateTime.UtcNow - _lastOverseerDiscovery < _overseerDiscoveryInterval)
                {
                    _logger.LogDebug("OVERSEER- Skipping discovery - too soon since last attempt ({Seconds} seconds ago)",
                        (DateTime.UtcNow - _lastOverseerDiscovery).TotalSeconds);
                    return;
                }

                _lastOverseerDiscovery = DateTime.UtcNow;

                // If we're not connected, try to find any overseer
                if (!_isConnected || _currentOverseer == null)
                {
                    _logger.LogInformation("OVERSEER- No overseer connection detected, attempting to discover and connect to available overseer");
                    await DiscoverAndConnectToBestOverseerAsync();
                    return;
                }

                // If we're connected, check for better overseers
                _logger.LogDebug("OVERSEER- Validating current overseer connection...");
                await ValidateCurrentOverseerAsync();

                // Also perform network discovery in case there are overseers not in the database
                var networkOverseerUrl = await DiscoverOverseerViaNetworkAsync();
                if (!string.IsNullOrEmpty(networkOverseerUrl) && networkOverseerUrl != _currentOverseerUrl)
                {
                    _logger.LogInformation("OVERSEER- Discovered potential overseer via network: {Url}", networkOverseerUrl);
                    // Test the connection and switch if it's better
                    if (await TestOverseerConnectionAsync(networkOverseerUrl))
                    {
                        _logger.LogInformation("OVERSEER- Network-discovered overseer is available, attempting to switch from {CurrentUrl}", _currentOverseerUrl);
                        await AttemptConnectionAsync(networkOverseerUrl, "Network Discovery");
                    }
                    else
                    {
                        _logger.LogDebug("OVERSEER- Network-discovered overseer at {Url} is not responding, keeping current connection", networkOverseerUrl);
                    }
                }
                else
                {
                    _logger.LogDebug("OVERSEER- No new overseers discovered via network");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("OVERSEER- Overseer discovery failed - this is normal and will be retried on next cycle. Error: {Error}", ex.Message);
            }
        }

        private async Task DiscoverAndConnectToBestOverseerAsync()
        {
            try
            {
                _logger.LogDebug("OVERSEER- Starting overseer discovery process...");

                // Try database first
                var databaseUrl = await GetOverseerUrlFromDatabaseAsync();
                if (!string.IsNullOrEmpty(databaseUrl))
                {
                    _logger.LogInformation("OVERSEER- Found overseer in database: {Url}, attempting connection", databaseUrl);
                    await AttemptConnectionAsync(databaseUrl, "Database Discovery");
                    return;
                }

                // Try network discovery
                var networkUrl = await DiscoverOverseerViaNetworkAsync();
                if (!string.IsNullOrEmpty(networkUrl))
                {
                    _logger.LogInformation("OVERSEER- Found overseer via network discovery: {Url}, attempting connection", networkUrl);
                    await AttemptConnectionAsync(networkUrl, "Network Discovery");
                    return;
                }

                // Try configuration
                var configUrl = await GetOverseerUrlFromConfigurationAsync();
                if (!string.IsNullOrEmpty(configUrl))
                {
                    _logger.LogInformation("OVERSEER- Found overseer in configuration: {Url}, attempting connection", configUrl);
                    await AttemptConnectionAsync(configUrl, "Configuration Discovery");
                    return;
                }

                // No overseer found - this is normal and will be retried on next discovery cycle
                _logger.LogDebug("OVERSEER- Overseer discovery completed - no active overseer found in database, will retry on next cycle");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("OVERSEER- Failed during overseer discovery process. Will retry on next cycle. Error: {Error}", ex.Message);
            }
        }

        private async Task SwitchToBetterOverseerAsync(BacklashDTOs.Data.OverseerInfo newOverseer)
        {
            try
            {
                var newUrl = $"http://{newOverseer.IPAddress}:{newOverseer.Port}/chartHub";

                // Don't switch if we're already connected to this overseer
                if (_currentOverseerUrl == newUrl)
                {
                    return;
                }

                _logger.LogInformation("OVERSEER- Switching to better overseer: {HostName} at {IPAddress}:{Port}",
                    newOverseer.HostName, newOverseer.IPAddress, newOverseer.Port);

                await SwitchToOverseerUrlAsync(newUrl, $"Better Overseer: {newOverseer.HostName}");
            }
            catch (Exception ex)
            {
                _logger.LogDebug("OVERSEER- Failed to switch to better overseer - will retry on next discovery cycle. Error: {Error}", ex.Message);
            }
        }

        private async Task SwitchToOverseerUrlAsync(string newUrl, string reason)
        {
            await AttemptConnectionAsync(newUrl, reason);
        }

        private bool IsConnectionActive()
        {
            return _hubConnection != null &&
                   _isConnected &&
                   _hubConnection.State == HubConnectionState.Connected;
        }

        private async Task SendCheckInAsync()
        {
            if (!IsConnectionActive() || _hubConnection == null)
            {
                _logger.LogDebug("OVERSEER- Overseer not connected or connection not active, skipping CheckIn (will retry on next cycle)");
                return;
            }

            try
            {
                _logger.LogDebug("OVERSEER- Preparing CheckIn data...");

                // Validate that we're still connected to the most recent overseer
                await ValidateCurrentOverseerAsync();

                var markets = await _serviceFactory.GetMarketDataService().FetchWatchedMarketsAsync();
                var errorHandler = _serviceFactory.GetBacklashErrorHandler();

                var checkInData = new
                {
                    BrainInstanceName = _clientName, // Add the brain instance name
                    Markets = markets,
                    ErrorCount = errorHandler.ErrorCount,
                    LastSnapshot = errorHandler.LastSuccessfulSnapshot == DateTime.MinValue
                        ? (DateTime?)null
                        : errorHandler.LastSuccessfulSnapshot,
                    LastErrorDate = errorHandler.LastErrorDate == DateTime.MinValue
                        ? (DateTime?)null
                        : errorHandler.LastErrorDate,
                    IsStartingUp = false, // Could be determined from service state
                    IsShuttingDown = false,
                    WatchPositions = true, // From configuration
                    WatchOrders = true,
                    ManagedWatchList = true,
                    CaptureSnapshots = false,
                    TargetWatches = 200, // From configuration
                    MinimumInterest = 5.0,
                    UsageMin = 70.0,
                    UsageMax = 90.0,
                    CurrentCpuUsage = 0.0, // Could get from system
                    EventQueueAvg = 0.0,
                    TickerQueueAvg = 0.0,
                    NotificationQueueAvg = 0.0,
                    OrderbookQueueAvg = 0.0,
                    IsWebSocketConnected = true // Could be determined
                };

                _logger.LogDebug("OVERSEER- Sending CheckIn to overseer: {MarketCount} markets, ErrorCount: {ErrorCount}, LastSnapshot: {LastSnapshot}",
                    markets.Count, errorHandler.ErrorCount, checkInData.LastSnapshot);

                await _hubConnection.InvokeAsync("CheckIn", checkInData);
                _logger.LogInformation("OVERSEER- CheckIn sent successfully to overseer at {Url}", _currentOverseerUrl);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("cannot be called if the connection is not active"))
            {
                _logger.LogWarning("OVERSEER- Connection became inactive during CheckIn attempt. Will mark as disconnected and retry. Error: {Error}", ex.Message);
                _isConnected = false; // Mark as disconnected so discovery can try to reconnect
            }
            catch (Exception ex)
            {
                _logger.LogWarning("OVERSEER- Failed to send CheckIn to overseer. Connection may have been lost - will retry. Error: {Error}", ex.Message);
                _isConnected = false; // Mark as disconnected so discovery can try to reconnect
            }
        }

        private void HandleHandshakeResponse(string response)
        {
            _logger.LogInformation("OVERSEER- Handshake response received from overseer: {Response}", response);
        }

        private void HandleCheckInResponse(string response)
        {
            _logger.LogInformation("OVERSEER- CheckIn response received from overseer: {Response}", response);
        }

        private void HandleMessageResponse(string response)
        {
            _logger.LogInformation("OVERSEER- Message response received from overseer: {Response}", response);
        }

        private Task HandleConnectionClosed(Exception? exception)
        {
            _isConnected = false;
            if (exception != null)
            {
                _logger.LogWarning("OVERSEER- Connection to overseer closed unexpectedly. Automatic reconnection will be attempted. Error: {Error}", exception.Message);
            }
            else
            {
                _logger.LogInformation("OVERSEER- Connection to overseer closed gracefully");
            }

            return Task.CompletedTask;
        }

        private Task HandleReconnected(string? connectionId)
        {
            _isConnected = true;
            _logger.LogInformation("OVERSEER- Successfully reconnected to overseer with connection ID: {ConnectionId}", connectionId);

            // Re-perform handshake on reconnection
            _ = Task.Run(async () =>
            {
                try
                {
                    await PerformHandshakeAsync();
                    _logger.LogInformation("OVERSEER- Handshake completed after reconnection");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("OVERSEER- Failed to perform handshake after reconnection. Error: {Error}", ex.Message);
                }
            });

            return Task.CompletedTask;
        }
    }
}