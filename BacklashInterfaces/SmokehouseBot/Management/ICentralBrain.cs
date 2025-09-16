using Microsoft.Extensions.Hosting;

namespace BacklashBot.Management.Interfaces
{
/// <summary>ICentralBrain</summary>
/// <summary>ICentralBrain</summary>
    public interface ICentralBrain : IDisposable, IHostedService
/// <summary>Gets or sets the IsStartingUp.</summary>
/// <summary>Gets or sets the IsServicesStopped.</summary>
    {
/// <summary>StartDashboard</summary>
/// <summary>Gets or sets the IsShuttingDown.</summary>
        bool IsServicesStopped { get; }
/// <summary>StartDashboard</summary>
        bool IsStartingUp { get; }
        bool IsShuttingDown { get; }

        Task StartDashboard();
        Task ShutdownDashboardAsync();

    }
}
