namespace AreEyeP.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public string PaymentMode { get; set; } // EWallet, BankTransfer
        public string QrCodePath { get; set; } // For EWallet QR code
        public string BankName { get; set; }   // For Bank Transfer
        public string AccountNumber { get; set; } // For Bank Transfer
    }
}
