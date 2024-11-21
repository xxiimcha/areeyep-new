using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AreEyeP.Models
{
    public class ClientPayment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int ApplicationId { get; set; } // Link to the BurialApplication

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters.")]
        public string PaymentMethod { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters.")]
        public string Status { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [StringLength(100, ErrorMessage = "Reference number cannot exceed 100 characters.")]
        public string ReferenceNumber { get; set; }

        // New column for Type of Service
        [Required]
        [StringLength(100, ErrorMessage = "Service type cannot exceed 100 characters.")]
        public string ServiceType { get; set; }

        [Required]
        public int ServiceRequestId { get; set; } // Link to the ServiceRequest

        // Navigation property to link with the BurialApplication entity
        [ForeignKey("ApplicationId")]
        public virtual BurialApplication BurialApplication { get; set; }
    }
}
