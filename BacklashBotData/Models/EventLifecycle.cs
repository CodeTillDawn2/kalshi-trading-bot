namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents the complete lifecycle state and timing information for a trading event in the Kalshi system.
    /// This entity tracks the progression of an event from opening to settlement, including all critical
    /// timestamps and state changes. It serves as the authoritative source for event status and timing
    /// used throughout the trading bot for market state management and strategy execution.
    /// </summary>
    public class EventLifecycle
    {
        /// <summary>
        /// Gets or sets the market ticker identifier for this event lifecycle.
        /// This uniquely identifies the market within the Kalshi trading system.
        /// </summary>
        public required string market_ticker { get; set; }

        /// <summary>
        /// Gets or sets the Unix timestamp when this event was opened for trading.
        /// This marks the beginning of the trading period for this event.
        /// </summary>
        public long open_ts { get; set; }

        /// <summary>
        /// Gets or sets the Unix timestamp when this event was closed for trading.
        /// After this time, no new orders can be placed for this event.
        /// </summary>
        public long close_ts { get; set; }

        /// <summary>
        /// Gets or sets the Unix timestamp when the outcome of this event was determined.
        /// This occurs after the event closes and the result becomes known.
        /// </summary>
        public long? determination_ts { get; set; }

        /// <summary>
        /// Gets or sets the Unix timestamp when this event was fully settled.
        /// This is when all payouts are processed and the event is completely resolved.
        /// </summary>
        public long? settled_ts { get; set; }

        /// <summary>
        /// Gets or sets the final result or outcome of this event.
        /// This determines which side of the market (Yes/No) was correct.
        /// </summary>
        public required string result { get; set; }

        /// <summary>
        /// Gets or sets whether this event has been deactivated or removed from active trading.
        /// Deactivated events are no longer available for trading but may still be visible for historical purposes.
        /// </summary>
        public bool is_deactivated { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this lifecycle data was logged in the system.
        /// This is used for auditing and tracking when the data was first captured.
        /// </summary>
        public DateTime LoggedDate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this lifecycle data was processed by the system.
        /// This indicates when the data was validated and made available for use.
        /// </summary>
        public DateTime? ProcessedDate { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated Market entity.
        /// This provides access to the full market details and related data.
        /// </summary>
        public required Market Market { get; set; }
    }
}
