using SmokehouseDTOs;
using SmokehouseDTOs.Data;

namespace SmokehouseBot.Management.Interfaces
{
    public interface IMarketManagerService
    {
        void ClearMarketsToReset();
        Task HandleMarketResets();
        void TriggerMarketReset(string marketTicker);
        Task MonitorWatchList(BrainInstanceDTO brain, PerformanceMetrics performanceMetrics);
    }
}