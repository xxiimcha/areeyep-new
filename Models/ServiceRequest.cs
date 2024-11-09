using System;
using System.ComponentModel.DataAnnotations;

namespace AreEyeP.Models
{
    public class ServiceRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DeceasedId { get; set; } // String to match nvarchar(MAX)

        [Required]
        public DateTime DateOfService { get; set; }

        [Required]
        public string ServiceType { get; set; } // String to match nvarchar(MAX)

        [Required]
        public string UrgencyLevel { get; set; } // String to match nvarchar(MAX)

        public string SpecialInstructions { get; set; } // Optional, string to match nvarchar(MAX)

        [Required]
        public string Staff { get; set; } = "Unassigned"; // Default value

        [Required]
        public string Status { get; set; } = "Pending"; // Default value

        [Required]
        public TimeSpan StartTime { get; set; } = TimeSpan.FromHours(9); // Default start time

        [Required]
        public TimeSpan EndTime { get; set; } = TimeSpan.FromHours(17); // Default end time

        public decimal? Amount { get; set; } // Optional

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
