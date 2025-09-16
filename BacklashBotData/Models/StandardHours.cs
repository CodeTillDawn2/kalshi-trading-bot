using System;

namespace KalshiBotData.Models
{
    /// <summary>
    /// Represents the standard operating hours configuration for the Kalshi exchange.
    /// This entity defines the regular trading schedule and time periods when the exchange
    /// is open for business. Standard hours are used to coordinate trading activities,
    /// schedule maintenance, and ensure proper market operation timing across the platform.
    /// </summary>
    public class StandardHours
    {
        /// <summary>
        /// Gets or sets the unique identifier for this standard hours configuration.
        /// This serves as the primary key in the database.
        /// </summary>
        public long StandardHoursID { get; set; }

        /// <summary>
        /// Gets or sets the foreign key reference to the parent exchange schedule.
        /// This links the standard hours to their associated schedule configuration.
        /// </summary>
        public long ExchangeScheduleID { get; set; }

        /// <summary>
        /// Gets or sets the start time for these standard operating hours.
        /// This marks when the exchange begins regular trading operations.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time for these standard operating hours.
        /// This marks when the exchange concludes regular trading operations.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this standard hours configuration was created.
        /// This is used for auditing and tracking schedule history.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this standard hours configuration was last modified.
        /// This helps track changes to the operating schedule.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated ExchangeSchedule entity.
        /// This provides access to the parent schedule and related configuration.
        /// </summary>
        public ExchangeSchedule ExchangeSchedule { get; set; }

        /// <summary>
        /// Gets or sets the collection of sessions that make up these standard hours.
        /// Sessions define specific time periods within the overall operating hours.
        /// </summary>
        public ICollection<StandardHoursSession> Sessions { get; set; } = new List<StandardHoursSession>();
    }
}
