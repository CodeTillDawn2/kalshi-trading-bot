namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents the current weekly schedule for the Kalshi exchange trading hours.
    /// This entity stores the simplified daily trading schedule with start and end times
    /// for each day of the week, providing a flattened view of the exchange's operating hours.
    /// </summary>
    public class CurrentSchedule
    {
        /// <summary>
        /// Gets or sets the day of the week for this schedule entry.
        /// This serves as the primary key and identifies which day this schedule applies to.
        /// </summary>
        public required string DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the start time for trading on this day.
        /// This represents when the exchange opens for trading on the specified day.
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time for trading on this day.
        /// This represents when the exchange closes for trading on the specified day.
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this schedule entry was last modified.
        /// This helps track when the schedule was last updated from the exchange data.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }
    }
}