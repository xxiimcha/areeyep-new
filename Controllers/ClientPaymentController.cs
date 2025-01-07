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

                // Retrieve the associated burial application using ReferenceNumber
                var payment = await _context.ClientPayments.FirstOrDefaultAsync(p => p.ReferenceNumber == model.ReferenceNumber);
                if (payment == null)
                {
                    return Json(new { success = false, message = "Payment not found." });
                }

                var burialApplication = await _context.BurialApplications.FirstOrDefaultAsync(b => b.Id == payment.ApplicationId);
                if (burialApplication == null)
                {
                    return Json(new { success = false, message = "Associated burial application not found." });
                }

                // Handle proof of payment upload
                if (paymentProof != null && paymentProof.Length > 0)
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
                else
                {
                    return Json(new { success = false, message = "Proof of payment is required." });
                }

                // Update payment and application statuses
                if (burialApplication.Status == "Pending Payment")
                {
                    payment.Status = "For Review";
                    payment.PaymentMethod = model.PaymentMethod ?? "Unspecified";
                    payment.PaymentDate = DateTime.UtcNow;

                    burialApplication.Status = "Payment for Approval";
                }

                // Create a notification for LGU
                var notification = new Notification
                {
                    Message = $"A new e-payment for application {burialApplication.ApplicationId} has been processed for review.",
                    TargetUser = "lgu",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    NotificationType = "info",
                    Type = "Payment"
                };

                _context.Notifications.Add(notification);

                // Save changes to the database
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

                // Retrieve the burial application by Id
                var burialApplication = await _context.BurialApplications
                    .FirstOrDefaultAsync(b => b.Id == burialId);

                if (burialApplication == null)
                {
                    return Json(new { success = false, message = "Burial application not found." });
                }

                // Retrieve client payment based on status and service type
                ClientPayment clientPayment = null;

                if (burialApplication.Status == "Pending Payment")
                {
                    clientPayment = await _context.ClientPayments
                        .FirstOrDefaultAsync(p => p.ApplicationId == burialApplication.Id && p.ServiceType == "Burial" && p.Status == "Pending");
                }
                else if (burialApplication.Status == "For Renewal")
                {
                    clientPayment = await _context.ClientPayments
                        .FirstOrDefaultAsync(p => p.ApplicationId == burialApplication.Id && p.ServiceType == "Renewal" && p.Status == "Pending");
                }

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
                clientPayment.PaymentDate = DateTime.UtcNow;

                // Update the BurialApplication status
                burialApplication.Status = "Payment for Approval";

                // Create a notification for LGU
                var notification = new Notification
                {
                    Message = $"A new payment receipt for application {burialApplication.ApplicationId} has been uploaded for review.",
                    TargetUser = "lgu",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    NotificationType = "info",
                    Type = "Payment"
                };

                _context.Notifications.Add(notification);

                // Save changes
                _context.ClientPayments.Update(clientPayment);
                _context.BurialApplications.Update(burialApplication);
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

        [HttpPost]
        public async Task<IActionResult> ConfirmRenewalPayment([FromBody] RenewalPaymentRequest request)
        {
            if (request == null || request.ApplicationId <= 0)
            {
                return Json(new { success = false, message = "Invalid application ID." });
            }

            try
            {
                var application = await _context.BurialApplications
                    .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && a.Status == "For Renewal");

                if (application == null)
                {
                    return Json(new { success = false, message = "Application not found or is not marked as 'For Renewal'." });
                }

                // Update application status to "Renewed"
                application.Status = "Renewed";
                application.DateOfRenewal = DateTime.UtcNow.AddYears(application.Terms ?? 1); // Extend the renewal date
                application.ForRenewal = false;

                // Update the payment status if applicable
                var payment = await _context.ClientPayments
                    .FirstOrDefaultAsync(p => p.ApplicationId == application.Id && p.ServiceType == "Renewal" && p.Status == "Pending");

                if (payment != null)
                {
                    payment.Status = "Completed";
                    payment.PaymentDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Renewal payment confirmed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while confirming renewal payment: " + ex.Message });
            }
        }

        public class RenewalPaymentRequest
        {
            public int ApplicationId { get; set; }
        }

    }
}
