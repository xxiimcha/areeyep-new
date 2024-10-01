using System;
using System.ComponentModel.DataAnnotations;

namespace AreEyeP.Models
{
    public class BurialApplication
    {
        [Key]
        public int Id { get; set; }

        // Applicant Information
        [Required]
        [Display(Name = "Applicant First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Applicant Last Name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Applicant Address")]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Relationship to Deceased")]
        public string RelationshipToDeceased { get; set; }

        [Required]
        [Display(Name = "Contact Information")]
        public string ContactInformation { get; set; }

        // Deceased Information
        [Required]
        [Display(Name = "Deceased First Name")]
        public string DeceasedFirstName { get; set; }  // Changed property name to be more specific

        [Required]
        [Display(Name = "Deceased Last Name")]
        public string DeceasedLastName { get; set; }  // Changed property name to be more specific

        [Required]
        public string Gender { get; set; }

        [Required]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [Display(Name = "Date of Death")]
        [DataType(DataType.Date)]
        public DateTime DateOfDeath { get; set; }

        [Required]
        public string CauseOfDeath { get; set; }

        public string Religion { get; set; } = "Roman Catholic";  // Default to Roman Catholic if not specified

        // File attachment path
        [Display(Name = "Attachment Path")]
        public string? AttachmentPath { get; set; }  // Make this nullable

        // Burial Details
        [Required]
        [Display(Name = "Date of Burial")]
        [DataType(DataType.Date)]
        public DateTime DateOfBurial { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Special Instructions")]
        public string SpecialInstructions { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Link to the user who submitted the application
        public int UserId { get; set; }

        // Application Status
        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";  // Default to "Pending"
    }
}
