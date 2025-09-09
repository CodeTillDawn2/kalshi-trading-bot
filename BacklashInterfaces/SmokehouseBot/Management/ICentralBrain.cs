using Microsoft.Extensions.Hosting;

namespace BacklashBot.Management.Interfaces
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
