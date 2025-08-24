namespace SmokehouseDTOs.Data
{
    public class CandlestickDTO
    {
        public string market_ticker { get; set; }
        public int interval_type { get; set; }
        public long end_period_ts { get; set; }
        public DateTime end_period_datetime_utc { get; set; }
        public int year { get; set; }
        public int month { get; set; }
        public int day { get; set; }
        public int hour { get; set; }
        public int minute { get; set; }
        public int open_interest { get; set; }
        public int? price_close { get; set; }
        public int? price_high { get; set; }
        public int? price_low { get; set; }
        public int? price_mean { get; set; }
        public int? price_open { get; set; }
        public int? price_previous { get; set; }
        public int volume { get; set; }
        public int yes_ask_close { get; set; }
        public int yes_ask_high { get; set; }
        public int yes_ask_low { get; set; }
        public int yes_ask_open { get; set; }
        public int yes_bid_close { get; set; }
        public int yes_bid_high { get; set; }
        public int yes_bid_low { get; set; }
        public int yes_bid_open { get; set; }

        public CandlestickDTO() { }

        public CandlestickDTO(string marketTicker, int intervalType, long endPeriodTs,
            int year, int month, int day, int hour, int minute,
            int openInterest, int? priceClose, int? priceHigh, int? priceLow,
            int? priceMean, int? priceOpen, int? pricePrevious, int volume,
            int yesAskClose, int yesAskHigh, int yesAskLow, int yesAskOpen,
            int yesBidClose, int yesBidHigh, int yesBidLow, int yesBidOpen)
        {
            market_ticker = marketTicker;
            interval_type = intervalType;
            end_period_ts = endPeriodTs;
            this.year = year;
            this.month = month;
            this.day = day;
            this.hour = hour;
            this.minute = minute;
            this.open_interest = openInterest;
            price_close = priceClose;
            price_high = priceHigh;
            price_low = priceLow;
            price_mean = priceMean;
            price_open = priceOpen;
            price_previous = pricePrevious;
            this.volume = volume;
            yes_ask_close = yesAskClose;
            yes_ask_high = yesAskHigh;
            yes_ask_low = yesAskLow;
            yes_ask_open = yesAskOpen;
            yes_bid_close = yesBidClose;
            yes_bid_high = yesBidHigh;
            yes_bid_low = yesBidLow;
            yes_bid_open = yesBidOpen;
        }
    }


}
