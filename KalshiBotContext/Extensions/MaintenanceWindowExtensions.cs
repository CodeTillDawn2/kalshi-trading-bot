using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between MaintenanceWindow model and MaintenanceWindowDTO,
    /// supporting exchange maintenance schedule data transfer operations.
    /// </summary>
    public static class MaintenanceWindowExtensions
    {
        /// <summary>
        /// Converts a MaintenanceWindow model to its DTO representation,
        /// mapping all maintenance window properties for data transfer.
        /// </summary>
        /// <param name="maintenanceWindowModel">The MaintenanceWindow model to convert.</param>
        /// <returns>A new MaintenanceWindowDTO with all maintenance window properties mapped.</returns>
        public static MaintenanceWindowDTO ToMaintenanceWindowDTO(this MaintenanceWindow maintenanceWindowModel)
        {
            return new MaintenanceWindowDTO
            {
                MaintenanceWindowID = maintenanceWindowModel.MaintenanceWindowID,
                ExchangeScheduleID = maintenanceWindowModel.ExchangeScheduleID,
                StartDateTime = maintenanceWindowModel.StartDateTime,
                EndDateTime = maintenanceWindowModel.EndDateTime,
                CreatedDate = maintenanceWindowModel.CreatedDate,
                LastModifiedDate = maintenanceWindowModel.LastModifiedDate
            };
        }

        /// <summary>
        /// Converts a MaintenanceWindowDTO to its model representation,
        /// creating a new MaintenanceWindow with all properties mapped from the DTO.
        /// </summary>
        /// <param name="maintenanceWindowDTO">The MaintenanceWindowDTO to convert.</param>
        /// <returns>A new MaintenanceWindow model with all properties mapped from the DTO.</returns>
        public static MaintenanceWindow ToMaintenanceWindow(this MaintenanceWindowDTO maintenanceWindowDTO)
        {
            return new MaintenanceWindow
            {
                MaintenanceWindowID = maintenanceWindowDTO.MaintenanceWindowID,
                ExchangeScheduleID = maintenanceWindowDTO.ExchangeScheduleID,
                StartDateTime = maintenanceWindowDTO.StartDateTime,
                EndDateTime = maintenanceWindowDTO.EndDateTime,
                CreatedDate = maintenanceWindowDTO.CreatedDate,
                LastModifiedDate = maintenanceWindowDTO.LastModifiedDate
            };
        }

        /// <summary>
        /// Updates an existing MaintenanceWindow model with data from a MaintenanceWindowDTO,
        /// validating maintenance window ID match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="maintenanceWindowModel">The MaintenanceWindow model to update.</param>
        /// <param name="maintenanceWindowDTO">The MaintenanceWindowDTO containing updated data.</param>
        /// <returns>The updated MaintenanceWindow model.</returns>
        /// <exception cref="Exception">Thrown when maintenance window IDs do not match.</exception>
        public static MaintenanceWindow UpdateMaintenanceWindow(this MaintenanceWindow maintenanceWindowModel, MaintenanceWindowDTO maintenanceWindowDTO)
        {
            if (maintenanceWindowModel.MaintenanceWindowID != maintenanceWindowDTO.MaintenanceWindowID)
            {
                throw new Exception("MaintenanceWindowIDs don't match for Update MaintenanceWindow");
            }
            maintenanceWindowModel.ExchangeScheduleID = maintenanceWindowDTO.ExchangeScheduleID;
            maintenanceWindowModel.StartDateTime = maintenanceWindowDTO.StartDateTime;
            maintenanceWindowModel.EndDateTime = maintenanceWindowDTO.EndDateTime;
            maintenanceWindowModel.CreatedDate = maintenanceWindowDTO.CreatedDate;
            maintenanceWindowModel.LastModifiedDate = DateTime.UtcNow;
            return maintenanceWindowModel;
        }
    }
}
