namespace BacklashInterfaces.Constants
{
    public static class KalshiConstants
    {
        public const string Status_Finalized = "finalized";
        public const string Status_Active = "active";
        public const string Status_Bad = "bad";
        public const string Status_Open = "open";
        public const string Status_Closed = "closed";
        public const string Status_Determined = "determined";
        public const string Status_Inactive = "inactive";
        public const string Status_Initialized = "initialized";
        public const string Status_Settled = "settled";
        public const string Status_Unopened = "unopened";

        public const string Interval_Minute = "minute";
        public const string Interval_Hour = "hour";
        public const string Interval_Day = "day";

        public const string Parameter_Action = "action";
        public const string Parameter_Series_Ticker = "series-ticker";
        public const string Parameter_Market_Ticker = "market-ticker";
        public const string Parameter_Event_Ticker = "event-ticker";
        public const string Parameter_Interval = "interval";
        public const string Parameter_Start_Ts = "start-ts";
        public const string Parameter_Start_Ts_m = "start-ts-m";
        public const string Parameter_Start_Ts_h = "start-ts-h";
        public const string Parameter_Start_Ts_d = "start-ts-d";
        public const string Parameter_End_Ts = "end-ts";
        public const string Parameter_Status = "status";
        public const string Parameter_With_Nested_Markets = "with-nested-markets";
        public const string Parameter_Max_Close_TS = "max-close-ts";
        public const string Parameter_Min_Close_TS = "min-close-ts";
        public const string Parameter_Tickers = "tickers";

        public const string ScriptType_Feed_Ticker = "ticker";
        public const string ScriptType_Feed_Orderbook = "orderbook";
        public const string ScriptType_Feed_Fill = "fill";
        public const string ScriptType_Feed_Lifecycle = "lifecycle";
        public const string ScriptType_Feed_Trade = "trade";

        public static readonly string[] AllChannels = {
            ScriptType_Feed_Ticker,
            ScriptType_Feed_Orderbook,
            ScriptType_Feed_Fill,
            ScriptType_Feed_Lifecycle,
            ScriptType_Feed_Trade
        };

        public static readonly string[] MarketChannels = {
            ScriptType_Feed_Ticker,
            ScriptType_Feed_Orderbook,
            ScriptType_Feed_Trade
        };

        public const string ScriptType_Market = "market";
        public const string ScriptType_Event = "event";
        public const string ScriptType_Series = "series";
        public const string ScriptType_Candlestick = "candlestick";
        public const string ScriptType_ChartCandlesticks = "chartcandlestick";


        public static DateTime TruncateToMinute(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Utc);
        }

        public static bool MarketIsEnded(string status)
        {
            if (status == Status_Finalized || status == Status_Bad || status == Status_Closed || status == Status_Settled
                || status == Status_Determined || status == Status_Inactive)
                return true;
            return false;
        }
    }
}
