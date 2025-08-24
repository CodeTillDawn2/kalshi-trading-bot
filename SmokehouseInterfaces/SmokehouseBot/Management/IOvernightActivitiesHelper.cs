using Microsoft.Extensions.DependencyInjection;

namespace SmokehouseBot.Management.Interfaces
{
    public interface IOvernightActivitiesHelper
    {
        Task RunOvernightTasks(IServiceScopeFactory scopeFactory, CancellationToken token);
        Task DeleteUnrecordedMarkets(IServiceScopeFactory scopeFactory, CancellationToken token);
        Task DeleteProcessedSnapshots(IServiceScopeFactory scopeFactory, CancellationToken token);
    }

}