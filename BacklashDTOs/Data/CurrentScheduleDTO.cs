namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for current schedule data.
    /// Represents the simplified daily trading schedule for the Kalshi exchange.
    /// </summary>
    public class CurrentScheduleDTO
    {
        /// <summary>
        /// Gets or sets the day of the week for this schedule entry.
        /// </summary>
        public required string DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the start time for trading on this day.
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time for trading on this day.
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this schedule entry was last modified.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }
    }
}