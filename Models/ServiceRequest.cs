using System;
using System.ComponentModel.DataAnnotations;

namespace AreEyeP.Models
{
    public class ServiceRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DeceasedId { get; set; }

        [Required]
        public DateTime DateOfService { get; set; }

        [Required]
        public string ServiceType { get; set; } = "Unknown Service Type"; // Default value

        [Required]
        public string UrgencyLevel { get; set; } = "Low"; // Default value

        public string SpecialInstructions { get; set; } = "None"; // Default value

        [Required]
        public string Staff { get; set; } = "Unassigned"; // Default value

        [Required]
        public string Status { get; set; } = "Pending"; // Default value

        [Required]
        public TimeSpan StartTime { get; set; } = TimeSpan.FromHours(9); // Default value

        [Required]
        public TimeSpan EndTime { get; set; } = TimeSpan.FromHours(17); // Default value

        public decimal? Amount { get; set; } = 0; // Default value

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Default value

        public DateTime? UpdatedAt { get; set; } = null; // Allow null initially

        // StaffContact is optional; set a default value
        public string StaffContact { get; set; } = "N/A"; // Default value

        [Required]
        public bool PaymentRequired { get; set; } = false; // Default value
    }
}
