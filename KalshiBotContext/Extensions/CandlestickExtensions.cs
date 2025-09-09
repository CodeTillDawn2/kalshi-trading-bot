using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class CandlestickExtensions
    {
        public static CandlestickDTO ToCandlestickDTO(this Candlestick candlestick)
        {
            return new CandlestickDTO
            {
                market_ticker = candlestick.market_ticker,
                interval_type = candlestick.interval_type,
                end_period_ts = candlestick.end_period_ts,
                end_period_datetime_utc = candlestick.end_period_datetime_utc,
                year = candlestick.year,
                month = candlestick.month,
                day = candlestick.day,
                hour = candlestick.hour,
                minute = candlestick.minute,
                open_interest = candlestick.open_interest,
                price_close = candlestick.price_close,
                price_high = candlestick.price_high,
                price_low = candlestick.price_low,
                price_mean = candlestick.price_mean,
                price_open = candlestick.price_open,
                price_previous = candlestick.price_previous,
                volume = candlestick.volume,
                yes_ask_close = candlestick.yes_ask_close,
                yes_ask_high = candlestick.yes_ask_high,
                yes_ask_low = candlestick.yes_ask_low,
                yes_ask_open = candlestick.yes_ask_open,
                yes_bid_close = candlestick.yes_bid_close,
                yes_bid_high = candlestick.yes_bid_high,
                yes_bid_low = candlestick.yes_bid_low,
                yes_bid_open = candlestick.yes_bid_open
            };
        }

        public static Candlestick ToCandlestick(this CandlestickDTO candlestickDTO)
        {
            return new Candlestick
            {
                market_ticker = candlestickDTO.market_ticker,
                interval_type = candlestickDTO.interval_type,
                end_period_ts = candlestickDTO.end_period_ts,
                end_period_datetime_utc = candlestickDTO.end_period_datetime_utc,
                year = candlestickDTO.year,
                month = candlestickDTO.month,
                day = candlestickDTO.day,
                hour = candlestickDTO.hour,
                minute = candlestickDTO.minute,
                open_interest = candlestickDTO.open_interest,
                price_close = candlestickDTO.price_close,
                price_high = candlestickDTO.price_high,
                price_low = candlestickDTO.price_low,
                price_mean = candlestickDTO.price_mean,
                price_open = candlestickDTO.price_open,
                price_previous = candlestickDTO.price_previous,
                volume = candlestickDTO.volume,
                yes_ask_close = candlestickDTO.yes_ask_close,
                yes_ask_high = candlestickDTO.yes_ask_high,
                yes_ask_low = candlestickDTO.yes_ask_low,
                yes_ask_open = candlestickDTO.yes_ask_open,
                yes_bid_close = candlestickDTO.yes_bid_close,
                yes_bid_high = candlestickDTO.yes_bid_high,
                yes_bid_low = candlestickDTO.yes_bid_low,
                yes_bid_open = candlestickDTO.yes_bid_open
            };
        }

        public static Candlestick UpdateCandlestick(this Candlestick candlestick, CandlestickDTO candlestickDTO)
        {
            if (candlestick.market_ticker != candlestickDTO.market_ticker || candlestick.end_period_ts != candlestickDTO.end_period_ts
                || candlestick.interval_type != candlestickDTO.interval_type)
            {
                throw new Exception("Market ticker, interval type or end period timestamp don't match for Update Candlestick");
            }
            candlestick.open_interest = candlestickDTO.open_interest;
            candlestick.price_close = candlestickDTO.price_close;
            candlestick.price_high = candlestickDTO.price_high;
            candlestick.price_low = candlestickDTO.price_low;
            candlestick.price_mean = candlestickDTO.price_mean;
            candlestick.price_open = candlestickDTO.price_open;
            candlestick.price_previous = candlestickDTO.price_previous;
            candlestick.volume = candlestickDTO.volume;
            candlestick.yes_ask_close = candlestickDTO.yes_ask_close;
            candlestick.yes_ask_high = candlestickDTO.yes_ask_high;
            candlestick.yes_ask_low = candlestickDTO.yes_ask_low;
            candlestick.yes_ask_open = candlestickDTO.yes_ask_open;
            candlestick.yes_bid_close = candlestickDTO.yes_bid_close;
            candlestick.yes_bid_high = candlestickDTO.yes_bid_high;
            candlestick.yes_bid_low = candlestickDTO.yes_bid_low;
            candlestick.yes_bid_open = candlestickDTO.yes_bid_open;
            return candlestick;
        }
    }
}
