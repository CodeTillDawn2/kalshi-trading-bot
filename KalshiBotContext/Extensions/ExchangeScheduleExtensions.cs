using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between ExchangeSchedule model and ExchangeScheduleDTO,
    /// including nested maintenance windows and standard hours collections for comprehensive schedule data transfer.
    /// </summary>
    public static class ExchangeScheduleExtensions
    {
        /// <summary>
        /// Converts an ExchangeSchedule model to its DTO representation,
        /// including all nested maintenance windows and standard hours data.
        /// </summary>
        /// <param name="exchangeScheduleModel">The ExchangeSchedule model to convert.</param>
        /// <returns>A new ExchangeScheduleDTO with all schedule properties and nested collections mapped.</returns>
        public static ExchangeScheduleDTO ToExchangeScheduleDTO(this ExchangeSchedule exchangeScheduleModel)
        {
            return new ExchangeScheduleDTO
            {
                ExchangeScheduleID = exchangeScheduleModel.ExchangeScheduleID,
                LastUpdated = exchangeScheduleModel.LastUpdated,
                CreatedDate = exchangeScheduleModel.CreatedDate,
                LastModifiedDate = exchangeScheduleModel.LastModifiedDate,
                MaintenanceWindows = exchangeScheduleModel.MaintenanceWindows.Select(mw => mw.ToMaintenanceWindowDTO()).ToList(),
                StandardHours = exchangeScheduleModel.StandardHours.Select(sh => sh.ToStandardHoursDTO()).ToList()
            };
        }

        /// <summary>
        /// Converts an ExchangeScheduleDTO to its model representation,
        /// creating a new ExchangeSchedule with all properties and nested collections mapped from the DTO.
        /// </summary>
        /// <param name="exchangeScheduleDTO">The ExchangeScheduleDTO to convert.</param>
        /// <returns>A new ExchangeSchedule model with all properties and nested collections mapped.</returns>
        public static ExchangeSchedule ToExchangeSchedule(this ExchangeScheduleDTO exchangeScheduleDTO)
        {
            return new ExchangeSchedule
            {
                ExchangeScheduleID = exchangeScheduleDTO.ExchangeScheduleID,
                LastUpdated = exchangeScheduleDTO.LastUpdated,
                CreatedDate = exchangeScheduleDTO.CreatedDate,
                LastModifiedDate = exchangeScheduleDTO.LastModifiedDate,
                MaintenanceWindows = exchangeScheduleDTO.MaintenanceWindows.Select(mw => mw.ToMaintenanceWindow()).ToList(),
                StandardHours = exchangeScheduleDTO.StandardHours.Select(sh => sh.ToStandardHours()).ToList()
            };
        }

        /// <summary>
        /// Updates an existing ExchangeSchedule model with data from an ExchangeScheduleDTO,
        /// validating schedule ID match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="exchangeScheduleModel">The ExchangeSchedule model to update.</param>
        /// <param name="exchangeScheduleDTO">The ExchangeScheduleDTO containing updated data.</param>
        /// <returns>The updated ExchangeSchedule model.</returns>
        /// <exception cref="Exception">Thrown when exchange schedule IDs do not match.</exception>
        public static ExchangeSchedule UpdateExchangeSchedule(this ExchangeSchedule exchangeScheduleModel, ExchangeScheduleDTO exchangeScheduleDTO)
        {
            if (exchangeScheduleModel.ExchangeScheduleID != exchangeScheduleDTO.ExchangeScheduleID)
            {
                throw new Exception("ExchangeScheduleIDs don't match for Update ExchangeSchedule");
            }
            exchangeScheduleModel.LastUpdated = exchangeScheduleDTO.LastUpdated;
            exchangeScheduleModel.CreatedDate = exchangeScheduleDTO.CreatedDate;
            exchangeScheduleModel.LastModifiedDate = DateTime.UtcNow;
            return exchangeScheduleModel;
        }
    }
}
