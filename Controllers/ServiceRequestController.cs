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
    string paymentOption,
    decimal? amountToBePaid)
        {
            try
            {
                // Fetch the service request and handle null values
                var serviceRequest = await _context.ServiceRequests
                    .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

                if (serviceRequest == null)
                {
                    return Json(new { success = false, message = "Service request not found." });
                }

                // Handle nullable fields
                serviceRequest.Staff = string.IsNullOrWhiteSpace(staffName) ? "Unassigned" : staffName;
                serviceRequest.StaffContact = string.IsNullOrWhiteSpace(staffContact) ? "N/A" : staffContact;
                serviceRequest.ServiceType ??= "Unknown Service Type";

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

                    var payment = new ClientPayment
                    {
                        UserId = serviceRequest.UserId,
                        ApplicationId = serviceRequest.Id,
                        ServiceRequestId = serviceRequest.Id,
                        Amount = amountToBePaid.Value,
                        PaymentMethod = paymentOption == "downPayment" ? "Down Payment" : "Full Payment",
                        Status = "Pending",
                        PaymentDate = DateTime.UtcNow,
                        ServiceType = serviceRequest.ServiceType,
                        ReferenceNumber = Guid.NewGuid().ToString() // Generate unique reference number
                    };

                    _context.ClientPayments.Add(payment);
                }

                // Add notifications for client
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
                // Log the exception for debugging
                Console.WriteLine($"An error occurred: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while processing the request.", error = ex.Message });
            }
        }

    }
}
