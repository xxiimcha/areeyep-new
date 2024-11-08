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
        public string ServiceType { get; set; }

        [Required]
        public string UrgencyLevel { get; set; }

        public string SpecialInstructions { get; set; }

        [Required]
        public string Staff { get; set; } // Assigned staff for the service

        [Required]
        public string Status { get; set; } // Status of the service request (e.g., Pending, Approved, Completed)

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        // User who created the service request
        [Required]
        public int UserId { get; set; } // Foreign key to link the request with a user

        // Timestamp fields
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Set default to current time

        public DateTime? UpdatedAt { get; set; } // Nullable to track when the record is updated
    }
}
