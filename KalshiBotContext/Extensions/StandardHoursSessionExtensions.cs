using KalshiBotData.Models;
using SmokehouseDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class StandardHoursSessionExtensions
    {
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
            standardHoursSessionModel.LastModifiedDate = DateTime.Now;
            return standardHoursSessionModel;
        }
    }
}