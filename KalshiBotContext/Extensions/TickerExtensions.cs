using KalshiBotData.Models;
using SmokehouseDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class TickerExtensions
    {
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