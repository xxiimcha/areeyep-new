using System;
using System.ComponentModel.DataAnnotations;

namespace AreEyeP.Models
{
    public class BurialApplicationViewModel
    {
        [Key]
        public int Id { get; set; }

        // Deceased Information
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Gender { get; set; }

        // Date of Birth
        [Required]
        public int BirthYear { get; set; }

        [Required]
        public int BirthMonth { get; set; }

        [Required]
        public int BirthDay { get; set; }

        // Date of Death
        [Required]
        public int DeathYear { get; set; }

        [Required]
        public int DeathMonth { get; set; }

        [Required]
        public int DeathDay { get; set; }

        [Required]
        public string CauseOfDeath { get; set; }

        public string Religion { get; set; }

        // File Attachment Path
        public string AttachmentPath { get; set; }

        // Burial Details
        [Required]
        public int BurialYear { get; set; }

        [Required]
        public int BurialMonth { get; set; }

        [Required]
        public int BurialDay { get; set; }

        [Required]
        public string StartTime { get; set; }

        [Required]
        public string EndTime { get; set; }

        public string SpecialInstructions { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
    }
}
