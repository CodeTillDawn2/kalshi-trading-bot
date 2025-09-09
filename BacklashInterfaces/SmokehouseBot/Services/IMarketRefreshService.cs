namespace BacklashBot.Services.Interfaces
{
    public interface IMarketRefreshService
    {
        void ExecuteServicesAsync(CancellationToken stoppingToken);
        Task TriggerImmediateRefreshAsync(string marketTicker);
        bool IsRunning();
        Task StopAsync(CancellationToken cancellationToken);

        TimeSpan LastWorkDuration { get; }
        int LastWorkMarketCount { get; }
    }
}
