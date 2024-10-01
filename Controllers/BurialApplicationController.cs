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

                    model.CreatedDate = DateTime.UtcNow;
                    model.Status = "Pending";
                    model.UserId = HttpContext.Session.GetInt32("UserId") ?? 0;

                    _context.BurialApplications.Add(model);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true });
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
        public async Task<IActionResult> TestInsert()
        {
            try
            {
                var testModel = new BurialApplication
                {
                    FirstName = "Test",
                    LastName = "User",
                    Address = "123 Test Street",
                    RelationshipToDeceased = "Friend",
                    ContactInformation = "123-456-7890",
                    DeceasedFirstName = "Deceased",
                    DeceasedLastName = "Person",
                    Gender = "Male",
                    DateOfBirth = new DateTime(1980, 1, 1),
                    DateOfDeath = new DateTime(2024, 9, 30),
                    CauseOfDeath = "Natural Causes",
                    DateOfBurial = new DateTime(2024, 10, 5),
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 0, 0),
                    SpecialInstructions = "Test Instructions",
                    CreatedDate = DateTime.UtcNow,
                    UserId = HttpContext.Session.GetInt32("UserId") ?? 0,
                    Status = "Pending"
                };

                _context.BurialApplications.Add(testModel);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Test insertion successful." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An exception occurred during test insertion: {ex.Message}" });
            }
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
