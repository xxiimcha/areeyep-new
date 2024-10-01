using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AreEyeP.Models
{
    public class Schedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Schedule ID")]
        public string ScheduleID { get; set; } // This will hold the formatted ID like "SCHD-123"

        [Required]
        [Display(Name = "Client Name")]
        public string ClientName { get; set; }

        [Required]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Details")]
        public string Details { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending"; // Default status

        // Foreign Key for the User who created the schedule
        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        // Foreign Key for the associated Client
        [Required]
        [ForeignKey("Client")]
        public int ClientId { get; set; }

        // Timestamp fields
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Automatically set the creation timestamp

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; } // This will be updated whenever the record is modified
    }
}
