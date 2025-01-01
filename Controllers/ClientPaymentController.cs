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

        [HttpPost]
        public async Task<IActionResult> ProcessEPayment([FromForm] IFormFile paymentProof, [FromForm] EPaymentModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid input data." });
                }

                // Validate ReferenceNumber
                if (string.IsNullOrWhiteSpace(model.ReferenceNumber))
                {
                    return Json(new { success = false, message = "Reference Number is required." });
                }

                // Retrieve payment record using ReferenceNumber
                var payment = await _context.ClientPayments.FirstOrDefaultAsync(p => p.ReferenceNumber == model.ReferenceNumber);
                if (payment == null)
                {
                    return Json(new { success = false, message = "Payment not found." });
                }

                // Retrieve the associated burial application using the ApplicationId
                var burialApplication = await _context.BurialApplications.FirstOrDefaultAsync(b => b.Id == payment.ApplicationId);
                if (burialApplication == null)
                {
                    return Json(new { success = false, message = "Associated burial application not found." });
                }

                // Handle proof of payment upload
                if (paymentProof != null && paymentProof.Length > 0)
                {
                    try
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/proofs");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(paymentProof.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await paymentProof.CopyToAsync(fileStream);
                        }

                        payment.PaymentProof = "/uploads/proofs/" + uniqueFileName; // Save path in the database
                    }
                    catch (Exception fileEx)
                    {
                        return Json(new { success = false, message = "Failed to upload proof of payment.", error = fileEx.Message });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Proof of payment is required." });
                }

                // Update payment record
                payment.Status = "For Review";
                payment.PaymentMethod = model.PaymentMethod ?? "Unspecified";
                payment.PaymentDate = DateTime.Now;

                // Update the BurialApplications status
                burialApplication.Status = "Payment for Approval";

                // Save changes to both tables
                _context.ClientPayments.Update(payment);
                _context.BurialApplications.Update(burialApplication);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Payment processed and application status updated successfully." });
            }
            catch (DbUpdateException dbEx)
            {
                return Json(new { success = false, message = "Database update failed.", error = dbEx.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An unexpected error occurred.", error = ex.Message });
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
            public string ReferenceNumber { get; set; } // Used instead of PaymentId
            public string PaymentMethod { get; set; }
            public IFormFile PaymentProof { get; set; }
        }
    }
}
