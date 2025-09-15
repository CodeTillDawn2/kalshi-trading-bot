namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for candlestick data.
    /// </summary>
    public class CandlestickDTO
    {
        /// <summary>
        /// Gets or sets the market ticker symbol.
        /// </summary>
        public string? market_ticker { get; set; }

        /// <summary>
        /// Gets or sets the interval type.
        /// </summary>
        public int interval_type { get; set; }

        /// <summary>
        /// Gets or sets the end period timestamp.
        /// </summary>
        public long end_period_ts { get; set; }

        /// <summary>
        /// Gets or sets the end period date and time in UTC.
        /// </summary>
        public DateTime end_period_datetime_utc { get; set; }

        /// <summary>
        /// Gets or sets the year.
        /// </summary>
        public int year { get; set; }

        /// <summary>
        /// Gets or sets the month.
        /// </summary>
        public int month { get; set; }

        /// <summary>
        /// Gets or sets the day.
        /// </summary>
        public int day { get; set; }

        /// <summary>
        /// Gets or sets the hour.
        /// </summary>
        public int hour { get; set; }

        /// <summary>
        /// Gets or sets the minute.
        /// </summary>
        public int minute { get; set; }

        /// <summary>
        /// Gets or sets the open interest.
        /// </summary>
        public int open_interest { get; set; }

        /// <summary>
        /// Gets or sets the closing price.
        /// </summary>
        public int? price_close { get; set; }

        /// <summary>
        /// Gets or sets the highest price.
        /// </summary>
        public int? price_high { get; set; }

        /// <summary>
        /// Gets or sets the lowest price.
        /// </summary>
        public int? price_low { get; set; }

        /// <summary>
        /// Gets or sets the mean price.
        /// </summary>
        public int? price_mean { get; set; }

        /// <summary>
        /// Gets or sets the opening price.
        /// </summary>
        public int? price_open { get; set; }

        /// <summary>
        /// Gets or sets the previous price.
        /// </summary>
        public int? price_previous { get; set; }

        /// <summary>
        /// Gets or sets the trading volume.
        /// </summary>
        public int volume { get; set; }

        /// <summary>
        /// Gets or sets the yes ask closing price.
        /// </summary>
        public int yes_ask_close { get; set; }

        /// <summary>
        /// Gets or sets the yes ask highest price.
        /// </summary>
        public int yes_ask_high { get; set; }

        /// <summary>
        /// Gets or sets the yes ask lowest price.
        /// </summary>
        public int yes_ask_low { get; set; }

        /// <summary>
        /// Gets or sets the yes ask opening price.
        /// </summary>
        public int yes_ask_open { get; set; }

        /// <summary>
        /// Gets or sets the yes bid closing price.
        /// </summary>
        public int yes_bid_close { get; set; }

        /// <summary>
        /// Gets or sets the yes bid highest price.
        /// </summary>
        public int yes_bid_high { get; set; }

        /// <summary>
        /// Gets or sets the yes bid lowest price.
        /// </summary>
        public int yes_bid_low { get; set; }

        /// <summary>
        /// Gets or sets the yes bid opening price.
        /// </summary>
        public int yes_bid_open { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CandlestickDTO"/> class.
        /// </summary>
        public CandlestickDTO() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CandlestickDTO"/> class with the specified parameters.
        /// </summary>
        /// <param name="marketTicker">The market ticker symbol.</param>
        /// <param name="intervalType">The interval type.</param>
        /// <param name="endPeriodTs">The end period timestamp.</param>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day.</param>
        /// <param name="hour">The hour.</param>
        /// <param name="minute">The minute.</param>
        /// <param name="openInterest">The open interest.</param>
        /// <param name="priceClose">The closing price.</param>
        /// <param name="priceHigh">The highest price.</param>
        /// <param name="priceLow">The lowest price.</param>
        /// <param name="priceMean">The mean price.</param>
        /// <param name="priceOpen">The opening price.</param>
        /// <param name="pricePrevious">The previous price.</param>
        /// <param name="volume">The trading volume.</param>
        /// <param name="yesAskClose">The yes ask closing price.</param>
        /// <param name="yesAskHigh">The yes ask highest price.</param>
        /// <param name="yesAskLow">The yes ask lowest price.</param>
        /// <param name="yesAskOpen">The yes ask opening price.</param>
        /// <param name="yesBidClose">The yes bid closing price.</param>
        /// <param name="yesBidHigh">The yes bid highest price.</param>
        /// <param name="yesBidLow">The yes bid lowest price.</param>
        /// <param name="yesBidOpen">The yes bid opening price.</param>
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
