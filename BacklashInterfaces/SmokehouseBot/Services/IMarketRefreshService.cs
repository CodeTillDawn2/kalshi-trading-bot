namespace BacklashBot.Services.Interfaces
{
/// <summary>IMarketRefreshService</summary>
/// <summary>IMarketRefreshService</summary>
    public interface IMarketRefreshService
/// <summary>TriggerImmediateRefreshAsync</summary>
/// <summary>ExecuteServicesAsync</summary>
    {
/// <summary>IsRunning</summary>
        void ExecuteServicesAsync(CancellationToken stoppingToken);
/// <summary>Gets or sets the LastWorkMarketCount.</summary>
        Task TriggerImmediateRefreshAsync(string marketTicker);
/// <summary>Gets or sets the AverageRefreshTimePerMarket.</summary>
/// <summary>Gets or sets the LastWorkDuration.</summary>
        bool IsRunning();
/// <summary>Gets or sets the LastMemoryUsage.</summary>
/// <summary>Gets or sets the TotalRefreshOperations.</summary>
        Task StopAsync(CancellationToken cancellationToken);
/// <summary>Gets or sets the LastRefreshCount.</summary>

/// <summary>Gets or sets the LastMemoryUsage.</summary>
        TimeSpan LastWorkDuration { get; }
        int LastWorkMarketCount { get; }
        long TotalRefreshOperations { get; }
        TimeSpan AverageRefreshTimePerMarket { get; }
        int LastRefreshCount { get; }
        TimeSpan LastCpuTime { get; }
        long LastMemoryUsage { get; }
        double RefreshThroughput { get; }
    }
}
