using Microsoft.AspNetCore.SignalR;
using BacklashBot.Services.Interfaces;
using KalshiBotData.Data.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace BacklashBot.Middleware
{
    public class SignalRAuthenticationMiddleware : IHubFilter
    {
        private readonly IServiceFactory _serviceFactory;
        private readonly ILogger<SignalRAuthenticationMiddleware> _logger;

        public SignalRAuthenticationMiddleware(
            IServiceFactory serviceFactory,
            ILogger<SignalRAuthenticationMiddleware> logger)
        {
            _serviceFactory = serviceFactory;
            _logger = logger;
        }

        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext invocationContext,
            Func<HubInvocationContext, ValueTask<object?>> next)
        {
            // Validate client authentication for sensitive operations
            if (IsAuthenticationRequired(invocationContext.HubMethodName))
            {
                if (!await ValidateClient(invocationContext))
                {
                    _logger.LogWarning("Unauthorized SignalR method call: {Method} from {ConnectionId}",
                        invocationContext.HubMethodName, invocationContext.Context.ConnectionId);
                    throw new HubException("Authentication required");
                }
            }

            return await next(invocationContext);
        }

        public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
        {
            // Validate client on connection
            var httpContext = context.Context.GetHttpContext();
            if (httpContext != null)
            {
                var clientId = httpContext.Request.Query["clientId"].ToString();
                var authToken = httpContext.Request.Query["authToken"].ToString();

                if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(authToken))
                {
                    if (await ValidateClientCredentials(clientId, authToken))
                    {
                        // Update client connection
                        await UpdateClientConnection(clientId, context.Context.ConnectionId);
                        _logger.LogInformation("Authenticated client connected: {ClientId}", clientId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed authentication for client: {ClientId}", clientId);
                        context.Context.Abort();
                        return;
                    }
                }
                else
                {
                    _logger.LogWarning("Missing authentication parameters for connection: {ConnectionId}", context.Context.ConnectionId);
                    // Allow connection but mark as unauthenticated
                }
            }

            await next(context);
        }

        public Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
        {
            // Clean up client connection on disconnect
            _logger.LogDebug("Client disconnected: {ConnectionId}", context.Context.ConnectionId);
            return next(context, exception);
        }

        private bool IsAuthenticationRequired(string methodName)
        {
            // Define which methods require authentication
            var protectedMethods = new[]
            {
                "SubscribeToMarket",
                "UnsubscribeFromMarket",
                "CheckIn" // Though this is server-to-client, we might want to protect it
            };

            return protectedMethods.Contains(methodName);
        }

        private async Task<bool> ValidateClient(HubInvocationContext context)
        {
            // Extract client information from connection
            var httpContext = context.Context.GetHttpContext();
            if (httpContext == null) return false;

            var clientId = httpContext.Request.Query["clientId"].ToString();
            var authToken = httpContext.Request.Query["authToken"].ToString();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(authToken))
                return false;

            return await ValidateClientCredentials(clientId, authToken);
        }

        private async Task<bool> ValidateClientCredentials(string clientId, string authToken)
        {
            try
            {
                using var scope = _serviceFactory.GetScopeManager().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                var client = await context.GetSignalRClient(clientId);
                if (client == null || !client.IsActive)
                    return false;

                // Validate auth token (in production, use proper hashing)
                var expectedToken = GenerateAuthToken(client.ClientId, client.ClientName);
                if (authToken != expectedToken)
                    return false;

                // Update last seen
                await context.UpdateSignalRClientLastSeen(clientId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating client credentials for {ClientId}", clientId);
                return false;
            }
        }

        private async Task UpdateClientConnection(string clientId, string connectionId)
        {
            try
            {
                using var scope = _serviceFactory.GetScopeManager().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                await context.UpdateSignalRClientConnection(clientId, connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client connection for {ClientId}", clientId);
            }
        }

        private string GenerateAuthToken(string clientId, string clientName)
        {
            // Simple token generation - in production, use proper JWT or secure tokens
            using var sha256 = SHA256.Create();
            var input = $"{clientId}:{clientName}:{DateTime.UtcNow.Date:yyyy-MM-dd}";
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}