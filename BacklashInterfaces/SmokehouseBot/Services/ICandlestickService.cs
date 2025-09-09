using BacklashDTOs;

namespace BacklashBot.Services.Interfaces
{
    public interface ICandlestickService
    {
        Task UpdateCandlesticksAsync(string marketTicker);
        Task PopulateMarketDataAsync(string marketTicker);
        List<CandlestickData> RetrieveHistoricalCandlesticksAsync(string marketTicker, string timeframe);
    }
}
