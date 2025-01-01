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
            var payment = await _context.ClientPayments
                .Include(p => p.BurialApplication) // Include related BurialApplication details
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                return Json(new { success = false, message = "Payment not found." });
            }

            return Json(new
            {
                success = true,
                payment = new
                {
                    payment.Id,
                    payment.UserId,
                    payment.ApplicationId,
                    payment.Amount,
                    payment.PaymentMethod,
                    payment.Status,
                    PaymentDate = payment.PaymentDate.ToString("yyyy-MM-dd"),
                    payment.ReferenceNumber,
                    payment.ServiceType,
                    payment.ServiceRequestId,
                    payment.PaymentProof,
                    BurialApplication = new
                    {
                        payment.BurialApplication?.DeceasedFirstName,
                        payment.BurialApplication?.DeceasedLastName,
                        payment.BurialApplication?.RelationshipToDeceased,
                        payment.BurialApplication?.DateOfBurial,
                        payment.BurialApplication?.StartTime,
                        payment.BurialApplication?.EndTime
                    }
                }
            });
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
                    var payment = await _context.ClientPayments.FirstOrDefaultAsync(p => p.Id == model.PaymentId);
                    if (payment == null)
                    {
                        return Json(new { success = false, message = "Payment not found." });
                    }

                    // Update payment record
                    payment.Status = "Paid";
                    payment.PaymentMethod = model.PaymentMethod;
                    payment.ReferenceNumber = model.ReferenceNumber;
                    payment.PaymentDate = DateTime.Now;
                    payment.PaymentProof = model.PaymentProof;

                    _context.ClientPayments.Update(payment);
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

        // EPaymentModel definition
        public class EPaymentModel
        {
            public int PaymentId { get; set; } // ID of the payment record
            public string PaymentMethod { get; set; } // e.g., GCash, PayPal, PayMaya
            public string ReferenceNumber { get; set; } // Reference number for the payment
            public string PaymentProof { get; set; } // Path to the payment proof
        }
    }
}
