using System;
using System.ComponentModel.DataAnnotations;

namespace AreEyeP.Models
{
    public class Catacomb
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CatacombID { get; set; }

        [Required]
        public string CatacombName { get; set; }

        public string Location { get; set; }

        [Required]
        [Display(Name = "Availability Status")]
        public string AvailabilityStatus { get; set; } = "Available"; // Default to Available

        [Display(Name = "Date Created")]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public string DeceasedInformation { get; set; }
    }
}
