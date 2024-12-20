using AreEyeP.Data;
using AreEyeP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace AreEyeP.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Services
        [HttpGet]
        public async Task<IActionResult> Services()
        {
            var services = await _context.Services.ToListAsync();
            // Specify the path to the view in the Shared folder
            return View("/Views/Shared/Services.cshtml", services);
        }

        // GET: /Application
        public IActionResult Application()
        {
            var applications = _context.BurialApplications.ToList();
            var userRole = HttpContext.Session.GetString("UserRole"); // Assuming the role is stored in session
            ViewBag.UserRole = userRole;
            return View("~/Views/Shared/Application.cshtml", applications);
        }


        // GET: Admin/Dashboard
        public IActionResult Dashboard()
        {
            // Fetch the total number of users
            var totalUsers = _context.Users.Count();

            // Fetch the total number of client accounts
            var clientAccounts = _context.Users.Count(u => u.Role.ToLower() == "client");

            // Pass the data to the view using ViewBag
            ViewBag.TotalUsers = totalUsers;
            ViewBag.ClientAccounts = clientAccounts;

            return View();
        }


        // GET: Admin/User
        public async Task<IActionResult> User()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // GET: /Admin/Catacombs
        public IActionResult Catacombs()
        {
            var catacombs = _context.Catacombs.ToList();

            // Generate the next Catacomb ID
            string generatedCatacombID = GenerateCatacombID();
            ViewBag.GeneratedCatacombID = generatedCatacombID;

            return View(catacombs);
        }

        // Method to generate the CatacombID
        private string GenerateCatacombID()
        {
            var random = new Random();
            string catacombID;

            do
            {
                int randomNumber = random.Next(1, 10000); // Generate a random number
                catacombID = $"AEP{randomNumber:D4}"; // Format as AEPxxxx
            }
            while (_context.Catacombs.Any(c => c.CatacombID == catacombID)); // Ensure uniqueness

            return catacombID;
        }

        // POST: Admin/AddUser
        [HttpPost]
        public async Task<IActionResult> AddUser(User user)
        {
            if (ModelState.IsValid)
            {
                _context.Users.Add(user); // Add the user to the DbSet
                await _context.SaveChangesAsync(); // Save changes to the database
                return Json(new { success = true, message = "User added successfully!" });
            }

            return Json(new { success = false, message = "Failed to add user." });
        }

        // POST: Admin/DeleteUser
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(User));
        }

        // GET: Admin/Payments
        public async Task<IActionResult> Payments()
        {
            var payments = await _context.Payments.ToListAsync();
            return View(payments);
        }

        // POST: Admin/AddPaymentDetails
        [HttpPost]
        public async Task<IActionResult> AddPaymentDetails(Payment payment, IFormFile QrCode)
        {
            if (ModelState.IsValid)
            {
                // Handle file upload for QR code
                if (QrCode != null && QrCode.Length > 0)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", QrCode.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await QrCode.CopyToAsync(stream);
                    }
                    payment.QrCodePath = "/uploads/" + QrCode.FileName; // Save the relative path
                }

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Payments));
            }
            return View("Payments", await _context.Payments.ToListAsync());
        }

        // Define the ServiceRequestViewModel within the AdminController
        public class ServiceRequestViewModel
        {
            public int Id { get; set; }
            public string DeceasedId { get; set; }
            public DateTime DateOfService { get; set; }
            public string ServiceType { get; set; }
            public string UrgencyLevel { get; set; }
            public string Staff { get; set; }
            public string Status { get; set; }
            public decimal? Amount { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        // GET: /Admin/ServiceRequest
        public async Task<IActionResult> ServiceRequest()
        {
            var requests = await _context.ServiceRequests
                .Select(r => new ServiceRequestViewModel
                {
                    Id = r.Id,
                    DeceasedId = r.DeceasedId,
                    DateOfService = r.DateOfService,
                    ServiceType = r.ServiceType,
                    UrgencyLevel = r.UrgencyLevel,
                    Staff = r.Staff,
                    Status = r.Status,
                    Amount = r.Amount,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return View(requests);
        }

        // POST: /Admin/UpdateRequestStatus
        [HttpPost]
        public async Task<IActionResult> UpdateRequestStatus(int id, string status)
        {
            var request = await _context.ServiceRequests.FindAsync(id);
            if (request == null)
            {
                return Json(new { success = false, message = "Request not found." });
            }

            request.Status = status;
            request.UpdatedAt = DateTime.UtcNow;
            _context.ServiceRequests.Update(request);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Status updated successfully." });
        }
    }
}