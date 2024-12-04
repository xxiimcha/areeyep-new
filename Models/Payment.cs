using System.ComponentModel.DataAnnotations;

namespace AreEyeP.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Payment mode is required.")]
        public string PaymentMode { get; set; } // "ewallet" or "bank"

        // QR Code Path for eWallet Payments
        public string QrCodePath { get; set; } // For EWallet QR code

        // For eWallet Provider or Bank Name
        [Required(ErrorMessage = "Bank or eWallet provider name is required.")]
        public string BankName { get; set; } // E.g., "GCash", "PayPal", or "Chase Bank"

        // For Account Number or eWallet Number
        [Required(ErrorMessage = "Account number or eWallet number is required.")]
        [RegularExpression(@"^\d{10,16}$", ErrorMessage = "Account number must be between 10 to 16 digits.")]
        public string AccountNumber { get; set; } // E.g., GCash Number or Bank Account Number

        // Only applicable for Bank Transfers
        public string BankBranch { get; set; } // Optional field for Bank Transfers
    }
}
