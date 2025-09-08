using KalshiBotData.Models;
using SmokehouseDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class MaintenanceWindowExtensions
    {
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
            maintenanceWindowModel.LastModifiedDate = DateTime.Now;
            return maintenanceWindowModel;
        }
    }
}