using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class MarketWatchExtensions
    {
        public static MarketWatchDTO ToMarketWatchDTO(this MarketWatch marketWatch)
        {
            return new MarketWatchDTO
            {
                market_ticker = marketWatch.market_ticker,
                BrainLock = marketWatch.BrainLock,
                InterestScore = marketWatch.InterestScore,
                InterestScoreDate = marketWatch.InterestScoreDate,
                LastWatched = marketWatch.LastWatched,
                AverageWebsocketEventsPerMinute = marketWatch.AverageWebsocketEventsPerMinute
            };
        }

        public static MarketWatch ToMarketWatch(this MarketWatchDTO marketWatchDTO)
        {
            return new MarketWatch
            {
                market_ticker = marketWatchDTO.market_ticker,
                BrainLock = marketWatchDTO.BrainLock,
                InterestScore = marketWatchDTO.InterestScore,
                InterestScoreDate = marketWatchDTO.InterestScoreDate,
                LastWatched = marketWatchDTO.LastWatched,
                AverageWebsocketEventsPerMinute = marketWatchDTO.AverageWebsocketEventsPerMinute,
            };
        }

        public static MarketWatch UpdateMarketWatch(this MarketWatch marketWatch, MarketWatchDTO marketWatchDTO)
        {
            if (marketWatch.market_ticker != marketWatchDTO.market_ticker)
            {
                throw new Exception("Market ticker doesn't match for Update MarketWatch");
            }
            marketWatch.BrainLock = marketWatchDTO.BrainLock;
            if (marketWatchDTO.InterestScore != null) marketWatch.InterestScore = marketWatchDTO.InterestScore;
            if (marketWatchDTO.InterestScoreDate != null) marketWatch.InterestScoreDate = marketWatchDTO.InterestScoreDate;
            if (marketWatchDTO.LastWatched != null) marketWatch.LastWatched = marketWatchDTO.LastWatched;
            marketWatch.AverageWebsocketEventsPerMinute = marketWatchDTO.AverageWebsocketEventsPerMinute;
            return marketWatch;
        }
    }
}
