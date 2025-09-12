using System;

namespace KalshiBotData.Models
{
    /// <summary>
    /// Represents the operational schedule and maintenance configuration for the Kalshi exchange.
    /// This entity manages the overall timing and availability of the trading platform, including
    /// regular operating hours and scheduled maintenance periods. It serves as the master schedule
    /// that coordinates all trading activities and system maintenance across the platform.
    /// </summary>
    public class ExchangeSchedule
    {
        /// <summary>
        /// Gets or sets the unique identifier for this exchange schedule.
        /// This serves as the primary key in the database.
        /// </summary>
        public long ExchangeScheduleID { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this schedule was last updated from the exchange.
        /// This indicates the freshness of the schedule data.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this schedule record was created in the system.
        /// This is used for auditing and tracking schedule history.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this schedule was last modified.
        /// This helps track changes to the schedule configuration.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the collection of maintenance windows associated with this exchange schedule.
        /// These define periods when the exchange is unavailable for trading due to maintenance.
        /// </summary>
        public ICollection<MaintenanceWindow> MaintenanceWindows { get; set; } = new List<MaintenanceWindow>();

        /// <summary>
        /// Gets or sets the collection of standard operating hours for this exchange schedule.
        /// These define the regular trading hours and sessions for the exchange.
        /// </summary>
        public ICollection<StandardHours> StandardHours { get; set; } = new List<StandardHours>();
    }
}
