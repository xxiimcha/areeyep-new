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
