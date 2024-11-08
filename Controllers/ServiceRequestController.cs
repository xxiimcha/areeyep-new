using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data;
using AreEyeP.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;

namespace AreEyeP.Controllers
{
    public class ServiceRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /ServiceRequest/Create
        public async Task<IActionResult> Create()
        {
            // Retrieve the current user ID, with error handling if user is not authenticated
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                Console.WriteLine("User is not authenticated.");
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = int.Parse(userIdClaim);
            Console.WriteLine($"Current User ID: {currentUserId}");

            try
            {
                // Get approved deceased records linked to the logged-in user
                var deceasedList = await _context.Deceased
                    .Where(d => _context.BurialApplications
                        .Any(b => b.Id == d.ApplicationId && b.Status == "Approved" && b.UserId == currentUserId))
                    .ToListAsync();

                Console.WriteLine($"Deceased records found: {deceasedList.Count}");
                deceasedList.ForEach(deceased =>
                    Console.WriteLine($"Deceased ID: {deceased.Id}, Name: {deceased.FirstName} {deceased.LastName}")
                );

                ViewBag.DeceasedList = deceasedList;

                // Retrieve available services
                var services = await _context.Services.ToListAsync();
                Console.WriteLine($"Services count: {services.Count}");
                services.ForEach(service =>
                    Console.WriteLine($"Service ID: {service.Id}, Name: {service.ServiceName}, Price Range: {service.MinPrice} - {service.MaxPrice}")
                );

                return View(services);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while loading data: {ex.Message}");
                return View("Error", new { message = "An error occurred while loading the service request page." });
            }
        }

        
    }
}
