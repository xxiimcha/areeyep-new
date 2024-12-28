using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AreEyeP.Data; // Adjust namespace based on your project structure
using AreEyeP.Helpers; // Include the EmailHelper namespace
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
                // Current date for comparison
                var today = DateTime.UtcNow.Date;

                // Get applications with a valid DateOfRenewal
                var renewalApplications = await _context.BurialApplications
                    .Where(b => b.DateOfRenewal.HasValue && b.DateOfRenewal.Value.Date >= today)
                    .ToListAsync();

                foreach (var application in renewalApplications)
                {
                    var renewalDate = application.DateOfRenewal.Value.Date; // Use only the date portion
                    var daysUntilRenewal = (renewalDate - today).Days;

                    // Check if a notification needs to be sent
                    if (daysUntilRenewal == 30 || daysUntilRenewal == 14 || daysUntilRenewal == 7 || daysUntilRenewal <= 1)
                    {
                        string clientMessage = daysUntilRenewal > 1
                            ? $"Your renewal is due in {daysUntilRenewal} days. Please renew before {renewalDate:MMMM dd, yyyy}."
                            : "Your renewal is due tomorrow. Please renew to avoid service interruptions.";

                        // Notify the client
                        AddNotification(
                            clientMessage,
                            "Renewals",
                            "client",
                            application.UserId
                        );

                        // Send email notification to the client
                        var userEmail = GetUserEmail(application.UserId);
                        if (!string.IsNullOrEmpty(userEmail))
                        {
                            EmailHelper.SendEmailConfirmation(userEmail, $"{application.FirstName} {application.LastName}");
                        }

                        // Notify LGU and staff (role-based, no UserId needed)
                        if (daysUntilRenewal == 30 || daysUntilRenewal == 14 || daysUntilRenewal == 7)
                        {
                            string roleMessage = $"Renewal for Application {application.ApplicationId} is due on {renewalDate:MMMM dd, yyyy}.";
                            AddNotification(roleMessage, "Renewals", "lgu");
                            AddNotification(roleMessage, "Renewals", "staff");
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Renewal notifications processed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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
            // Fetch user email based on userId from the Users table
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
        public async Task<IActionResult> SendRenewalNotificationsManually()
        {
            try
            {
                // Manually trigger renewal notifications
                await ProcessRenewalNotifications();
                return Json(new { success = true, message = "Renewal notifications sent manually." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
