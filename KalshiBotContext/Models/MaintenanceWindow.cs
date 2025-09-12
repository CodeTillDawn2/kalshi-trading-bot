using System;

namespace KalshiBotData.Models
{
    /// <summary>
    /// Represents a scheduled maintenance window for the Kalshi exchange.
    /// This entity defines periods when the exchange is unavailable for trading due to
    /// planned maintenance, upgrades, or system updates. Maintenance windows are critical
    /// for coordinating system downtime and ensuring trading bots can react appropriately
    /// to planned service interruptions.
    /// </summary>
    public class MaintenanceWindow
    {
        /// <summary>
        /// Gets or sets the unique identifier for this maintenance window.
        /// This serves as the primary key in the database.
        /// </summary>
        public long MaintenanceWindowID { get; set; }

        /// <summary>
        /// Gets or sets the foreign key reference to the parent exchange schedule.
        /// This links the maintenance window to its associated schedule configuration.
        /// </summary>
        public long ExchangeScheduleID { get; set; }

        /// <summary>
        /// Gets or sets the start date and time of this maintenance window.
        /// Trading should be suspended before this time to avoid disruptions.
        /// </summary>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the end date and time of this maintenance window.
        /// Trading can resume after this time when the exchange becomes available again.
        /// </summary>
        public DateTime EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this maintenance window was created in the system.
        /// This is used for auditing and tracking maintenance scheduling history.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this maintenance window was last modified.
        /// This helps track changes to maintenance schedules and timing.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated ExchangeSchedule entity.
        /// This provides access to the parent schedule and related maintenance windows.
        /// </summary>
        public ExchangeSchedule ExchangeSchedule { get; set; }
    }
}
