using Microsoft.AspNetCore.Mvc;
using AreEyeP.Models;
using System.Linq;
using System.Threading.Tasks;
using AreEyeP.Data;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace AreEyeP.Controllers
{
    public class ApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ApplicationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Application/
        public IActionResult Index()
        {
            // Fetch all burial applications from the database
            var applications = _context.BurialApplications.ToList();
            return View(applications);
        }

        [HttpPost]
        public JsonResult UpdateStatus([FromBody] UpdateStatusModel model)
        {
            var application = _context.BurialApplications.Find(model.Id);
            if (application != null)
            {
                // Update the status based on the input from the front-end
                application.Status = model.Status;

                // If terms are provided, set the Terms and calculate DateOfRenewal
                if (model.Terms.HasValue)
                {
                    application.Terms = model.Terms;
                    application.DateOfRenewal = DateTime.Now.AddYears(model.Terms.Value);
                }

                // If the status is set to "Pending Payment" and an amount is provided, add a payment record
                if (model.Status == "Pending Payment" && model.Amount.HasValue)
                {
                    var payment = new ClientPayment
                    {
                        UserId = application.UserId,               // Link to the user
                        ApplicationId = application.Id,            // Link to the application
                        Amount = model.Amount.Value,               // Set the amount
                        PaymentMethod = "Unspecified",             // Set a default payment method or customize as needed
                        Status = "Pending",                        // Initial payment status
                        PaymentDate = DateTime.Now,                // Current date as the payment date
                        ReferenceNumber = GenerateReferenceNumber(), // Generate a reference number if required
                        ServiceType = "Burial"
                    };

                    // Add the payment record to the ClientPayments table
                    _context.ClientPayments.Add(payment);
                }

                _context.SaveChanges();
                return Json(new { success = true, message = "Application status and terms updated successfully." });
            }

            return Json(new { success = false, message = "Application not found." });
        }


        // Model for the status update
        public class UpdateStatusModel
        {
            public int Id { get; set; }
            public string Status { get; set; }
            public decimal? Amount { get; set; }
            public int? Terms { get; set; } // Nullable integer for Terms
        }

        // Helper method to generate a unique reference number for payments
        private string GenerateReferenceNumber()
        {
            return Guid.NewGuid().ToString("N").ToUpper(); // Generate a unique GUID
        }

        // GET: /Application/GetDetails?id=1
        [HttpGet]
        public JsonResult GetDetails(int id)
        {
            var application = _context.BurialApplications
                                      .Where(a => a.Id == id)
                                      .Select(a => new
                                      {
                                          a.DeceasedFirstName,
                                          a.DeceasedLastName,
                                          DateOfDeath = a.DateOfDeath.ToString("yyyy-MM-dd"),
                                          DateOfBurial = a.DateOfBurial.ToString("yyyy-MM-dd"),
                                          a.SpecialInstructions,
                                          a.CauseOfDeath,
                                          a.Religion,
                                          a.Address,
                                          a.Status,
                                          a.AttachmentPath,
                                          Payment = _context.ClientPayments
                                              .Where(p => p.ApplicationId == a.Id)
                                              .OrderByDescending(p => p.PaymentDate) // Get the latest payment
                                              .Select(p => new
                                              {
                                                  p.Amount,
                                                  p.Status,
                                                  PaymentDate = p.PaymentDate.ToString("yyyy-MM-dd"),
                                                  p.ReferenceNumber
                                              })
                                              .FirstOrDefault() // Assuming only one recent payment per application
                                      })
                                      .FirstOrDefault();

            if (application != null)
            {
                return Json(new { success = true, application });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public JsonResult CompleteApplication([FromBody] CompleteApplicationRequest request)
        {
            try
            {
                int applicationId = request.ApplicationId;

                // Log the applicationId for verification
                Console.WriteLine($"Received applicationId: {applicationId} (Type: {applicationId.GetType()})");

                // Direct SQL check to verify record existence
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT COUNT(*) FROM BurialApplications WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("@Id", applicationId);

                    var count = (int)command.ExecuteScalar();
                    Console.WriteLine($"Direct SQL check: Records found with Id = {applicationId}: {count}");

                    if (count == 0)
                    {
                        Console.WriteLine("Application record not found in direct SQL check.");
                        return Json(new { success = false, message = "Application record not found." });
                    }
                }

                // Fetch application without AsNoTracking to allow for status update
                var application = _context.BurialApplications.FirstOrDefault(a => a.Id == applicationId);
                if (application == null)
                {
                    Console.WriteLine("Application record not found in Entity Framework query.");
                    return Json(new { success = false, message = "Application record not found in Entity Framework query." });
                }

                // Attempt to fetch the associated payment record
                var payment = _context.ClientPayments.FirstOrDefault(p => p.ApplicationId == applicationId);
                if (payment == null)
                {
                    Console.WriteLine("Payment record not found.");
                    return Json(new { success = false, message = "Payment record not found." });
                }

                // Update statuses
                application.Status = "Approved";
                payment.Status = "Completed";
                _context.SaveChanges();

                // Insert deceased information into Deceased table
                var deceased = new Deceased
                {
                    FirstName = application.DeceasedFirstName,
                    LastName = application.DeceasedLastName,
                    DateOfBirth = application.DateOfBirth,
                    DateOfDeath = application.DateOfDeath,
                    Address = application.Address,
                    CauseOfDeath = application.CauseOfDeath,
                    Gender = application.Gender,
                    ApplicationId = application.Id
                };

                _context.Deceased.Add(deceased);
                _context.SaveChanges();

                // Retrieve the ID of the newly added deceased record
                int deceasedId = deceased.Id;

                // Assign a random available catacomb to the deceased
                var availableCatacomb = _context.Catacombs
                    .Where(c => c.AvailabilityStatus == "Available") // Check if the catacomb is available by string comparison
                    .OrderBy(c => Guid.NewGuid())  // Randomly select an available catacomb
                    .FirstOrDefault();

                if (availableCatacomb != null)
                {
                    availableCatacomb.AvailabilityStatus = "Occupied"; // Set the status to "Occupied"
                    availableCatacomb.DeceasedInformation = deceasedId.ToString(); // Convert int to string

                    _context.SaveChanges();

                    Console.WriteLine($"Assigned Catacomb ID: {availableCatacomb.Id} to Deceased ID: {deceasedId}");
                }
                else
                {
                    Console.WriteLine("No available catacombs found.");
                    return Json(new { success = false, message = "No available catacombs to assign." });
                }


                // Log successful completion
                Console.WriteLine("Application and payment statuses updated, deceased record inserted, and catacomb assigned successfully.");
                return Json(new { success = true, message = "Application completed, statuses updated, and catacomb assigned successfully." });
            }
            catch (Exception ex)
            {
                // Log any exception that occurs
                Console.WriteLine("Error completing application: " + ex.Message);
                return Json(new { success = false, message = "An error occurred while completing the application." });
            }
        }

        public class CompleteApplicationRequest
        {
            public int ApplicationId { get; set; }
        }
    }
}
