using KalshiBotData.Models;
using SmokehouseDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class StandardHoursExtensions
    {
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
            standardHoursModel.LastModifiedDate = DateTime.Now;
            return standardHoursModel;
        }
    }
}