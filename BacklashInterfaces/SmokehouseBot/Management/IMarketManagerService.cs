using BacklashDTOs;
using BacklashDTOs.Data;

namespace BacklashBot.Management.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages market operations including resets,
    /// watch list monitoring, and market state management within the trading bot.
    /// </summary>
    public interface IMarketManagerService
    {
        /// <summary>
        /// Clears the list of markets that are scheduled for reset.
        /// </summary>
        void ClearMarketsToReset();

        /// <summary>
        /// Handles the reset operations for markets that have been scheduled for reset.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HandleMarketResets();

        /// <summary>
        /// Triggers a market reset for the specified market ticker.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market to reset.</param>
        void TriggerMarketReset(string marketTicker);

        /// <summary>
        /// Monitors the watch list for the specified brain instance and updates performance metrics.
        /// </summary>
        /// <param name="brain">The brain instance DTO containing configuration and state.</param>
        /// <param name="performanceMetrics">The performance metrics to update during monitoring.</param>
        /// <returns>A task representing the asynchronous monitoring operation.</returns>
        Task MonitorWatchList(BrainInstanceDTO brain, PerformanceMetrics performanceMetrics);
    }
}
