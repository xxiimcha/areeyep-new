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

        // GET: /Client/ServiceRequest
        [HttpGet("/Client/ServiceRequest/Create")]
        public async Task<IActionResult> Create()
        {
            // Fetch the current user's ID from the session
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch the services from the database
            var services = await _context.Services.ToListAsync();

            // Pass the services to the view
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
    }
}
