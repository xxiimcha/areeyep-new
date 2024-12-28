using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AreEyeP.Data; // Adjust based on your project structure
using System.Linq;
using System.Threading.Tasks;
using AreEyeP.Models;

namespace AreEyeP.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Notifications
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Fetch logged-in user's ID and role
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole"); // Assuming role is stored in the session

            if (userId == null || string.IsNullOrEmpty(userRole))
            {
                return Unauthorized("User is not logged in.");
            }

            // Fetch notifications based on the role
            IQueryable<Notification> notificationsQuery;

            if (userRole.ToLower() == "client")
            {
                // Select notifications targeted to the logged-in client
                notificationsQuery = _context.Notifications
                    .Where(n => n.UserId == userId || n.TargetUser == "client")
                    .OrderByDescending(n => n.CreatedAt);
            }
            else
            {
                // Select notifications targeted for the logged-in user's role
                notificationsQuery = _context.Notifications
                    .Where(n => n.TargetUser == userRole.ToLower() || n.TargetUser == null || n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt);
            }

            // Execute the query and fetch results
            var notifications = await notificationsQuery.ToListAsync();

            // Categorize notifications into Application, ServiceRequests, Payments, and Renewals
            var categorizedNotifications = new Dictionary<string, IEnumerable<Notification>>
            {
                { "Application", notifications.Where(n => n.Type == "Application") },
                { "ServiceRequests", notifications.Where(n => n.Type == "Service Request") },
                { "Payments", notifications.Where(n => n.Type == "Payments") },
                { "Renewals", notifications.Where(n => n.Type == "Renewals") }
            };

            // Pass the categorized notifications to the view
            return View("~/Views/Shared/Notifications.cshtml", categorizedNotifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Notification not found." });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null || string.IsNullOrEmpty(userRole))
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            // Fetch unread notifications for the user
            var notifications = _context.Notifications.Where(n =>
                (n.UserId == userId || n.TargetUser == userRole.ToLower()) && !n.IsRead);

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ClearAll()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null || string.IsNullOrEmpty(userRole))
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            // Fetch all notifications for the user or role
            var notifications = _context.Notifications.Where(n =>
                n.UserId == userId || n.TargetUser == userRole.ToLower());

            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
