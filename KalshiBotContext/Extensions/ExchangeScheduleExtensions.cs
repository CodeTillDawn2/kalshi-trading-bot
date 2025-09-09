using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class ExchangeScheduleExtensions
    {
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

        public static ExchangeSchedule UpdateExchangeSchedule(this ExchangeSchedule exchangeScheduleModel, ExchangeScheduleDTO exchangeScheduleDTO)
        {
            if (exchangeScheduleModel.ExchangeScheduleID != exchangeScheduleDTO.ExchangeScheduleID)
            {
                throw new Exception("ExchangeScheduleIDs don't match for Update ExchangeSchedule");
            }
            exchangeScheduleModel.LastUpdated = exchangeScheduleDTO.LastUpdated;
            exchangeScheduleModel.CreatedDate = exchangeScheduleDTO.CreatedDate;
            exchangeScheduleModel.LastModifiedDate = DateTime.Now;
            return exchangeScheduleModel;
        }
    }
}
