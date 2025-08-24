using SmokehouseDTOs.KalshiAPI;

namespace SmokehouseBot.KalshiAPI.Interfaces
{
    public interface IKalshiAPIService
    {
        Task<(int ProcessedCount, int ErrorCount)> FetchMarketsAsync(
            string? eventTicker = null, string? seriesTicker = null, string? maxCloseTs = null,
            string? minCloseTs = null, string? status = null, string[]? tickers = null, bool updateNotFoundToClosed = true);

        Task<SeriesResponse?> FetchSeriesAsync(string seriesTicker);

        Task<EventResponse?> FetchEventAsync(string eventTicker, bool withNestedMarkets = false);

        Task<(int ProcessedCount, int ErrorCount)> FetchPositionsAsync(
            string? cursor = null, int? limit = null, string? countFilter = null, string? settlementStatus = null);

        Task<(int ProcessedCount, int ErrorCount)> FetchCandlesticksAsync(
            string seriesTicker, string marketTicker, string interval, long startTs, long? endTs = null, bool updateLastCandlestick = true);

        Task<long> GetBalanceAsync();

        Task<ExchangeStatus> GetExchangeStatusAsync();

        Task<ExchangeScheduleResponse> GetExchangeScheduleAsync();

        Task<(int ProcessedCount, int ErrorCount)> FetchOrdersAsync(
            string? ticker = null, string? eventTicker = null, long? minTs = null, long? maxTs = null,
            string? status = null, string? cursor = null, int? limit = null);
    }
}