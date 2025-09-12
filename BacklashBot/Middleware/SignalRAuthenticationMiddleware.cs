using Microsoft.AspNetCore.SignalR;
using BacklashBot.Services.Interfaces;
using KalshiBotData.Data.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace BacklashBot.Middleware
{
    /// <summary>
    /// Middleware for authenticating SignalR clients in the BacklashBot trading system.
    /// This middleware intercepts SignalR method invocations and connection events to validate client credentials,
    /// ensuring only authenticated clients can access sensitive hub methods. It integrates with the database
    /// to verify client tokens and manage connection state, providing a secure layer for real-time communication
    /// between trading clients and the server.
    /// </summary>
    public class SignalRAuthenticationMiddleware : IHubFilter
    {
        private readonly IServiceFactory _serviceFactory;
        private readonly ILogger<SignalRAuthenticationMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the SignalRAuthenticationMiddleware class.
        /// </summary>
        /// <param name="serviceFactory">Factory for creating service instances, used to access database context and other services.</param>
        /// <param name="logger">Logger instance for recording authentication events and errors.</param>
        public SignalRAuthenticationMiddleware(
            IServiceFactory serviceFactory,
            ILogger<SignalRAuthenticationMiddleware> logger)
        {
            _serviceFactory = serviceFactory;
            _logger = logger;
        }

        /// <summary>
        /// Intercepts SignalR method invocations to enforce authentication for protected hub methods.
        /// Checks if the invoked method requires authentication and validates the client's credentials
        /// before allowing the method to proceed. If authentication fails, throws an exception
        /// to prevent unauthorized access.
        /// </summary>
        /// <param name="invocationContext">Context containing information about the hub method being invoked.</param>
        /// <param name="next">Delegate to the next middleware or hub method in the pipeline.</param>
        /// <returns>The result of the hub method invocation.</returns>
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

        /// <summary>
        /// Handles client connection events in the SignalR pipeline.
        /// Attempts to authenticate the client using query parameters and updates the database
        /// with the connection information. If authentication fails, the connection is aborted.
        /// If no authentication parameters are provided, the connection is allowed but marked as unauthenticated.
        /// </summary>
        /// <param name="context">Context containing information about the connection event.</param>
        /// <param name="next">Delegate to the next middleware or hub in the pipeline.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Handles client disconnection events in the SignalR pipeline.
        /// Logs the disconnection for monitoring purposes and allows the pipeline to continue.
        /// </summary>
        /// <param name="context">Context containing information about the disconnection event.</param>
        /// <param name="exception">Exception that caused the disconnection, if any.</param>
        /// <param name="next">Delegate to the next middleware or hub in the pipeline.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
        {
            // Clean up client connection on disconnect
            _logger.LogInformation("Client disconnected: {ConnectionId}", context.Context.ConnectionId);
            return next(context, exception);
        }

        /// <summary>
        /// Determines whether a specific hub method requires client authentication.
        /// Only methods that perform sensitive operations or modify system state require authentication.
        /// </summary>
        /// <param name="methodName">The name of the hub method being invoked.</param>
        /// <returns>True if the method requires authentication, false otherwise.</returns>
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

        /// <summary>
        /// Validates a client's authentication credentials during a hub method invocation.
        /// Extracts client ID and auth token from the HTTP context query parameters
        /// and verifies them against the database.
        /// </summary>
        /// <param name="context">The hub invocation context containing connection information.</param>
        /// <returns>True if the client is authenticated, false otherwise.</returns>
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

        /// <summary>
        /// Validates client credentials against the database and generates expected auth token for comparison.
        /// Retrieves the client record, checks if it's active, generates the expected token using the same algorithm,
        /// and updates the client's last seen timestamp upon successful validation.
        /// </summary>
        /// <param name="clientId">The unique identifier of the client.</param>
        /// <param name="authToken">The authentication token provided by the client.</param>
        /// <returns>True if credentials are valid and client is active, false otherwise.</returns>
        private async Task<bool> ValidateClientCredentials(string clientId, string authToken)
        {
            try
            {
                using var scope = _serviceFactory.GetScopeManager().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                var client = await context.GetSignalRClient(clientId);
                if (client == null || !client.IsActive)
                    return false;

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

        /// <summary>
        /// Updates the database with the current SignalR connection ID for a client.
        /// This ensures the system knows which connection belongs to which client for targeted messaging.
        /// </summary>
        /// <param name="clientId">The unique identifier of the client.</param>
        /// <param name="connectionId">The SignalR connection ID to associate with the client.</param>
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

        /// <summary>
        /// Generates an authentication token for a client using SHA256 hashing.
        /// The token is based on client ID, client name, and the current UTC date to ensure
        /// tokens are valid for the current day only, requiring periodic renewal.
        /// </summary>
        /// <param name="clientId">The unique identifier of the client.</param>
        /// <param name="clientName">The name of the client.</param>
        /// <returns>A base64-encoded SHA256 hash serving as the authentication token.</returns>
        private string GenerateAuthToken(string clientId, string clientName)
        {
            using var sha256 = SHA256.Create();
            var input = $"{clientId}:{clientName}:{DateTime.UtcNow.Date:yyyy-MM-dd}";
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}