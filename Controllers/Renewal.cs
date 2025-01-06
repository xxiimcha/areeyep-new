using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AreEyeP.Data;
using AreEyeP.Helpers;
using AreEyeP.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AreEyeP.Controllers
{
    public class RenewalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RenewalController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpPost]
        public async Task<IActionResult> ProcessRenewalNotifications()
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                var renewalApplications = await _context.BurialApplications
                    .Where(b => b.DateOfRenewal.HasValue && b.DateOfRenewal.Value.Date >= today)
                    .ToListAsync();

                foreach (var application in renewalApplications)
                {
                    var renewalDate = application.DateOfRenewal.Value.Date;
                    var daysUntilRenewal = (renewalDate - today).Days;

                    if (daysUntilRenewal == 30 || daysUntilRenewal == 14 || daysUntilRenewal == 7 || daysUntilRenewal <= 1)
                    {
                        string subject = "Reminder: Your Renewal is Due";
                        string body = GenerateRenewalEmailBody(application, daysUntilRenewal);

                        // Send email to the client
                        var userEmail = GetUserEmail(application.UserId);
                        if (!string.IsNullOrEmpty(userEmail))
                        {
                            EmailHelper.SendEmail(userEmail, subject, body);
                        }

                        // Update application status to "For Renewal"
                        application.Status = "For Renewal";

                        // Add a notification for the client
                        AddNotification(
                            $"Your application for {application.DeceasedFirstName} {application.DeceasedLastName} is due for renewal on {renewalDate:MMMM dd, yyyy}.",
                            "Renewals",
                            "client",
                            application.UserId
                        );

                        // Fetch the existing payment record with the same ApplicationId
                        var existingPayment = await _context.ClientPayments
                            .FirstOrDefaultAsync(p => p.ApplicationId == application.Id);

                        if (existingPayment != null)
                        {
                            // Create a new payment record for renewal
                            var renewalPayment = new ClientPayment
                            {
                                UserId = existingPayment.UserId,
                                ApplicationId = existingPayment.ApplicationId,
                                Amount = existingPayment.Amount, // Copy the amount from the existing payment
                                PaymentMethod = "Unspecified",  // Update if required
                                Status = "Pending",            // Set as pending for the renewal
                                PaymentDate = DateTime.UtcNow, // Set current date
                                ReferenceNumber = GenerateReferenceNumber(), // Generate a new reference number
                                ServiceType = "Renewal"        // Set service type as renewal
                            };

                            _context.ClientPayments.Add(renewalPayment);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Renewal notifications processed successfully.", applications = renewalApplications });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GenerateReferenceNumber()
        {
            return Guid.NewGuid().ToString("N").ToUpper(); // Generate a unique reference number
        }

        private string GenerateRenewalEmailBody(BurialApplication application, int daysUntilRenewal)
        {
            string dueDateMessage = daysUntilRenewal > 1
                ? $"Your renewal is due in {daysUntilRenewal} days, on {application.DateOfRenewal.Value:MMMM dd, yyyy}."
                : "Your renewal is due tomorrow.";

            return $@"
            <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 10px; padding: 20px;'>
                        <div style='text-align: center;'>
                            <img src='https://via.placeholder.com/150' alt='AreEyeP Logo' style='max-width: 150px; margin-bottom: 20px;' />
                        </div>
                        <h1 style='color: #2c3e50; text-align: center;'>Renewal Reminder</h1>
                        <p style='font-size: 16px;'>Dear {application.FirstName} {application.LastName},</p>
                        <p style='font-size: 16px;'>{dueDateMessage}</p>
                        <p style='font-size: 16px;'>Please renew your application before the due date to avoid service interruptions.</p>
                        <p style='font-size: 16px;'>If you have questions, contact us at <a href='mailto:support@areeyep.com' style='color: #3498db;'>support@areeyep.com</a>.</p>
                        <hr style='border-top: 1px solid #ddd;' />
                        <p style='font-size: 14px; text-align: center;'>Best regards,<br /><strong>The AreEyeP Team</strong></p>
                        <p style='font-size: 12px; text-align: center; color: #999;'>This is an automated email. Please do not reply.</p>
                    </div>
                </body>
            </html>";
        }

        private void AddNotification(string message, string type, string targetRole, int? userId = null)
        {
            var notification = new Notification
            {
                Message = message,
                Type = type,
                TargetUser = targetRole,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                NotificationType = "info"
            };

            _context.Notifications.Add(notification);
        }

        private string GetUserEmail(int? userId)
        {
            if (!userId.HasValue)
                return null;

            var user = _context.Users.FirstOrDefault(u => u.Id == userId.Value);
            return user?.Email;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendRenewalNotifications()
        {
            try
            {
                // Manually trigger renewal notifications and retrieve renewal applications
                var today = DateTime.UtcNow.Date;

                var renewalApplications = await _context.BurialApplications
                    .Where(b => b.DateOfRenewal.HasValue && b.DateOfRenewal.Value.Date >= today)
                    .ToListAsync();

                await ProcessRenewalNotifications();

                return Json(new { success = true, message = "Renewal notifications sent successfully.", applications = renewalApplications });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
