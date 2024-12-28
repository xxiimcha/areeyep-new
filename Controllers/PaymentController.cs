using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data;
using AreEyeP.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace AreEyeP.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment([FromForm] Payment payment, [FromForm] IFormFile QrCode)
        {
            try
            {
                // Disable validation for QrCodePath
                ModelState.Remove("QrCodePath");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage).ToList();
                    Console.WriteLine("Validation failed with errors: " + string.Join(", ", errors));
                    return BadRequest(new { success = false, message = "Validation failed.", errors });
                }

                // Define upload directory
                var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "payment-methods");

                // Ensure the directory exists
                if (!Directory.Exists(uploadDirectory))
                {
                    Console.WriteLine("Creating upload directory: " + uploadDirectory);
                    Directory.CreateDirectory(uploadDirectory);
                }

                // Handle QR Code file upload if provided
                if (QrCode != null && QrCode.Length > 0)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(QrCode.FileName)}";
                    var filePath = Path.Combine(uploadDirectory, uniqueFileName);

                    Console.WriteLine("Saving QR Code file to: " + filePath);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await QrCode.CopyToAsync(stream);
                    }

                    if (!System.IO.File.Exists(filePath))
                    {
                        Console.WriteLine("File was not saved properly: " + filePath);
                        return BadRequest(new { success = false, message = "File upload failed." });
                    }

                    payment.QrCodePath = $"/uploads/payment-methods/{uniqueFileName}";
                }
                else
                {
                    payment.QrCodePath = null; // Optional if no file is uploaded
                }

                // Save payment record
                Console.WriteLine("Saving payment details to database...");
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                Console.WriteLine("Payment details saved successfully with ID: " + payment.Id);

                return Json(new { success = true, message = "Payment details uploaded successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during payment upload: " + ex.Message);
                return BadRequest(new { success = false, message = "An error occurred while processing the payment.", error = ex.Message });
            }
        }

        // POST: Payment/Delete
        [HttpPost]
        public async Task<IActionResult> DeletePayment(int id)
        {
            try
            {
                Console.WriteLine("Fetching payment record with ID: " + id);
                var payment = await _context.Payments.FindAsync(id);
                if (payment == null)
                {
                    Console.WriteLine("Payment not found with ID: " + id);
                    return NotFound(new { success = false, message = "Payment not found." });
                }

                // Remove associated QR Code file if exists
                if (!string.IsNullOrEmpty(payment.QrCodePath))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", payment.QrCodePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        Console.WriteLine("Deleted QR Code file: " + filePath);
                    }
                }

                Console.WriteLine("Removing payment record from database...");
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();

                Console.WriteLine("Payment record deleted successfully.");
                return Json(new { success = true, message = "Payment deleted successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred during DeletePayment: " + ex.Message);
                return BadRequest(new { success = false, message = "An error occurred while deleting the payment.", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            try
            {
                Console.WriteLine("Fetching payment details for ID: " + id);
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == id);
                if (payment == null)
                {
                    Console.WriteLine("Payment not found with ID: " + id);
                    return Json(new { success = false, message = "Payment not found." });
                }

                // Return payment details
                return Json(new
                {
                    success = true,
                    application = new
                    {
                        paymentMode = payment.PaymentMode,
                        bankName = payment.BankName,
                        accountNumber = payment.AccountNumber,
                        qrCodePath = payment.QrCodePath
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching payment details: " + ex.Message);
                return Json(new { success = false, message = "An error occurred while fetching payment details.", error = ex.Message });
            }
        }


    }
}
