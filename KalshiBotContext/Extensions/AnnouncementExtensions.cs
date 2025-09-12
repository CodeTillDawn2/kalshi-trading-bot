using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between Announcement model and AnnouncementDTO,
    /// enabling seamless data transfer between the database layer and external interfaces.
    /// </summary>
    public static class AnnouncementExtensions
    {
        /// <summary>
        /// Converts an Announcement model instance to its corresponding DTO representation,
        /// mapping all properties for data transfer operations.
        /// </summary>
        /// <param name="announcementModel">The Announcement model to convert.</param>
        /// <returns>A new AnnouncementDTO with all properties mapped from the model.</returns>
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

        /// <summary>
        /// Converts an AnnouncementDTO to its corresponding model representation,
        /// creating a new Announcement instance with all properties mapped from the DTO.
        /// </summary>
        /// <param name="announcementDTO">The AnnouncementDTO to convert.</param>
        /// <returns>A new Announcement model with all properties mapped from the DTO.</returns>
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

        /// <summary>
        /// Updates an existing Announcement model with data from an AnnouncementDTO,
        /// ensuring the announcement IDs match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="announcementModel">The Announcement model to update.</param>
        /// <param name="announcementDTO">The AnnouncementDTO containing the updated data.</param>
        /// <returns>The updated Announcement model.</returns>
        /// <exception cref="Exception">Thrown when the announcement IDs do not match.</exception>
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
            announcementModel.LastModifiedDate = DateTime.UtcNow;
            return announcementModel;
        }
    }
}
