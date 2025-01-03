﻿using Microsoft.AspNetCore.Mvc;
using AreEyeP.Models;
using System.Linq;
using System.Threading.Tasks;
using AreEyeP.Data;
using System;
using Microsoft.EntityFrameworkCore;

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

                // Handle status-specific logic
                switch (model.Status)
                {
                    case "Declined":
                        if (!string.IsNullOrEmpty(model.DeclineReason))
                        {
                            application.DeclineReason = model.DeclineReason;

                            // Insert a notification for the client
                            var declineNotification = new Notification
                            {
                                Message = $"Your application for {application.DeceasedFirstName} {application.DeceasedLastName} has been declined. Reason: {model.DeclineReason}",
                                TargetUser = "client",
                                UserId = application.UserId, // Reference the user ID who submitted the application
                                CreatedAt = DateTime.UtcNow,
                                IsRead = false,
                                NotificationType = "error",
                                Type = "Application"
                            };

                            _context.Notifications.Add(declineNotification);
                        }
                        break;

                    case "Approved":
                        // Insert a notification for the client
                        var approvedNotification = new Notification
                        {
                            Message = $"Your application for {application.DeceasedFirstName} {application.DeceasedLastName} has been approved.",
                            TargetUser = "client",
                            UserId = application.UserId, // Reference the user ID who submitted the application
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false,
                            NotificationType = "success",
                            Type = "Application"
                        };

                        _context.Notifications.Add(approvedNotification);
                        break;

                    case "Pending Payment":
                        if (model.Amount.HasValue)
                        {
                            // Add a payment record
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

                            _context.ClientPayments.Add(payment);

                            // Insert a notification for the client
                            var paymentNotification = new Notification
                            {
                                Message = $"Your application for {application.DeceasedFirstName} {application.DeceasedLastName} is now pending payment. Amount due: ₱{model.Amount.Value}.",
                                TargetUser = "client",
                                UserId = application.UserId, // Reference the user ID who submitted the application
                                CreatedAt = DateTime.UtcNow,
                                IsRead = false,
                                NotificationType = "info",
                                Type = "Application"
                            };

                            _context.Notifications.Add(paymentNotification);
                        }
                        break;
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
            public string DeclineReason { get; set; } // Add property for decline reason
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
                                          a.DeclineReason,
                                          Payment = _context.ClientPayments
                                              .Where(p => p.ApplicationId == a.Id)
                                              .OrderByDescending(p => p.PaymentDate)
                                              .Select(p => new
                                              {
                                                  p.Amount,
                                                  p.Status,
                                                  PaymentDate = p.PaymentDate.ToString("yyyy-MM-dd"),
                                                  p.ReferenceNumber,
                                                  PaymentProof = p.PaymentProof // Assuming the payment proof image URL is stored here
                                              })
                                              .FirstOrDefault()
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

                // Fetch application record
                var application = _context.BurialApplications.FirstOrDefault(a => a.Id == applicationId);
                if (application == null)
                {
                    return Json(new { success = false, message = "Application record not found." });
                }

                // Fetch payment record
                var payment = _context.ClientPayments.FirstOrDefault(p => p.ApplicationId == applicationId);
                if (payment == null)
                {
                    return Json(new { success = false, message = "Payment record not found." });
                }

                // Update application and payment statuses
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
                    .Where(c => c.AvailabilityStatus == "Available")
                    .OrderBy(c => Guid.NewGuid())
                    .FirstOrDefault();

                if (availableCatacomb != null)
                {
                    availableCatacomb.AvailabilityStatus = "Occupied";
                    availableCatacomb.DeceasedInformation = deceasedId.ToString();
                    _context.SaveChanges();
                }
                else
                {
                    return Json(new { success = false, message = "No available catacombs to assign." });
                }

                // Insert a notification for the client
                var notification = new Notification
                {
                    Message = $"Your application for {application.DeceasedFirstName} {application.DeceasedLastName} has been approved. Assigned Catacomb: {availableCatacomb?.Id}",
                    TargetUser = "client", // Set target user type as "Client"
                    UserId = application.UserId, // Populate UserId for the specific client
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    NotificationType = "info",
                    Type = "Application"
                };

                _context.Notifications.Add(notification);
                _context.SaveChanges();

                return Json(new { success = true, message = "Application completed, statuses updated, catacomb assigned, and notification sent to the client." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        public class CompleteApplicationRequest
        {
            public int ApplicationId { get; set; }
        }
    }
}
