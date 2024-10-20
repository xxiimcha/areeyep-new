using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data; // Ensure you have the correct namespace for your DbContext
using AreEyeP.Models;
using System.Linq;
using System;
using System.Threading.Tasks; // Added for asynchronous support
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AreEyeP.Controllers
{
    public class BurialApplicationController : Controller
    {
        private readonly ApplicationDbContext _context; // Reference to your database context

        public BurialApplicationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /BurialApplication/Index
        public async Task<IActionResult> Index()
        {
            // Ensure the user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch burial applications submitted by the logged-in user
            var applications = await _context.BurialApplications
                                             .Where(a => a.UserId == userId)
                                             .ToListAsync();

            return View(applications);
        }

        // GET: /BurialApplication/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var application = await _context.BurialApplications.FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound("The requested burial application was not found.");
            }

            // Ensure the logged-in user owns the application
            if (application.UserId != HttpContext.Session.GetInt32("UserId"))
            {
                return Unauthorized("You are not authorized to view this application.");
            }

            return View(application);
        }

        // GET: /BurialApplication/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /BurialApplication/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BurialApplication model, IFormFile? Attachment)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (Attachment != null && Attachment.Length > 0)
                    {
                        var filePath = Path.Combine("wwwroot/uploads", Attachment.FileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await Attachment.CopyToAsync(stream);
                        }
                        model.AttachmentPath = filePath;
                    }

                    // Check if ApplicationId was provided from the form field
                    if (string.IsNullOrWhiteSpace(model.ApplicationId))
                    {
                        return Json(new { success = false, message = "Application ID is required." });
                    }

                    model.CreatedDate = DateTime.UtcNow;
                    model.Status = "Pending";
                    model.UserId = HttpContext.Session.GetInt32("UserId") ?? 0;

                    _context.BurialApplications.Add(model);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = $"Application submitted successfully with ID: {model.ApplicationId}" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"An exception occurred: {ex.Message}" });
                }
            }

            // Capture and return all validation errors
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return Json(new { success = false, message = "Validation failed.", errors = validationErrors });
        }

        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            // Fetch the burial application by ID
            var application = await _context.BurialApplications.FindAsync(id);

            if (application == null)
            {
                return Json(new { success = false, message = "Application not found." });
            }

            // Return the application data as JSON
            return Json(new { success = true, application });
        }

        // GET: /BurialApplication/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var application = await _context.BurialApplications.FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound("The application was not found.");
            }

            // Ensure the logged-in user owns the application
            if (application.UserId != HttpContext.Session.GetInt32("UserId"))
            {
                return Unauthorized("You are not authorized to edit this application.");
            }

            return View(application);
        }

        // POST: /BurialApplication/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BurialApplication model)
        {
            if (ModelState.IsValid)
            {
                var existingApplication = await _context.BurialApplications.FirstOrDefaultAsync(a => a.Id == model.Id);

                if (existingApplication != null)
                {
                    // Ensure the logged-in user owns the application
                    if (existingApplication.UserId != HttpContext.Session.GetInt32("UserId"))
                    {
                        return Unauthorized("You are not authorized to edit this application.");
                    }

                    existingApplication.FirstName = model.FirstName;
                    existingApplication.LastName = model.LastName;
                    existingApplication.Address = model.Address;
                    existingApplication.RelationshipToDeceased = model.RelationshipToDeceased;
                    existingApplication.ContactInformation = model.ContactInformation;
                    existingApplication.DeceasedFirstName = model.DeceasedFirstName;
                    existingApplication.DeceasedLastName = model.DeceasedLastName;
                    existingApplication.Gender = model.Gender;
                    existingApplication.DateOfBirth = model.DateOfBirth;
                    existingApplication.DateOfDeath = model.DateOfDeath;
                    existingApplication.CauseOfDeath = model.CauseOfDeath;
                    existingApplication.Religion = model.Religion;
                    existingApplication.AttachmentPath = model.AttachmentPath;
                    existingApplication.DateOfBurial = model.DateOfBurial;
                    existingApplication.StartTime = model.StartTime;
                    existingApplication.EndTime = model.EndTime;
                    existingApplication.SpecialInstructions = model.SpecialInstructions;
                    existingApplication.Status = model.Status;

                    await _context.SaveChangesAsync();

                    return RedirectToAction("Index");
                }

                return NotFound("The application to edit was not found.");
            }

            return View(model);
        }

        // GET: /BurialApplication/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var application = await _context.BurialApplications.FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound("The application to delete was not found.");
            }

            // Ensure the logged-in user owns the application
            if (application.UserId != HttpContext.Session.GetInt32("UserId"))
            {
                return Unauthorized("You are not authorized to delete this application.");
            }

            return View(application);
        }

        // POST: /BurialApplication/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var application = await _context.BurialApplications.FirstOrDefaultAsync(a => a.Id == id);

            if (application != null)
            {
                // Ensure the logged-in user owns the application
                if (application.UserId != HttpContext.Session.GetInt32("UserId"))
                {
                    return Unauthorized("You are not authorized to delete this application.");
                }

                _context.BurialApplications.Remove(application);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return NotFound("The application to delete was not found.");
        }
    }
}
