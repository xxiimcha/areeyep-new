using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace AreEyeP.Controllers
{
    public class NotificationsController : Controller
    {
        // GET: Notifications
        [HttpGet]
        public IActionResult Index()
        {
            // Static list of notifications as an example
            var notifications = new List<string>
            {
                "Payment deadline approaching for burial plot maintenance.",
                "New burial plot available in Section C.",
                "Cemetery maintenance scheduled from Nov 18-20, 2024.",
                "Reminder: Update your documents by the end of the year.",
                "Office closure on Nov 11 for Veterans Day.",
                "Policy update: Check new guidelines on decorations."
            };

            // Pass the notifications to the view
            return View("/Views/Shared/Notifications.cshtml", notifications);
        }
    }
}
