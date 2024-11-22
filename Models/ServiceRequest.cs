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
        public string Staff { get; set; } = "Unassigned";

        [Required]
        public string Status { get; set; } = "Pending";

        [Required]
        public TimeSpan StartTime { get; set; } = TimeSpan.FromHours(9);

        [Required]
        public TimeSpan EndTime { get; set; } = TimeSpan.FromHours(17);

        public decimal? Amount { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // StaffContact is optional; no [Required] attribute.
        public string StaffContact { get; set; }

        [Required]
        public bool PaymentRequired { get; set; } = false;
    }

}
