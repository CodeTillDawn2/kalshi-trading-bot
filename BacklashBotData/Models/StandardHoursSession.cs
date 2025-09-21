namespace KalshiBotData.Models
{
    /// <summary>
    /// Represents a specific session within the standard operating hours of the Kalshi exchange.
    /// This entity defines individual time periods on specific days when trading is active.
    /// Sessions allow for flexible scheduling where different days may have different trading
    /// windows, and multiple sessions can exist within a single day's operating hours.
    /// </summary>
    public class StandardHoursSession
    {
        /// <summary>
        /// Gets or sets the unique identifier for this session.
        /// This serves as the primary key in the database.
        /// </summary>
        public long SessionID { get; set; }

        /// <summary>
        /// Gets or sets the foreign key reference to the parent standard hours configuration.
        /// This links the session to its associated operating hours schedule.
        /// </summary>
        public long StandardHoursID { get; set; }

        /// <summary>
        /// Gets or sets the day of the week for this session.
        /// This specifies which day (e.g., "Monday", "Tuesday") this session applies to.
        /// </summary>
        public string DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the start time for this trading session.
        /// This marks when trading begins for this specific session on the specified day.
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time for this trading session.
        /// This marks when trading concludes for this specific session on the specified day.
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this session was created.
        /// This is used for auditing and tracking session configuration history.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this session was last modified.
        /// This helps track changes to the session schedule.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated StandardHours entity.
        /// This provides access to the parent operating hours configuration.
        /// </summary>
        public StandardHours StandardHours { get; set; }
    }
}
