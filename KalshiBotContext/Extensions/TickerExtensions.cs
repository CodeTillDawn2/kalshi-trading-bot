using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between Ticker model and TickerDTO,
    /// supporting real-time market ticker data transfer and price information updates.
    /// </summary>
    public static class TickerExtensions
    {
        /// <summary>
        /// Converts a Ticker model to its DTO representation,
        /// mapping all ticker data including market information, prices, and volume metrics.
        /// </summary>
        /// <param name="ticker">The Ticker model to convert.</param>
        /// <returns>A new TickerDTO with all ticker properties mapped.</returns>
        public static TickerDTO ToTickerDTO(this Ticker ticker)
        {
            return new TickerDTO
            {
                market_id = ticker.market_id,
                market_ticker = ticker.market_ticker,
                price = ticker.price,
                yes_bid = ticker.yes_bid,
                yes_ask = ticker.yes_ask,
                volume = ticker.volume,
                open_interest = ticker.open_interest,
                dollar_volume = ticker.dollar_volume,
                dollar_open_interest = ticker.dollar_open_interest,
                ts = ticker.ts,
                LoggedDate = ticker.LoggedDate,
                ProcessedDate = ticker.ProcessedDate
            };
        }

        /// <summary>
        /// Converts a TickerDTO to its model representation,
        /// creating a new Ticker with all properties mapped from the DTO.
        /// </summary>
        /// <param name="tickerDTO">The TickerDTO to convert.</param>
        /// <returns>A new Ticker model with all properties mapped from the DTO.</returns>
        public static Ticker ToTicker(this TickerDTO tickerDTO)
        {
            return new Ticker
            {
                market_id = tickerDTO.market_id,
                market_ticker = tickerDTO.market_ticker,
                price = tickerDTO.price,
                yes_bid = tickerDTO.yes_bid,
                yes_ask = tickerDTO.yes_ask,
                volume = tickerDTO.volume,
                open_interest = tickerDTO.open_interest,
                dollar_volume = tickerDTO.dollar_volume,
                dollar_open_interest = tickerDTO.dollar_open_interest,
                ts = tickerDTO.ts,
                LoggedDate = tickerDTO.LoggedDate,
                ProcessedDate = tickerDTO.ProcessedDate
            };
        }

        /// <summary>
        /// Updates an existing Ticker model with data from a TickerDTO,
        /// validating market ticker and logged date match before updating price and volume data.
        /// </summary>
        /// <param name="ticker">The Ticker model to update.</param>
        /// <param name="tickerDTO">The TickerDTO containing updated data.</param>
        /// <returns>The updated Ticker model.</returns>
        /// <exception cref="Exception">Thrown when market tickers or logged dates do not match.</exception>
        public static Ticker UpdateTicker(this Ticker ticker, TickerDTO tickerDTO)
        {
            if (ticker.market_ticker != tickerDTO.market_ticker || ticker.LoggedDate != tickerDTO.LoggedDate)
            {
                throw new Exception("Market ticker or logged dates don't match for Update Ticker");
            }
            ticker.price = tickerDTO.price;
            ticker.yes_bid = tickerDTO.yes_bid;
            ticker.yes_ask = tickerDTO.yes_ask;
            ticker.volume = tickerDTO.volume;
            ticker.open_interest = tickerDTO.open_interest;
            ticker.dollar_volume = tickerDTO.dollar_volume;
            ticker.dollar_open_interest = tickerDTO.dollar_open_interest;
            ticker.ts = tickerDTO.ts;
            ticker.ProcessedDate = tickerDTO.ProcessedDate;
            return ticker;
        }
    }
}
