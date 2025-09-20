namespace KalshiBotData.Models
{
    /// <summary>
    /// Represents a candlestick data point for a market in the Kalshi trading bot.
    /// Captures OHLCV (Open, High, Low, Close, Volume) and bid/ask prices over a specific time interval.
    /// Used for technical analysis, pattern detection, and trading decisions.
    /// </summary>
    public class Candlestick
    {
        /// <summary>
        /// Gets or sets the ticker symbol of the market this candlestick belongs to.
        /// </summary>
        public string market_ticker { get; set; }

        /// <summary>
        /// Gets or sets the type of time interval for this candlestick (e.g., 1-minute, 5-minute).
        /// </summary>
        public int interval_type { get; set; }

        /// <summary>
        /// Gets or sets the Unix timestamp marking the end of the candlestick period.
        /// </summary>
        public long end_period_ts { get; set; }

        /// <summary>
        /// Gets or sets the UTC datetime marking the end of the candlestick period.
        /// </summary>
        public DateTime end_period_datetime_utc { get; set; }

        /// <summary>
        /// Gets or sets the year component of the end period datetime.
        /// </summary>
        public int year { get; set; }

        /// <summary>
        /// Gets or sets the month component of the end period datetime.
        /// </summary>
        public int month { get; set; }

        /// <summary>
        /// Gets or sets the day component of the end period datetime.
        /// </summary>
        public int day { get; set; }

        /// <summary>
        /// Gets or sets the hour component of the end period datetime.
        /// </summary>
        public int hour { get; set; }

        /// <summary>
        /// Gets or sets the minute component of the end period datetime.
        /// </summary>
        public int minute { get; set; }

        /// <summary>
        /// Gets or sets the open interest for the market at the end of the period.
        /// </summary>
        public int open_interest { get; set; }

        /// <summary>
        /// Gets or sets the closing price (yes price) at the end of the period.
        /// </summary>
        public int? price_close { get; set; }

        /// <summary>
        /// Gets or sets the highest price (yes price) during the period.
        /// </summary>
        public int? price_high { get; set; }

        /// <summary>
        /// Gets or sets the lowest price (yes price) during the period.
        /// </summary>
        public int? price_low { get; set; }

        /// <summary>
        /// Gets or sets the average (mean) price (yes price) during the period.
        /// </summary>
        public int? price_mean { get; set; }

        /// <summary>
        /// Gets or sets the opening price (yes price) at the start of the period.
        /// </summary>
        public int? price_open { get; set; }

        /// <summary>
        /// Gets or sets the previous period's closing price for comparison.
        /// </summary>
        public int? price_previous { get; set; }

        /// <summary>
        /// Gets or sets the trading volume during the period.
        /// </summary>
        public int volume { get; set; }

        /// <summary>
        /// Gets or sets the closing ask price (yes ask) at the end of the period.
        /// </summary>
        public int yes_ask_close { get; set; }

        /// <summary>
        /// Gets or sets the highest ask price (yes ask) during the period.
        /// </summary>
        public int yes_ask_high { get; set; }

        /// <summary>
        /// Gets or sets the lowest ask price (yes ask) during the period.
        /// </summary>
        public int yes_ask_low { get; set; }

        /// <summary>
        /// Gets or sets the opening ask price (yes ask) at the start of the period.
        /// </summary>
        public int yes_ask_open { get; set; }

        /// <summary>
        /// Gets or sets the closing bid price (yes bid) at the end of the period.
        /// </summary>
        public int yes_bid_close { get; set; }

        /// <summary>
        /// Gets or sets the highest bid price (yes bid) during the period.
        /// </summary>
        public int yes_bid_high { get; set; }

        /// <summary>
        /// Gets or sets the lowest bid price (yes bid) during the period.
        /// </summary>
        public int yes_bid_low { get; set; }

        /// <summary>
        /// Gets or sets the opening bid price (yes bid) at the start of the period.
        /// </summary>
        public int yes_bid_open { get; set; }

        /// <summary>
        /// Gets or sets the associated market entity for this candlestick.
        /// </summary>
        public Market Market { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Candlestick"/> class (default constructor).
        /// </summary>
        public Candlestick() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Candlestick"/> class with all required data.
        /// </summary>
        /// <param name="marketTicker">The market ticker symbol.</param>
        /// <param name="intervalType">The interval type (e.g., minutes).</param>
        /// <param name="endPeriodTs">The end period Unix timestamp.</param>
        /// <param name="year">The year of the end period.</param>
        /// <param name="month">The month of the end period.</param>
        /// <param name="day">The day of the end period.</param>
        /// <param name="hour">The hour of the end period.</param>
        /// <param name="minute">The minute of the end period.</param>
        /// <param name="openInterest">The open interest.</param>
        /// <param name="priceClose">The closing price (nullable).</param>
        /// <param name="priceHigh">The high price (nullable).</param>
        /// <param name="priceLow">The low price (nullable).</param>
        /// <param name="priceMean">The mean price (nullable).</param>
        /// <param name="priceOpen">The opening price (nullable).</param>
        /// <param name="pricePrevious">The previous closing price (nullable).</param>
        /// <param name="volume">The volume.</param>
        /// <param name="yesAskClose">The closing ask price.</param>
        /// <param name="yesAskHigh">The high ask price.</param>
        /// <param name="yesAskLow">The low ask price.</param>
        /// <param name="yesAskOpen">The opening ask price.</param>
        /// <param name="yesBidClose">The closing bid price.</param>
        /// <param name="yesBidHigh">The high bid price.</param>
        /// <param name="yesBidLow">The low bid price.</param>
        /// <param name="yesBidOpen">The opening bid price.</param>
        public Candlestick(string marketTicker, int intervalType, long endPeriodTs,
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
