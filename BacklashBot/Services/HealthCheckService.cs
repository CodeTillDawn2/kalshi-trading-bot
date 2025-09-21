using BacklashBotData.Data.Interfaces;
using BacklashInterfaces.SmokehouseBot.Services;

namespace BacklashBot.Services
{
    /// <summary>
    /// Implementation of IHealthCheckService to verify dependent services are operational.
    /// </summary>
    public class HealthCheckService : IHealthCheckService
    {
        private readonly ILogger<HealthCheckService> _logger;
        private readonly IBacklashBotContext _context;

        /// <summary>
        /// Initializes a new instance of the HealthCheckService class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="context">The database context.</param>
        public HealthCheckService(ILogger<HealthCheckService> logger, IBacklashBotContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Performs health checks on dependent services.
        /// </summary>
        /// <returns>True if all health checks pass, false otherwise.</returns>
        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                _logger.LogInformation("Performing health checks on dependent services...");

                // Check database connectivity with overall timeout
                _logger.LogDebug("Starting database connectivity check...");
                var healthCheckTimeout = Task.Delay(TimeSpan.FromSeconds(30)); // Overall timeout for all health checks
                var healthCheckTask = CheckDatabaseHealthAsync();

                var completedTask = await Task.WhenAny(healthCheckTask, healthCheckTimeout);

                if (completedTask == healthCheckTimeout)
                {
                    _logger.LogError("Health check timed out after 30 seconds");
                    return false;
                }

                bool dbHealthResult = await healthCheckTask;
                if (!dbHealthResult)
                {
                    _logger.LogError("Health check failed: Database is not accessible.");
                    return false;
                }

                _logger.LogDebug("Database health check completed successfully");

                // Add more health checks here as needed (e.g., external APIs, network connectivity)

                _logger.LogInformation("All health checks passed.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during health check: {Message}", ex.Message);
                _logger.LogDebug(ex, "Health check exception details");
                return false;
            }
        }

        /// <summary>
        /// Checks the health of the database connection.
        /// </summary>
        /// <returns>True if the database is accessible, false otherwise.</returns>
        private async Task<bool> CheckDatabaseHealthAsync()
        {
            try
            {
                _logger.LogDebug("Starting database health check...");

                // Execute a simple SELECT 1 query that doesn't depend on any data existing
                // Add timeout to prevent hanging
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var healthCheckTask = _context.TestDbAsync();

                var completedTask = await Task.WhenAny(healthCheckTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogError("Database health check timed out after 10 seconds");
                    return false;
                }

                await healthCheckTask; // Ensure the task completed successfully
                _logger.LogDebug("Database health check passed - connection successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed: {Message}", ex.Message);
                return false;
            }
        }
    }
}
