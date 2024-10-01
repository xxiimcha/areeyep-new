using System.ComponentModel.DataAnnotations;

namespace AreEyeP.Models
{
    public class ClientPayment
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public DateTime PaymentDate { get; set; }
        public string ReferenceNumber { get; set; }
    }
}
