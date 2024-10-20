using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data; // Make sure this namespace points to your DbContext
using AreEyeP.Models; // Update this with the correct namespace for your models
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;

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

        // GET: Payment/AddPayment
        public IActionResult AddPayment()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(Payment payment, IFormFile QrCode)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Handle file upload for QR code
                    if (QrCode != null && QrCode.Length > 0)
                    {
                        var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        if (!Directory.Exists(uploadsDirectory))
                        {
                            Directory.CreateDirectory(uploadsDirectory);
                        }

                        var filePath = Path.Combine(uploadsDirectory, QrCode.FileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await QrCode.CopyToAsync(stream);
                        }

                        // Save the relative path of the QR code image
                        payment.QrCodePath = "/uploads/" + QrCode.FileName;
                    }

                    // Save the payment record to the database
                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();

                    // Return a JSON response indicating success
                    return Json(new { success = true, message = "Payment details uploaded successfully!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            // If validation fails, return a JSON response with errors
            return Json(new { success = false, message = "Invalid payment details provided." });
        }

        // GET: Payment/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }
            return View(payment);
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

            return RedirectToAction(nameof(Index));
        }
    }
}
