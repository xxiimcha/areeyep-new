using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data;
using AreEyeP.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.IO;

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

        [HttpPost]
        public async Task<IActionResult> UploadReceipt([FromForm] IFormFile uploadReceipt, [FromForm] int burialId)
        {
            try
            {
                // Validate receipt file
                if (uploadReceipt == null || uploadReceipt.Length == 0)
                {
                    return Json(new { success = false, message = "No receipt file uploaded." });
                }

                Console.WriteLine($"Received BurialApplication ID: {burialId}");

                // Retrieve the burial application by Id
                var burialApplication = await _context.BurialApplications
                    .FirstOrDefaultAsync(b => b.Id == burialId);

                if (burialApplication == null)
                {
                    return Json(new { success = false, message = "Burial application not found." });
                }

                // Retrieve the associated client payment using ApplicationId from BurialApplication
                var clientPayment = await _context.ClientPayments
                    .FirstOrDefaultAsync(p => p.ApplicationId == burialApplication.Id);

                if (clientPayment == null)
                {
                    return Json(new { success = false, message = "Client payment record not found." });
                }

                // Save the uploaded receipt to a directory
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/receipts");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(uploadReceipt.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadReceipt.CopyToAsync(fileStream);
                }

                // Update the ClientPayment record
                clientPayment.PaymentProof = "/uploads/receipts/" + uniqueFileName;
                clientPayment.PaymentMethod = "Walk-in";
                clientPayment.Status = "For Review";

                // Update the BurialApplication status
                burialApplication.Status = "Payment for Approval";

                // Save changes
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Receipt uploaded successfully and statuses updated." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error uploading receipt.", error = ex.Message });
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
