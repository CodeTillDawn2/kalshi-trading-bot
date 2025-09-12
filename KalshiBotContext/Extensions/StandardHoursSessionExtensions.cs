using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between StandardHoursSession model and StandardHoursSessionDTO,
    /// supporting individual session data transfer within exchange standard hours.
    /// </summary>
    public static class StandardHoursSessionExtensions
    {
        /// <summary>
        /// Converts a StandardHoursSession model to its DTO representation,
        /// mapping all session properties for data transfer.
        /// </summary>
        /// <param name="standardHoursSessionModel">The StandardHoursSession model to convert.</param>
        /// <returns>A new StandardHoursSessionDTO with all session properties mapped.</returns>
        public static StandardHoursSessionDTO ToStandardHoursSessionDTO(this StandardHoursSession standardHoursSessionModel)
        {
            return new StandardHoursSessionDTO
            {
                SessionID = standardHoursSessionModel.SessionID,
                StandardHoursID = standardHoursSessionModel.StandardHoursID,
                DayOfWeek = standardHoursSessionModel.DayOfWeek,
                StartTime = standardHoursSessionModel.StartTime,
                EndTime = standardHoursSessionModel.EndTime,
                CreatedDate = standardHoursSessionModel.CreatedDate,
                LastModifiedDate = standardHoursSessionModel.LastModifiedDate
            };
        }

        /// <summary>
        /// Converts a StandardHoursSessionDTO to its model representation,
        /// creating a new StandardHoursSession with all properties mapped from the DTO.
        /// </summary>
        /// <param name="standardHoursSessionDTO">The StandardHoursSessionDTO to convert.</param>
        /// <returns>A new StandardHoursSession model with all properties mapped from the DTO.</returns>
        public static StandardHoursSession ToStandardHoursSession(this StandardHoursSessionDTO standardHoursSessionDTO)
        {
            return new StandardHoursSession
            {
                SessionID = standardHoursSessionDTO.SessionID,
                StandardHoursID = standardHoursSessionDTO.StandardHoursID,
                DayOfWeek = standardHoursSessionDTO.DayOfWeek,
                StartTime = standardHoursSessionDTO.StartTime,
                EndTime = standardHoursSessionDTO.EndTime,
                CreatedDate = standardHoursSessionDTO.CreatedDate,
                LastModifiedDate = standardHoursSessionDTO.LastModifiedDate
            };
        }

        /// <summary>
        /// Updates an existing StandardHoursSession model with data from a StandardHoursSessionDTO,
        /// validating session ID match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="standardHoursSessionModel">The StandardHoursSession model to update.</param>
        /// <param name="standardHoursSessionDTO">The StandardHoursSessionDTO containing updated data.</param>
        /// <returns>The updated StandardHoursSession model.</returns>
        /// <exception cref="Exception">Thrown when session IDs do not match.</exception>
        public static StandardHoursSession UpdateStandardHoursSession(this StandardHoursSession standardHoursSessionModel, StandardHoursSessionDTO standardHoursSessionDTO)
        {
            if (standardHoursSessionModel.SessionID != standardHoursSessionDTO.SessionID)
            {
                throw new Exception("SessionIDs don't match for Update StandardHoursSession");
            }
            standardHoursSessionModel.StandardHoursID = standardHoursSessionDTO.StandardHoursID;
            standardHoursSessionModel.DayOfWeek = standardHoursSessionDTO.DayOfWeek;
            standardHoursSessionModel.StartTime = standardHoursSessionDTO.StartTime;
            standardHoursSessionModel.EndTime = standardHoursSessionDTO.EndTime;
            standardHoursSessionModel.CreatedDate = standardHoursSessionDTO.CreatedDate;
            standardHoursSessionModel.LastModifiedDate = DateTime.UtcNow;
            return standardHoursSessionModel;
        }
    }
}
