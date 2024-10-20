using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data; // Ensure this points to your DbContext
using AreEyeP.Models; // Update this with the correct namespace for your models
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace AreEyeP.Controllers
{
    public class ServiceRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: /ServiceRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequest serviceRequest)
        {
            if (ModelState.IsValid)
            {
                // Add the new service request
                _context.ServiceRequests.Add(serviceRequest);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Service request created successfully" });
            }

            return Json(new { success = false, message = "Invalid data" });
        }
    }
}
