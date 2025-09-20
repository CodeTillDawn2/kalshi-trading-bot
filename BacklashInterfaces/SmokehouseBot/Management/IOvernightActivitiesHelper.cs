using Microsoft.Extensions.DependencyInjection;

namespace BacklashBot.Management.Interfaces
{
/// <summary>IOvernightActivitiesHelper</summary>
/// <summary>IOvernightActivitiesHelper</summary>
    public interface IOvernightActivitiesHelper
/// <summary>DeleteUnrecordedMarkets</summary>
/// <summary>RunOvernightTasks</summary>
    {
/// <summary>DeleteProcessedSnapshots</summary>
        Task RunOvernightTasks(IServiceScopeFactory scopeFactory, CancellationToken token);
        Task DeleteUnrecordedMarkets(IServiceScopeFactory scopeFactory, CancellationToken token);
        Task DeleteProcessedSnapshots(IServiceScopeFactory scopeFactory, CancellationToken token);
    }

}
