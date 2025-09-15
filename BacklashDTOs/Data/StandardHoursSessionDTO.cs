using System;

namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for standard hours session data.
    /// </summary>
    public class StandardHoursSessionDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier for the session.
        /// </summary>
        public long SessionID { get; set; }

        /// <summary>
        /// Gets or sets the standard hours identifier.
        /// </summary>
        public long StandardHoursID { get; set; }

        /// <summary>
        /// Gets or sets the day of the week.
        /// </summary>
        public string? DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the last modified date.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }
    }
}
