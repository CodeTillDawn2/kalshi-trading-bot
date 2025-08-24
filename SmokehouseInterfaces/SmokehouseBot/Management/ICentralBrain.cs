using Microsoft.Extensions.Hosting;

namespace SmokehouseBot.Management.Interfaces
{
    public interface ICentralBrain : IDisposable, IHostedService
    {
        bool IsServicesStopped { get; }
        bool IsStartingUp { get; }
        bool IsShuttingDown { get; }

        Task StartDashboard();
        Task ShutdownDashboardAsync();

    }
}