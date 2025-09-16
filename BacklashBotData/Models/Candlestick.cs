namespace KalshiBotData.Models
{
/// <summary>Candlestick</summary>
/// <summary>Candlestick</summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
    public class Candlestick
/// <summary>Candlestick</summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
    {
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public string market_ticker { get; set; }
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public int interval_type { get; set; }
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public long end_period_ts { get; set; }
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public DateTime end_period_datetime_utc { get; set; }
/// <summary>
/// </summary>
        public int year { get; set; }
/// <summary>
/// </summary>
        public int month { get; set; }
/// <summary>
/// </summary>
        public int day { get; set; }
/// <summary>
/// </summary>
        public int hour { get; set; }
/// <summary>Gets or sets the minute.</summary>
/// <summary>Gets or sets the minute.</summary>
        public int minute { get; set; }
/// <summary>
/// </summary>
        public int open_interest { get; set; }
/// <summary>Gets or sets the price_low.</summary>
/// <summary>Gets or sets the price_high.</summary>
        public int? price_close { get; set; }
/// <summary>Gets or sets the price_previous.</summary>
/// <summary>Gets or sets the price_mean.</summary>
        public int? price_high { get; set; }
/// <summary>Gets or sets the yes_ask_high.</summary>
/// <summary>Gets or sets the price_previous.</summary>
        public int? price_low { get; set; }
/// <summary>Gets or sets the yes_bid_close.</summary>
/// <summary>Gets or sets the yes_ask_close.</summary>
        public int? price_mean { get; set; }
/// <summary>Gets or sets the yes_bid_open.</summary>
/// <summary>Gets or sets the yes_ask_low.</summary>
        public int? price_open { get; set; }
/// <summary>Candlestick</summary>
/// <summary>Gets or sets the yes_bid_close.</summary>
        public int? price_previous { get; set; }
/// <summary>Gets or sets the yes_bid_low.</summary>
        public int volume { get; set; }
/// <summary>Gets or sets the Market.</summary>
        public int yes_ask_close { get; set; }
/// <summary>Candlestick</summary>
        public int yes_ask_high { get; set; }
/// <summary>Candlestick</summary>
        public int yes_ask_low { get; set; }
        public int yes_ask_open { get; set; }
        public int yes_bid_close { get; set; }
        public int yes_bid_high { get; set; }
        public int yes_bid_low { get; set; }
        public int yes_bid_open { get; set; }
        public Market Market { get; set; }

        public Candlestick() { }

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
