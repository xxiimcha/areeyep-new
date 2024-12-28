using System.ComponentModel.DataAnnotations;

namespace AreEyeP.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Payment mode is required.")]
        public string PaymentMode { get; set; } // "ewallet" or "bank"

        public string QrCodePath { get; set; } // For EWallet QR code

        [Required(ErrorMessage = "Bank or eWallet provider name is required.")]
        public string BankName { get; set; } // E.g., "GCash" or "PayPal"

        [Required(ErrorMessage = "Account number or eWallet number is required.")]
        [RegularExpression(@"^\d{10,16}$", ErrorMessage = "Account number must be between 10 to 16 digits.")]
        public string AccountNumber { get; set; }
    }
}
