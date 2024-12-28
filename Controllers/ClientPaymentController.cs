using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data;
using AreEyeP.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace AreEyeP.Controllers
{
    public class ClientPaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientPaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Fetch payment details
        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return Json(new { success = false, message = "Payment not found." });
            }

            return Json(new { success = true, payment });
        }

        // Process e-payment
        [HttpPost]
        public async Task<IActionResult> ProcessEPayment([FromForm] EPaymentModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Retrieve payment record
                    var payment = await _context.Payments.FindAsync(model.PaymentId);
                    if (payment == null)
                    {
                        return Json(new { success = false, message = "Payment not found." });
                    }

                    // Update payment record
                    payment.Status = "Paid";
                    payment.PaymentMethod = model.PaymentMethod;
                    payment.AccountNumber = model.AccountNumber;
                    payment.AccountName = model.AccountName;
                    payment.AmountPaid = model.PaymentAmount;

                    _context.Payments.Update(payment);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Payment processed successfully." });
                }

                return Json(new { success = false, message = "Invalid input data." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error processing payment.", error = ex.Message });
            }
        }
    }
}
