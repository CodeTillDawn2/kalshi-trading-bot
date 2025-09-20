using BacklashDTOs;
using BacklashDTOs.Data;

namespace BacklashBot.Management.Interfaces
{
/// <summary>IMarketManagerService</summary>
/// <summary>IMarketManagerService</summary>
    public interface IMarketManagerService
/// <summary>HandleMarketResets</summary>
/// <summary>ClearMarketsToReset</summary>
    {
/// <summary>TriggerMarketReset</summary>
        void ClearMarketsToReset();
        Task HandleMarketResets();
        void TriggerMarketReset(string marketTicker);
        Task MonitorWatchList(BrainInstanceDTO brain, PerformanceMetrics performanceMetrics);
    }
}
