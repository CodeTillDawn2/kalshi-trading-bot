using BacklashDTOs;
using BacklashDTOs.Data;

namespace BacklashBot.Management.Interfaces
{
    public interface IMarketManagerService
    {
        void ClearMarketsToReset();
        Task HandleMarketResets();
        void TriggerMarketReset(string marketTicker);
        Task MonitorWatchList(BrainInstanceDTO brain, PerformanceMetrics performanceMetrics);
    }
}
