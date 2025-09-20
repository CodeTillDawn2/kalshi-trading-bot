using BacklashDTOs.KalshiAPI;

namespace BacklashBot.KalshiAPI.Interfaces
{
    /// <summary>
    /// Interface for interacting with the Kalshi API to fetch market data, positions, orders, and other trading information.
    /// </summary>
    public interface IKalshiAPIService
    {
        /// <summary>
        /// Fetches markets from the Kalshi API based on specified filters.
        /// </summary>
        /// <param name="eventTicker">Optional event ticker filter.</param>
        /// <param name="seriesTicker">Optional series ticker filter.</param>
        /// <param name="maxCloseTs">Optional maximum close timestamp filter.</param>
        /// <param name="minCloseTs">Optional minimum close timestamp filter.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="tickers">Optional array of tickers to filter by.</param>
        /// <param name="updateNotFoundToClosed">Whether to update markets not found to closed status.</param>
        /// <returns>A tuple containing processed count and error count.</returns>
        Task<(int ProcessedCount, int ErrorCount)> FetchMarketsAsync(
            string? eventTicker = null, string? seriesTicker = null, string? maxCloseTs = null,
            string? minCloseTs = null, string? status = null, string[]? tickers = null, bool updateNotFoundToClosed = true);

        /// <summary>
        /// Fetches a series by its ticker from the Kalshi API.
        /// </summary>
        /// <param name="seriesTicker">The series ticker to fetch.</param>
        /// <returns>The series response if found, null otherwise.</returns>
        Task<SeriesResponse?> FetchSeriesAsync(string seriesTicker);

        /// <summary>
        /// Fetches an event by its ticker from the Kalshi API.
        /// </summary>
        /// <param name="eventTicker">The event ticker to fetch.</param>
        /// <param name="withNestedMarkets">Whether to include nested markets in the response.</param>
        /// <returns>The event response if found, null otherwise.</returns>
        Task<EventResponse?> FetchEventAsync(string eventTicker, bool withNestedMarkets = false);

        /// <summary>
        /// Fetches positions from the Kalshi API based on specified filters.
        /// </summary>
        /// <param name="cursor">Optional cursor for pagination.</param>
        /// <param name="limit">Optional limit for number of results.</param>
        /// <param name="countFilter">Optional count filter.</param>
        /// <param name="settlementStatus">Optional settlement status filter.</param>
        /// <returns>A tuple containing processed count and error count.</returns>
        Task<(int ProcessedCount, int ErrorCount)> FetchPositionsAsync(
            string? cursor = null, int? limit = null, string? countFilter = null, string? settlementStatus = null);

        /// <summary>
        /// Fetches candlestick data from the Kalshi API for a specific market.
        /// </summary>
        /// <param name="seriesTicker">The series ticker.</param>
        /// <param name="marketTicker">The market ticker.</param>
        /// <param name="interval">The time interval for candlesticks.</param>
        /// <param name="startTs">The start timestamp.</param>
        /// <param name="endTs">Optional end timestamp.</param>
        /// <param name="updateLastCandlestick">Whether to update the last candlestick.</param>
        /// <returns>A tuple containing processed count and error count.</returns>
        Task<(int ProcessedCount, int ErrorCount)> FetchCandlesticksAsync(
            string seriesTicker, string marketTicker, string interval, long startTs, long? endTs = null, bool updateLastCandlestick = true);

        /// <summary>
        /// Gets the current account balance from the Kalshi API.
        /// </summary>
        /// <returns>The account balance as a long value.</returns>
        Task<long> GetBalanceAsync();

        /// <summary>
        /// Gets the current exchange status from the Kalshi API.
        /// </summary>
        /// <returns>The exchange status.</returns>
        Task<ExchangeStatus> GetExchangeStatusAsync();

        /// <summary>
        /// Gets the exchange schedule from the Kalshi API.
        /// </summary>
        /// <returns>The exchange schedule response.</returns>
        Task<ExchangeScheduleResponse> GetExchangeScheduleAsync();

        /// <summary>
        /// Fetches orders from the Kalshi API based on specified filters.
        /// </summary>
        /// <param name="ticker">Optional market ticker filter.</param>
        /// <param name="eventTicker">Optional event ticker filter.</param>
        /// <param name="minTs">Optional minimum timestamp filter.</param>
        /// <param name="maxTs">Optional maximum timestamp filter.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="cursor">Optional cursor for pagination.</param>
        /// <param name="limit">Optional limit for number of results.</param>
        /// <returns>A tuple containing processed count and error count.</returns>
        Task<(int ProcessedCount, int ErrorCount)> FetchOrdersAsync(
            string? ticker = null, string? eventTicker = null, long? minTs = null, long? maxTs = null,
            string? status = null, string? cursor = null, int? limit = null);

        /// <summary>
        /// Fetches announcements from the Kalshi API.
        /// </summary>
        /// <returns>A tuple containing processed count and error count.</returns>
        Task<(int ProcessedCount, int ErrorCount)> FetchAnnouncementsAsync();

        /// <summary>
        /// Fetches the exchange schedule from the Kalshi API.
        /// </summary>
        /// <returns>A tuple containing processed count and error count.</returns>
        Task<(int ProcessedCount, int ErrorCount)> FetchExchangeScheduleAsync();
    }
}
