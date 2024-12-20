using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data; // Ensure this namespace points to your DbContext
using AreEyeP.Models; // Update this with the correct namespace for your models
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

        // GET: Payment/Index
        public async Task<IActionResult> Index()
        {
            // Fetch the payments from the database
            var payments = await _context.Payments.ToListAsync();
            return View(payments); // Pass the payments data to the view
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(Payment payment, IFormFile QrCode)
        {
            ILogger<PaymentController> logger = HttpContext.RequestServices.GetRequiredService<ILogger<PaymentController>>();

            try
            {
                logger.LogInformation("AddPayment request received.");

                // Check ModelState
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                                                   .Select(e => e.ErrorMessage)
                                                   .ToList();
                    logger.LogWarning("Validation failed: {Errors}", string.Join(", ", errors));
                    return Json(new { success = false, message = "Validation failed.", errors });
                }

                logger.LogInformation("Model state is valid.");

                // Process the QR Code
                if (QrCode != null && QrCode.Length > 0)
                {
                    logger.LogInformation("Processing QR Code upload.");

                    var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "payment-methods");
                    if (!Directory.Exists(uploadsDirectory))
                    {
                        Directory.CreateDirectory(uploadsDirectory);
                    }

                    var fileName = $"{Path.GetFileNameWithoutExtension(QrCode.FileName)}_{Guid.NewGuid()}{Path.GetExtension(QrCode.FileName)}";
                    var filePath = Path.Combine(uploadsDirectory, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await QrCode.CopyToAsync(stream);
                    }

                    payment.QrCodePath = $"/uploads/payment-methods/{fileName}";
                }

                logger.LogInformation("Saving payment record to the database.");
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                logger.LogInformation("Payment record saved successfully with ID {PaymentId}.", payment.Id);
                return Json(new { success = true, message = "Payment details uploaded successfully!" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred.");
                return Json(new { success = false, message = "An unexpected error occurred.", error = ex.Message });
            }
        }


        // POST: Payment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Payment record deleted successfully!" });
        }
    }
}
