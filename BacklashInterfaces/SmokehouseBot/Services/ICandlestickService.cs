using BacklashDTOs;

namespace BacklashBot.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages candlestick data operations,
    /// including updating, populating, and retrieving historical candlestick information for markets.
    /// </summary>
    public interface ICandlestickService
    {
        /// <summary>
        /// Updates candlestick data asynchronously for the specified market ticker.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market to update candlesticks for.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateCandlesticksAsync(string marketTicker);

        /// <summary>
        /// Populates market data asynchronously for the specified market ticker,
        /// ensuring all necessary candlestick data is available.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market to populate data for.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PopulateMarketDataAsync(string marketTicker);

        /// <summary>
        /// Retrieves historical candlestick data for the specified market ticker and timeframe.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market to retrieve data for.</param>
        /// <param name="timeframe">The timeframe for the candlestick data (e.g., "1m", "5m").</param>
        /// <returns>A list of candlestick data objects for the specified market and timeframe.</returns>
        List<CandlestickData> RetrieveHistoricalCandlesticksAsync(string marketTicker, string timeframe);
    }
}
