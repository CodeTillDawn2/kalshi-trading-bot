namespace BacklashInterfaces.SmokehouseBot.Services
{
    /// <summary>
    /// Interface for health check services to verify dependent services are operational.
    /// </summary>
    public interface IHealthCheckService
    {
        /// <summary>
        /// Performs health checks on dependent services.
        /// </summary>
        /// <returns>True if all health checks pass, false otherwise.</returns>
        Task<bool> CheckHealthAsync();
    }
}
