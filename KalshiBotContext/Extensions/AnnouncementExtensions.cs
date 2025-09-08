using KalshiBotData.Models;
using SmokehouseDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class AnnouncementExtensions
    {
        public static AnnouncementDTO ToAnnouncementDTO(this Announcement announcementModel)
        {
            return new AnnouncementDTO
            {
                AnnouncementID = announcementModel.AnnouncementID,
                DeliveryTime = announcementModel.DeliveryTime,
                Message = announcementModel.Message,
                Status = announcementModel.Status,
                Type = announcementModel.Type,
                CreatedDate = announcementModel.CreatedDate,
                LastModifiedDate = announcementModel.LastModifiedDate
            };
        }

        public static Announcement ToAnnouncement(this AnnouncementDTO announcementDTO)
        {
            return new Announcement
            {
                AnnouncementID = announcementDTO.AnnouncementID,
                DeliveryTime = announcementDTO.DeliveryTime,
                Message = announcementDTO.Message,
                Status = announcementDTO.Status,
                Type = announcementDTO.Type,
                CreatedDate = announcementDTO.CreatedDate,
                LastModifiedDate = announcementDTO.LastModifiedDate
            };
        }

        public static Announcement UpdateAnnouncement(this Announcement announcementModel, AnnouncementDTO announcementDTO)
        {
            if (announcementModel.AnnouncementID != announcementDTO.AnnouncementID)
            {
                throw new Exception("AnnouncementIDs don't match for Update Announcement");
            }
            announcementModel.DeliveryTime = announcementDTO.DeliveryTime;
            announcementModel.Message = announcementDTO.Message;
            announcementModel.Status = announcementDTO.Status;
            announcementModel.Type = announcementDTO.Type;
            announcementModel.CreatedDate = announcementDTO.CreatedDate;
            announcementModel.LastModifiedDate = DateTime.Now;
            return announcementModel;
        }
    }
}