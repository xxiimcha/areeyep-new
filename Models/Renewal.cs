using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AreEyeP.Models
{
    public class Renewal
    {
        [Key]
        public int RenewalID { get; set; } // Primary Key

        [ForeignKey("User")]
        public int UserID { get; set; } // Foreign Key referencing User

        [ForeignKey("Deceased")]
        public int DeceasedID { get; set; } // Foreign Key referencing Deceased

        [DataType(DataType.Date)]
        public DateTime RenewalDate { get; set; }

        [StringLength(50)]
        public string RenewalStatus { get; set; }

        public int NumberOfYearsContract { get; set; }

        // Navigation Properties
        public virtual User User { get; set; } // Navigation property to User model

        [StringLength(12)]
        public string Deceased { get; set; }

        // Timestamp for creation or last update
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Set default to current UTC time
    }
}
