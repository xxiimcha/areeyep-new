using System;
using System.ComponentModel.DataAnnotations;

namespace AreEyeP.Models
{
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // ID of the user who performed the action

        [Required]
        public string UserType { get; set; } // Type of the user (e.g., Admin, Staff, Client, etc.)

        [Required]
        public string Action { get; set; } // The action performed (e.g., "Login", "Add Payment")

        [Required]
        public string Details { get; set; } // Additional details about the action

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // When the action occurred
    }
}
