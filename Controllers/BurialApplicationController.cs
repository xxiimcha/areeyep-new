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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] BurialApplication model, [FromForm] List<IFormFile>? AttachmentPath)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Log debug information
                    Console.WriteLine("Model received:");
                    Console.WriteLine($"ApplicationId: {model.ApplicationId}");

                    if (AttachmentPath == null || !AttachmentPath.Any())
                    {
                        Console.WriteLine("No files uploaded.");
                        return Json(new { success = false, message = "No files were uploaded." });
                    }

                    // Define the documents directory inside uploads
                    var uploadDirectory = Path.Combine("wwwroot", "uploads", "documents");

                    // Ensure the directory exists
                    if (!Directory.Exists(uploadDirectory))
                    {
                        Console.WriteLine("Creating directory: " + uploadDirectory);
                        Directory.CreateDirectory(uploadDirectory);
                    }

                    var savedFilePaths = new List<string>();

                    foreach (var file in AttachmentPath)
                    {
                        if (file.Length > 0)
                        {
                            // Generate a unique filename to avoid conflicts
                            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                            var filePath = Path.Combine(uploadDirectory, uniqueFileName);

                            Console.WriteLine($"Saving file: {filePath}");

                            // Save the file
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            // Store the relative file path to save in the database
                            savedFilePaths.Add($"/uploads/documents/{uniqueFileName}");
                        }
                    }

                    // Save the file paths as a comma-separated string in the AttachmentPath column
                    model.AttachmentPath = string.Join(",", savedFilePaths);

                    Console.WriteLine("Files uploaded: " + model.AttachmentPath);

                    model.CreatedDate = DateTime.UtcNow;
                    model.Status = "Pending";
                    model.UserId = HttpContext.Session.GetInt32("UserId") ?? 0;

                    _context.BurialApplications.Add(model);
                    await _context.SaveChangesAsync();

                    // Insert a notification for the LGU
                    var notification = new Notification
                    {
                        Message = "A new burial application is waiting for approval.",
                        TargetUser = "LGU",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        NotificationType = "info",
                        Type = "Application"
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();

                    // Fetch the user's email address
                    var user = await _context.Users.FindAsync(model.UserId);
                    if (user != null)
                    {
                        try
                        {
                            // Generate email content
                            string subject = "Burial Application Submitted Successfully";
                            string body = GenerateSubmissionEmailBody(model);

                            // Send email to the user
                            AreEyeP.Helpers.EmailHelper.SendEmail(user.Email, subject, body);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error sending email: {ex.Message}");
                        }
                    }

                    return Json(new { success = true, message = $"Application submitted successfully with ID: {model.ApplicationId}" });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                    return Json(new { success = false, message = $"An exception occurred: {ex.Message}" });
                }
            }

            // Capture and return all validation errors
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            Console.WriteLine("Validation errors: " + string.Join(", ", validationErrors));

            return Json(new { success = false, message = "Validation failed.", errors = validationErrors });
        }

        private string GenerateSubmissionEmailBody(BurialApplication model)
        {
            // Since DateOfBurial is not nullable, we directly use it
            string dateOfBurial = model.DateOfBurial.ToString("MMMM dd, yyyy");

            return $@"
        <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 10px; padding: 20px;'>
                    <h1 style='color: #2c3e50; text-align: center;'>Application Submitted Successfully</h1>
                    <p style='font-size: 16px;'>Dear {model.FirstName} {model.LastName},</p>
                    <p style='font-size: 16px;'>Your burial application has been successfully submitted.</p>
                    <p style='font-size: 16px;'>
                        Application ID: <strong>{model.ApplicationId}</strong><br />
                        Deceased Name: <strong>{model.DeceasedFirstName} {model.DeceasedLastName}</strong><br />
                        Date of Burial: <strong>{dateOfBurial}</strong>
                    </p>
                    <p style='font-size: 16px;'>We will review your application and notify you about the status soon.</p>
                    <p style='font-size: 16px;'>If you have any questions, feel free to contact us.</p>
                    <hr style='border-top: 1px solid #ddd;' />
                    <p style='font-size: 14px; text-align: center;'>Best regards,<br /><strong>The AreEyeP Team</strong></p>
                    <p style='font-size: 12px; text-align: center; color: #999;'>This is an automated email. Please do not reply.</p>
                </div>
            </body>
        </html>";
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

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var application = await _context.BurialApplications.FindAsync(id);
            if (application == null)
            {
                return Json(new { success = false, message = "Application not found." });
            }

            application.Status = "Canceled";
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Application canceled successfully." });
        }

    }
}
