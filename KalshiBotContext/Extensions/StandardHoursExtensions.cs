using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between StandardHours model and StandardHoursDTO,
    /// including nested sessions collection for comprehensive exchange hours data transfer.
    /// </summary>
    public static class StandardHoursExtensions
    {
        /// <summary>
        /// Converts a StandardHours model to its DTO representation,
        /// including all nested session data for complete hours information.
        /// </summary>
        /// <param name="standardHoursModel">The StandardHours model to convert.</param>
        /// <returns>A new StandardHoursDTO with all hours properties and nested sessions mapped.</returns>
        public static StandardHoursDTO ToStandardHoursDTO(this StandardHours standardHoursModel)
        {
            return new StandardHoursDTO
            {
                StandardHoursID = standardHoursModel.StandardHoursID,
                ExchangeScheduleID = standardHoursModel.ExchangeScheduleID,
                StartTime = standardHoursModel.StartTime,
                EndTime = standardHoursModel.EndTime,
                CreatedDate = standardHoursModel.CreatedDate,
                LastModifiedDate = standardHoursModel.LastModifiedDate,
                Sessions = standardHoursModel.Sessions.Select(s => s.ToStandardHoursSessionDTO()).ToList()
            };
        }

        /// <summary>
        /// Converts a StandardHoursDTO to its model representation,
        /// creating a new StandardHours with all properties and nested sessions mapped from the DTO.
        /// </summary>
        /// <param name="standardHoursDTO">The StandardHoursDTO to convert.</param>
        /// <returns>A new StandardHours model with all properties and nested sessions mapped.</returns>
        public static StandardHours ToStandardHours(this StandardHoursDTO standardHoursDTO)
        {
            return new StandardHours
            {
                StandardHoursID = standardHoursDTO.StandardHoursID,
                ExchangeScheduleID = standardHoursDTO.ExchangeScheduleID,
                StartTime = standardHoursDTO.StartTime,
                EndTime = standardHoursDTO.EndTime,
                CreatedDate = standardHoursDTO.CreatedDate,
                LastModifiedDate = standardHoursDTO.LastModifiedDate,
                Sessions = standardHoursDTO.Sessions.Select(s => s.ToStandardHoursSession()).ToList()
            };
        }

        /// <summary>
        /// Updates an existing StandardHours model with data from a StandardHoursDTO,
        /// validating standard hours ID match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="standardHoursModel">The StandardHours model to update.</param>
        /// <param name="standardHoursDTO">The StandardHoursDTO containing updated data.</param>
        /// <returns>The updated StandardHours model.</returns>
        /// <exception cref="Exception">Thrown when standard hours IDs do not match.</exception>
        public static StandardHours UpdateStandardHours(this StandardHours standardHoursModel, StandardHoursDTO standardHoursDTO)
        {
            if (standardHoursModel.StandardHoursID != standardHoursDTO.StandardHoursID)
            {
                throw new Exception("StandardHoursIDs don't match for Update StandardHours");
            }
            standardHoursModel.ExchangeScheduleID = standardHoursDTO.ExchangeScheduleID;
            standardHoursModel.StartTime = standardHoursDTO.StartTime;
            standardHoursModel.EndTime = standardHoursDTO.EndTime;
            standardHoursModel.CreatedDate = standardHoursDTO.CreatedDate;
            standardHoursModel.LastModifiedDate = DateTime.UtcNow;
            return standardHoursModel;
        }
    }
}
