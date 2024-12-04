using System;
using System.ComponentModel.DataAnnotations;

namespace AreEyeP.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; } // Primary key

        [Required(ErrorMessage = "Notification message is required.")]
        [MaxLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
        public string Message { get; set; } // The notification content

        public string TargetUser { get; set; } // Optional: For targeted user notifications (e.g., username or role)

        public int? UserId { get; set; } // Optional: Reference to the user ID, nullable for system-wide notifications

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Timestamp for when the notification was created

        [Required]
        public bool IsRead { get; set; } = false; // Tracks whether the notification has been read

        [MaxLength(100)]
        public string NotificationType { get; set; } // Optional: Categorize notifications (e.g., "info", "warning", "error")
    }
}
