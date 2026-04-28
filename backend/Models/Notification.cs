using System;
using System.ComponentModel.DataAnnotations;
namespace CarpoolApp.Server.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(50, ErrorMessage = "Message cannot exceed 50 characters.")]
        public string Message { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "User ID is required.")]
        public int UserId { get; set; }

        public User User { get; set; }

        [Required(ErrorMessage = "Notification Type is required.")]
        public NotificationType Type { get; set; } // Enum for notification types
    }

    public enum NotificationType
    {
        RideRequest,
        RideAccepted,
        RideCancelled,
    }

}