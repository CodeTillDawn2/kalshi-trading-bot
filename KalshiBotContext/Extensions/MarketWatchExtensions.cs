using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between MarketWatch model and MarketWatchDTO,
    /// supporting market monitoring and interest scoring data transfer operations.
    /// </summary>
    public static class MarketWatchExtensions
    {
        /// <summary>
        /// Converts a MarketWatch model to its DTO representation,
        /// mapping all market watch properties for data transfer.
        /// </summary>
        /// <param name="marketWatch">The MarketWatch model to convert.</param>
        /// <returns>A new MarketWatchDTO with all market watch properties mapped.</returns>
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

        /// <summary>
        /// Converts a MarketWatchDTO to its model representation,
        /// creating a new MarketWatch with all properties mapped from the DTO.
        /// </summary>
        /// <param name="marketWatchDTO">The MarketWatchDTO to convert.</param>
        /// <returns>A new MarketWatch model with all properties mapped from the DTO.</returns>
        public static MarketWatch ToMarketWatch(this MarketWatchDTO marketWatchDTO)
        {
            return new MarketWatch
            {
                market_ticker = marketWatchDTO.market_ticker,
                BrainLock = marketWatchDTO.BrainLock,
                InterestScore = marketWatchDTO.InterestScore,
                InterestScoreDate = marketWatchDTO.InterestScoreDate,
                LastWatched = marketWatchDTO.LastWatched,
                AverageWebsocketEventsPerMinute = marketWatchDTO.AverageWebsocketEventsPerMinute
            };
        }

        /// <summary>
        /// Updates an existing MarketWatch model with data from a MarketWatchDTO,
        /// validating market ticker match before applying selective property updates.
        /// </summary>
        /// <param name="marketWatch">The MarketWatch model to update.</param>
        /// <param name="marketWatchDTO">The MarketWatchDTO containing updated data.</param>
        /// <returns>The updated MarketWatch model.</returns>
        /// <exception cref="Exception">Thrown when market tickers do not match.</exception>
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
