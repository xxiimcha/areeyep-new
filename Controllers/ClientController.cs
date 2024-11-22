using Microsoft.AspNetCore.Mvc;
using AreEyeP.Models;
using AreEyeP.Data;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AreEyeP.Controllers
{
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Client/Dashboard
        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet("/Client/ServiceRequest/Create")]
        public async Task<IActionResult> Create()
        {
            // Fetch the current user's ID from the session
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Log the user ID to the server console
            Console.WriteLine($"User ID: {userId}");

            // Pass the user ID to the view using ViewBag
            ViewBag.UserId = userId;

            // Fetch approved deceased records linked to the logged-in user, along with their CatacombID
            var deceasedList = await _context.Deceased
                .Where(d => _context.BurialApplications
                    .Any(b => b.Id == d.ApplicationId && b.Status == "Approved" && b.UserId == userId))
                .Select(d => new
                {
                    d.Id,
                    d.FirstName,
                    d.LastName,
                    CatacombID = _context.Catacombs
                        .Where(c => c.DeceasedInformation == d.Id.ToString()) // Convert d.Id to string
                        .Select(c => c.CatacombID)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // Log the deceased records for debugging
            Console.WriteLine($"Deceased records found: {deceasedList.Count}");
            deceasedList.ForEach(deceased =>
                Console.WriteLine($"Deceased ID: {deceased.Id}, Name: {deceased.FirstName} {deceased.LastName}, CatacombID: {deceased.CatacombID}")
            );

            // Pass the deceased list to the view
            ViewBag.DeceasedList = deceasedList;

            // Fetch the services from the database
            var services = await _context.Services.ToListAsync();
            Console.WriteLine($"Services count: {services.Count}");

            // Pass the services to the view as the model
            return View("ServiceRequest", services);
        }

        // GET: /Client/BurialApplication
        public IActionResult BurialApplication()
        {
            // Generate the ApplicationId in the desired format
            var applicationId = "APP-" + (_context.BurialApplications.Count() + 1).ToString("D4");

            // Create a new instance of the BurialApplication model
            var model = new BurialApplication
            {
                ApplicationId = applicationId // Set the generated ApplicationId
            };

            return View(model); // Pass the model to the view
        }

        // GET: /Client/PaymentHistory
        public IActionResult PaymentHistory()
        {
            // Assuming the UserId is stored in the session
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch payment records for the current user from ClientPayment table
            var payments = _context.ClientPayments
                                   .Where(p => p.UserId == userId)
                                   .OrderByDescending(p => p.PaymentDate)
                                   .ToList();

            return View(payments);
        }

        // GET: /Client/ManageApplications
        [HttpGet]
        public async Task<IActionResult> ManageApplications()
        {
            // Fetch the current user's ID from the session
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Retrieve all burial applications for the logged-in user
            var applications = await _context.BurialApplications
                                             .Where(app => app.UserId == userId)
                                             .ToListAsync();

            return View(applications);
        }

        // GET: /Client/ViewApplication/{id}
        [HttpGet]
        public async Task<IActionResult> ViewApplication(int id)
        {
            // Retrieve the application by ID
            var application = await _context.BurialApplications.FindAsync(id);

            if (application == null || application.UserId != HttpContext.Session.GetInt32("UserId"))
            {
                return NotFound();
            }

            return View(application);
        }

        [HttpGet("/Client/ServiceRequest/Requests")]
        public async Task<IActionResult> Requests()
        {
            // Fetch the current user's ID from the session
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Retrieve all service requests for the logged-in user and handle potential nulls
            var serviceRequests = await _context.ServiceRequests
                                     .Where(sr => sr.UserId == userId)
                                     .Join(_context.Services,
                                           sr => sr.ServiceType,
                                           s => s.Id.ToString(),
                                           (sr, s) => new ServiceRequest
                                           {
                                               Id = sr.Id,
                                               DeceasedId = sr.DeceasedId ?? "N/A",
                                               ServiceType = s.ServiceName ?? "Unknown", // Map ServiceName to ServiceType
                                               UrgencyLevel = sr.UrgencyLevel ?? "Medium",
                                               DateOfService = sr.DateOfService,
                                               Status = sr.Status ?? "Pending",
                                               Staff = sr.Staff ?? "Unassigned",
                                               PaymentRequired = sr.PaymentRequired
                                           })
                                     .ToListAsync();

            return View("ManageServiceRequests", serviceRequests);

        }


        // GET: /Client/ManageRelatives
        [HttpGet("/Client/ManageRelatives")]
        public async Task<IActionResult> ManageRelatives()
        {
            // Fetch the current user's ID from the session
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch deceased records linked to the logged-in user
            var relatives = await _context.Deceased
                .Where(d => _context.BurialApplications
                    .Any(b => b.Id == d.ApplicationId && b.UserId == userId))
                .Select(d => new
                {
                    d.Id,
                    d.FirstName,
                    d.LastName,
                    d.DateOfBirth,
                    d.DateOfDeath,
                    d.Address,
                    d.CauseOfDeath,
                    d.Gender,
                    d.ApplicationId
                })
                .ToListAsync();

            return View("ManageRelatives", relatives);
        }

        [HttpGet("/Client/GetServiceRequestDetails/{id}")]
        public async Task<IActionResult> GetServiceRequestDetails(int id)
        {
            try
            {
                var serviceRequest = await (from sr in _context.ServiceRequests
                                            join s in _context.Services on sr.ServiceType equals s.Id.ToString()
                                            where sr.Id == id
                                            select new
                                            {
                                                sr.Id,
                                                ServiceType = s.ServiceName,
                                                sr.DateOfService,
                                                sr.UrgencyLevel,
                                                sr.Status,
                                                sr.SpecialInstructions,
                                                Staff = sr.Staff ?? "N/A",
                                                StaffContact = sr.StaffContact ?? "N/A",
                                                PaymentMessage = sr.PaymentRequired ? "Payment is required" : "Pay upon completion"
                                            }).FirstOrDefaultAsync();

                if (serviceRequest == null)
                {
                    return Json(new { success = false, message = "Service request not found." });
                }

                return Json(new { success = true, serviceRequest });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error fetching service request details: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while fetching the details." });
            }
        }


    }
}