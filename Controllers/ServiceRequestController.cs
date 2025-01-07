using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data;
using AreEyeP.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AreEyeP.Controllers
{
    public class ServiceRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("ServiceRequest/Create")]
        public async Task<IActionResult> Create([FromBody] ServiceRequest serviceRequest)
        {
            // Log each field to verify it is being received
            Console.WriteLine($"DeceasedId: {serviceRequest.DeceasedId}");
            Console.WriteLine($"DateOfService: {serviceRequest.DateOfService}");
            Console.WriteLine($"ServiceType: {serviceRequest.ServiceType}");
            Console.WriteLine($"UrgencyLevel: {serviceRequest.UrgencyLevel}");
            Console.WriteLine($"SpecialInstructions: {serviceRequest.SpecialInstructions}");
            Console.WriteLine($"UserId: {serviceRequest.UserId}");
            Console.WriteLine($"PaymentRequired: {serviceRequest.PaymentRequired}");

            // Remove StaffContact from model state validation
            ModelState.Remove(nameof(serviceRequest.StaffContact));

            if (ModelState.IsValid)
            {
                // Set additional fields if necessary
                serviceRequest.Staff ??= "Unassigned";
                serviceRequest.Status ??= "Pending";
                serviceRequest.StartTime = serviceRequest.StartTime == default ? TimeSpan.FromHours(9) : serviceRequest.StartTime;
                serviceRequest.EndTime = serviceRequest.EndTime == default ? TimeSpan.FromHours(17) : serviceRequest.EndTime;

                // Convert PaymentRequired to a boolean value for storage in the database
                serviceRequest.PaymentRequired = serviceRequest.PaymentRequired ? true : false;

                _context.ServiceRequests.Add(serviceRequest);
                await _context.SaveChangesAsync();

                // Notify staff about the new service request
                var staffNotification = new Notification
                {
                    Message = $"A new service request (ID: {serviceRequest.Id}) has been created and is awaiting assignment.",
                    TargetUser = "staff",
                    UserId = null, // No specific user ID for staff notification
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    NotificationType = "info",
                    Type = "Service Request"
                };

                _context.Notifications.Add(staffNotification);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Service request created successfully and staff notified!" });
            }

            // Log ModelState errors for debugging
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            Console.WriteLine("ModelState errors: " + string.Join(", ", errors));

            return Json(new { success = false, message = "Failed to create service request. Invalid data.", errors });
        }

        [HttpPost]
        public async Task<IActionResult> AssignStaffAndSavePayment(
    int serviceRequestId,
    string staffName,
    string staffContact,
    bool isPaymentRequired,
    decimal? amountToBePaid)
        {
            try
            {
                // Fetch the service request
                var serviceRequest = await _context.ServiceRequests
                    .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

                if (serviceRequest == null)
                {
                    return Json(new { success = false, message = "Service request not found." });
                }

                // Fetch the related ApplicationId using the DeceasedId
                var deceased = await _context.Deceased
                    .FirstOrDefaultAsync(d => d.Id == int.Parse(serviceRequest.DeceasedId));

                if (deceased == null || deceased.ApplicationId == 0)
                {
                    return Json(new { success = false, message = "Related deceased or application record not found." });
                }

                int applicationId = deceased.ApplicationId;

                // Handle nullable fields
                serviceRequest.Staff = string.IsNullOrWhiteSpace(staffName) ? "Unassigned" : staffName;
                serviceRequest.StaffContact = string.IsNullOrWhiteSpace(staffContact) ? "N/A" : staffContact;

                // Update service request fields
                serviceRequest.Status = "Assigned";
                serviceRequest.UpdatedAt = DateTime.UtcNow;

                // Handle payment details
                if (isPaymentRequired)
                {
                    if (!amountToBePaid.HasValue || amountToBePaid.Value <= 0)
                    {
                        return Json(new { success = false, message = "Invalid payment amount. Please provide a valid amount." });
                    }

                    serviceRequest.PaymentRequired = true;
                    serviceRequest.Amount = amountToBePaid.Value;

                    var payment = new ClientPayment
                    {
                        UserId = serviceRequest.UserId,
                        ApplicationId = applicationId, // Use the ApplicationId from the Deceased record
                        ServiceRequestId = serviceRequest.Id,
                        Amount = amountToBePaid.Value,
                        PaymentMethod = "Pre-Service Payment", // Fixed payment method
                        Status = "Pending",
                        PaymentDate = DateTime.UtcNow,
                        ServiceType = serviceRequest.ServiceType,
                        ReferenceNumber = Guid.NewGuid().ToString()
                    };

                    _context.ClientPayments.Add(payment);

                    var paymentNotification = new Notification
                    {
                        Message = $"You have a pending payment of ₱{amountToBePaid.Value} for your service request (ID: {serviceRequestId}).",
                        TargetUser = "client",
                        UserId = serviceRequest.UserId,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        NotificationType = "info",
                        Type = "Payment"
                    };

                    _context.Notifications.Add(paymentNotification);
                }
                else
                {
                    // Reset payment fields if payment is not required
                    serviceRequest.PaymentRequired = false;
                    serviceRequest.Amount = 0;
                }

                // Add notification for client
                var assignmentNotification = new Notification
                {
                    Message = $"Staff {serviceRequest.Staff} has been assigned to your service request (ID: {serviceRequestId}).",
                    TargetUser = "client",
                    UserId = serviceRequest.UserId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    NotificationType = "info",
                    Type = "Service Request"
                };

                _context.Notifications.Add(assignmentNotification);

                // Save changes
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Staff assigned and payment details saved successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return Json(new { success = false, message = "An error occurred while processing the request.", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MakePayment(int serviceRequestId, decimal amount, string paymentMethod, IFormFile paymentProof)
        {
            try
            {
                var serviceRequest = await _context.ServiceRequests.FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

                if (serviceRequest == null || !serviceRequest.PaymentRequired)
                {
                    return Json(new { success = false, message = "Invalid service request or payment is not required." });
                }

                // Save the payment proof file
                string proofPath = null;
                if (paymentProof != null)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    proofPath = Path.Combine(uploadsFolder, paymentProof.FileName);
                    using (var stream = new FileStream(proofPath, FileMode.Create))
                    {
                        await paymentProof.CopyToAsync(stream);
                    }
                }

                var payment = new ClientPayment
                {
                    UserId = serviceRequest.UserId,
                    ApplicationId = serviceRequest.Id,
                    ServiceRequestId = serviceRequest.Id,
                    Amount = amount,
                    PaymentMethod = paymentMethod,
                    Status = "Completed",
                    PaymentDate = DateTime.UtcNow,
                    ServiceType = serviceRequest.ServiceType,
                    ReferenceNumber = Guid.NewGuid().ToString(),
                    PaymentProof = $"/uploads/{paymentProof.FileName}"
                };

                _context.ClientPayments.Add(payment);
                serviceRequest.PaymentRequired = false; // Mark as paid
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Payment successful." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing payment: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while processing the payment." });
            }
        }

    }
}
