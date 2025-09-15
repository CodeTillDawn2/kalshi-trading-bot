namespace BacklashInterfaces.Constants
{
    /// <summary>
    /// Contains constant values used throughout the Kalshi trading bot system for API interactions,
    /// WebSocket communication, and data processing. This centralizes magic strings to improve
    /// maintainability and reduce the risk of typos in hardcoded values.
    /// </summary>
    public static class KalshiConstants
    {
        #region Enums
        /// <summary>
        /// Enum representing the possible statuses of a market.
        /// </summary>
        public enum MarketStatus
        {
            /// <summary>
            /// Market has been finalized and trading is complete.
            /// </summary>
            Finalized,

            /// <summary>
            /// Market is actively trading and accepting orders.
            /// </summary>
            Active,

            /// <summary>
            /// Market has been marked as invalid or problematic.
            /// </summary>
            Bad,

            /// <summary>
            /// Market is open for trading but not yet active.
            /// </summary>
            Open,

            /// <summary>
            /// Market has been closed and no longer accepts orders.
            /// </summary>
            Closed,

            /// <summary>
            /// Market outcome has been determined.
            /// </summary>
            Determined,

            /// <summary>
            /// Market is inactive and not available for trading.
            /// </summary>
            Inactive,

            /// <summary>
            /// Market has been initialized but not yet opened.
            /// </summary>
            Initialized,

            /// <summary>
            /// Market has been settled and payouts distributed.
            /// </summary>
            Settled,

            /// <summary>
            /// Market has not yet opened for trading.
            /// </summary>
            Unopened
        }

        /// <summary>
        /// Enum representing time intervals for candlestick data.
        /// </summary>
        public enum TimeInterval
        {
            /// <summary>
            /// One-minute time interval for candlestick data.
            /// </summary>
            Minute,

            /// <summary>
            /// One-hour time interval for candlestick data.
            /// </summary>
            Hour,

            /// <summary>
            /// One-day time interval for candlestick data.
            /// </summary>
            Day
        }

        /// <summary>
        /// Enum representing WebSocket feed types.
        /// </summary>
        public enum FeedType
        {
            /// <summary>
            /// Feed type for ticker updates containing price and volume information.
            /// </summary>
            Ticker,

            /// <summary>
            /// Feed type for orderbook updates containing bid/ask price changes.
            /// </summary>
            Orderbook,

            /// <summary>
            /// Feed type for fill updates containing executed trade information.
            /// </summary>
            Fill,

            /// <summary>
            /// Feed type for lifecycle updates containing market status changes.
            /// </summary>
            Lifecycle,

            /// <summary>
            /// Feed type for trade updates containing individual trade executions.
            /// </summary>
            Trade,

            /// <summary>
            /// Feed type for event lifecycle updates containing event-level status changes.
            /// </summary>
            Event_Lifecycle
        }

        /// <summary>
        /// Enum representing script types for API endpoints.
        /// </summary>
        public enum ScriptType
        {
            /// <summary>
            /// Script type identifier for market-related API endpoints.
            /// </summary>
            Market,

            /// <summary>
            /// Script type identifier for event-related API endpoints.
            /// </summary>
            Event,

            /// <summary>
            /// Script type identifier for series-related API endpoints.
            /// </summary>
            Series,

            /// <summary>
            /// Script type identifier for candlestick data API endpoints.
            /// </summary>
            Candlestick,

            /// <summary>
            /// Script type identifier for chart candlestick data API endpoints.
            /// </summary>
            ChartCandlesticks
        }
        #endregion

        #region Market Status Constants
        /// <summary>
        /// Represents a market that has been finalized and is no longer active.
        /// </summary>
        public const string Status_Finalized = "finalized";

        /// <summary>
        /// Represents an actively trading market where orders can be placed.
        /// </summary>
        public const string Status_Active = "active";

        /// <summary>
        /// Represents a market that has been marked as bad or invalid.
        /// </summary>
        public const string Status_Bad = "bad";

        /// <summary>
        /// Represents a market that is open for trading but not yet active.
        /// </summary>
        public const string Status_Open = "open";

        /// <summary>
        /// Represents a market that has been closed and is no longer accepting orders.
        /// </summary>
        public const string Status_Closed = "closed";

        /// <summary>
        /// Represents a market where the outcome has been determined.
        /// </summary>
        public const string Status_Determined = "determined";

        /// <summary>
        /// Represents a market that is inactive and not available for trading.
        /// </summary>
        public const string Status_Inactive = "inactive";

        /// <summary>
        /// Represents a market that has been initialized but not yet open.
        /// </summary>
        public const string Status_Initialized = "initialized";

        /// <summary>
        /// Represents a market that has been settled and payouts distributed.
        /// </summary>
        public const string Status_Settled = "settled";

        /// <summary>
        /// Represents a market that has not yet opened for trading.
        /// </summary>
        public const string Status_Unopened = "unopened";
        #endregion

        #region Time Interval Constants
        /// <summary>
        /// Represents a one-minute time interval for candlestick data.
        /// </summary>
        public const string Interval_Minute = "minute";

        /// <summary>
        /// Represents a one-hour time interval for candlestick data.
        /// </summary>
        public const string Interval_Hour = "hour";

        /// <summary>
        /// Represents a one-day time interval for candlestick data.
        /// </summary>
        public const string Interval_Day = "day";
        #endregion

        #region API Parameter Constants
        /// <summary>
        /// Parameter name for specifying the action in API requests (e.g., buy/sell).
        /// </summary>
        public const string Parameter_Action = "action";

        /// <summary>
        /// Parameter name for specifying a series ticker in API requests.
        /// </summary>
        public const string Parameter_Series_Ticker = "series-ticker";

        /// <summary>
        /// Parameter name for specifying a market ticker in API requests.
        /// </summary>
        public const string Parameter_Market_Ticker = "market-ticker";

        /// <summary>
        /// Parameter name for specifying an event ticker in API requests.
        /// </summary>
        public const string Parameter_Event_Ticker = "event-ticker";

        /// <summary>
        /// Parameter name for specifying a time interval in API requests.
        /// </summary>
        public const string Parameter_Interval = "interval";

        /// <summary>
        /// Parameter name for specifying a start timestamp in API requests.
        /// </summary>
        public const string Parameter_Start_Ts = "start-ts";

        /// <summary>
        /// Parameter name for specifying a start timestamp in minutes in API requests.
        /// </summary>
        public const string Parameter_Start_Ts_m = "start-ts-m";

        /// <summary>
        /// Parameter name for specifying a start timestamp in hours in API requests.
        /// </summary>
        public const string Parameter_Start_Ts_h = "start-ts-h";

        /// <summary>
        /// Parameter name for specifying a start timestamp in days in API requests.
        /// </summary>
        public const string Parameter_Start_Ts_d = "start-ts-d";

        /// <summary>
        /// Parameter name for specifying an end timestamp in API requests.
        /// </summary>
        public const string Parameter_End_Ts = "end-ts";

        /// <summary>
        /// Parameter name for specifying market status in API requests.
        /// </summary>
        public const string Parameter_Status = "status";

        /// <summary>
        /// Parameter name for including nested markets in API responses.
        /// </summary>
        public const string Parameter_With_Nested_Markets = "with-nested-markets";

        /// <summary>
        /// Parameter name for specifying maximum close timestamp in API requests.
        /// </summary>
        public const string Parameter_Max_Close_TS = "max-close-ts";

        /// <summary>
        /// Parameter name for specifying minimum close timestamp in API requests.
        /// </summary>
        public const string Parameter_Min_Close_TS = "min-close-ts";

        /// <summary>
        /// Parameter name for specifying multiple tickers in API requests.
        /// </summary>
        public const string Parameter_Tickers = "tickers";
        #endregion

        #region WebSocket Feed Type Constants
        /// <summary>
        /// Feed type for ticker updates containing price and volume information.
        /// </summary>
        public const string ScriptType_Feed_Ticker = "ticker";

        /// <summary>
        /// Feed type for orderbook updates containing bid/ask price changes.
        /// </summary>
        public const string ScriptType_Feed_Orderbook = "orderbook";

        /// <summary>
        /// Feed type for fill updates containing executed trade information.
        /// </summary>
        public const string ScriptType_Feed_Fill = "fill";

        /// <summary>
        /// Feed type for lifecycle updates containing market status changes.
        /// </summary>
        public const string ScriptType_Feed_Lifecycle = "lifecycle";

        /// <summary>
        /// Feed type for trade updates containing individual trade executions.
        /// </summary>
        public const string ScriptType_Feed_Trade = "trade";

        /// <summary>
        /// Feed type for event lifecycle updates containing event-level status changes.
        /// </summary>
        public const string ScriptType_Feed_Event_Lifecycle = "event_lifecycle";
        #endregion

        #region Channel Constants
        /// <summary>
        /// Channel name for orderbook delta updates (incremental changes).
        /// </summary>
        public const string Channel_Orderbook_Delta = "orderbook_delta";

        /// <summary>
        /// Channel name for market lifecycle v2 updates.
        /// </summary>
        public const string Channel_Market_Lifecycle_V2 = "market_lifecycle_v2";
        #endregion

        #region Channel Arrays
        /// <summary>
        /// Array of all available WebSocket feed channels for subscription.
        /// Note: These arrays are static readonly for performance, initialized once at runtime.
        /// For frequent access, consider caching if lookups are needed.
        /// </summary>
        public static readonly string[] AllChannels = {
            ScriptType_Feed_Ticker,
            ScriptType_Feed_Orderbook,
            ScriptType_Feed_Fill,
            ScriptType_Feed_Lifecycle,
            ScriptType_Feed_Trade,
            ScriptType_Feed_Event_Lifecycle
        };

        /// <summary>
        /// Array of market-specific WebSocket feed channels (excluding event-level feeds).
        /// Note: These arrays are static readonly for performance, initialized once at runtime.
        /// For frequent access, consider caching if lookups are needed.
        /// </summary>
        public static readonly string[] MarketChannels = {
            ScriptType_Feed_Ticker,
            ScriptType_Feed_Orderbook,
            ScriptType_Feed_Trade
        };

        /// <summary>
        /// Array of market-specific WebSocket channels for delta updates.
        /// Note: These arrays are static readonly for performance, initialized once at runtime.
        /// For frequent access, consider caching if lookups are needed.
        /// </summary>
        public static readonly string[] MarketChannelsDelta = {
            Channel_Orderbook_Delta,
            ScriptType_Feed_Ticker,
            ScriptType_Feed_Trade
        };
        #endregion

        #region Script Type Constants
        /// <summary>
        /// Script type identifier for market-related API endpoints.
        /// </summary>
        public const string ScriptType_Market = "market";

        /// <summary>
        /// Script type identifier for event-related API endpoints.
        /// </summary>
        public const string ScriptType_Event = "event";

        /// <summary>
        /// Script type identifier for series-related API endpoints.
        /// </summary>
        public const string ScriptType_Series = "series";

        /// <summary>
        /// Script type identifier for candlestick data API endpoints.
        /// </summary>
        public const string ScriptType_Candlestick = "candlestick";

        /// <summary>
        /// Script type identifier for chart candlestick data API endpoints.
        /// </summary>
        public const string ScriptType_ChartCandlesticks = "chartcandlestick";
        #endregion

        #region Utility Methods
        /// <summary>
        /// Truncates a DateTime to the nearest minute boundary, setting seconds and milliseconds to zero.
        /// This is useful for aligning timestamps for candlestick data and time-based calculations.
        /// </summary>
        /// <param name="dt">The DateTime to truncate.</param>
        /// <returns>A new DateTime truncated to the minute.</returns>
        public static DateTime TruncateDateTimeToMinute(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Utc);
        }

        /// <summary>
        /// HashSet of market statuses that indicate the market has ended.
        /// Used for efficient lookup in IsMarketStatusEnded.
        /// </summary>
        private static readonly HashSet<string> EndedStatuses = new HashSet<string>
        {
            Status_Finalized,
            Status_Bad,
            Status_Closed,
            Status_Settled,
            Status_Determined,
            Status_Inactive
        };

        /// <summary>
        /// HashSet of market status enums that indicate the market has ended.
        /// Used for efficient lookup in IsMarketStatusEnded.
        /// </summary>
        private static readonly HashSet<MarketStatus> EndedStatusEnums = new HashSet<MarketStatus>
        {
            MarketStatus.Finalized,
            MarketStatus.Bad,
            MarketStatus.Closed,
            MarketStatus.Settled,
            MarketStatus.Determined,
            MarketStatus.Inactive
        };

        /// <summary>
        /// Determines whether a market status indicates that the market has ended and is no longer active.
        /// Ended markets include those that are finalized, bad, closed, settled, determined, or inactive.
        /// </summary>
        /// <param name="status">The market status string to evaluate.</param>
        /// <returns>True if the market has ended, false otherwise.</returns>
        public static bool IsMarketStatusEnded(string status)
        {
            return !string.IsNullOrEmpty(status) && EndedStatuses.Contains(status);
        }

        /// <summary>
        /// Determines whether a market status indicates that the market has ended and is no longer active.
        /// </summary>
        /// <param name="status">The market status enum to evaluate.</param>
        /// <returns>True if the market has ended, false otherwise.</returns>
        public static bool IsMarketStatusEnded(MarketStatus status)
        {
            return EndedStatusEnums.Contains(status);
        }
        #endregion
    }
}
