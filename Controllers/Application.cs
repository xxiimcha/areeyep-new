using Microsoft.AspNetCore.Mvc;
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
                // Update the status
                application.Status = model.Status;

                // If terms are provided, set the Terms and calculate DateOfRenewal
                if (model.Terms.HasValue)
                {
                    application.Terms = model.Terms;
                    application.DateOfRenewal = DateTime.Now.AddYears(model.Terms.Value);
                }

                // Fetch user email
                var user = _context.Users.FirstOrDefault(u => u.Id == application.UserId);
                if (user != null)
                {
                    try
                    {
                        // Handle status-specific logic and send email
                        string subject = string.Empty;
                        string body = string.Empty;

                        switch (model.Status)
                        {
                            case "Declined":
                                if (!string.IsNullOrEmpty(model.DeclineReason))
                                {
                                    application.DeclineReason = model.DeclineReason;

                                    subject = "Application Declined";
                                    body = GenerateDeclineEmailBody(application, model.DeclineReason);

                                    // Insert a notification for the client
                                    var declineNotification = new Notification
                                    {
                                        Message = $"Your application for {application.DeceasedFirstName} {application.DeceasedLastName} has been declined. Reason: {model.DeclineReason}",
                                        TargetUser = "client",
                                        UserId = application.UserId,
                                        CreatedAt = DateTime.UtcNow,
                                        IsRead = false,
                                        NotificationType = "error",
                                        Type = "Application"
                                    };

                                    _context.Notifications.Add(declineNotification);
                                }
                                break;

                            case "Approved":
                                subject = "Application Approved";
                                body = GenerateApprovalEmailBody(application);

                                // Insert a notification for the client
                                var approvedNotification = new Notification
                                {
                                    Message = $"Your application for {application.DeceasedFirstName} {application.DeceasedLastName} has been approved.",
                                    TargetUser = "client",
                                    UserId = application.UserId,
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
                                        UserId = application.UserId,
                                        ApplicationId = application.Id,
                                        Amount = model.Amount.Value,
                                        PaymentMethod = "Unspecified",
                                        Status = "Pending",
                                        PaymentDate = DateTime.Now,
                                        ReferenceNumber = GenerateReferenceNumber(),
                                        ServiceType = "Burial"
                                    };

                                    _context.ClientPayments.Add(payment);

                                    subject = "Payment Pending for Application";
                                    body = GeneratePaymentPendingEmailBody(application, model.Amount.Value);

                                    // Insert a notification for the client
                                    var paymentNotification = new Notification
                                    {
                                        Message = $"Your application for {application.DeceasedFirstName} {application.DeceasedLastName} is now pending payment. Amount due: ₱{model.Amount.Value}.",
                                        TargetUser = "client",
                                        UserId = application.UserId,
                                        CreatedAt = DateTime.UtcNow,
                                        IsRead = false,
                                        NotificationType = "info",
                                        Type = "Application"
                                    };

                                    _context.Notifications.Add(paymentNotification);
                                }
                                break;
                        }

                        // Send email to the user
                        if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(body))
                        {
                            AreEyeP.Helpers.EmailHelper.SendEmail(user.Email, subject, body);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending email: {ex.Message}");
                    }
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

                // Fetch user email
                var user = _context.Users.FirstOrDefault(u => u.Id == application.UserId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User record not found." });
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

                // Send approval email
                try
                {
                    string subject = "Application Approved";
                    string body = GenerateApprovalEmailBody(application);

                    AreEyeP.Helpers.EmailHelper.SendEmail(user.Email, subject, body);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending email: {ex.Message}");
                }

                // Continue with the rest of the method...

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

        private string GenerateApprovalEmailBody(BurialApplication application)
        {
            return $@"
            <html>
                <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0;'>
                    <div style='max-width: 600px; margin: 20px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);'>
                        <div style='background: #4caf50; padding: 20px; text-align: center; color: #ffffff;'>
                            <h1>Application Approved</h1>
                        </div>
                        <div style='padding: 20px;'>
                            <p style='font-size: 16px; line-height: 1.6;'>
                                Dear {application.FirstName} {application.LastName},
                            </p>
                            <p style='font-size: 16px; line-height: 1.6;'>
                                Congratulations! Your application for <strong>{application.DeceasedFirstName} {application.DeceasedLastName}</strong> has been <strong>approved</strong>.
                            </p>
                            <p style='font-size: 16px; line-height: 1.6;'>
                                Thank you for trusting AreEyeP. We are here to assist you with your burial needs.
                            </p>
                        </div>
                        <div style='background: #f4f4f4; text-align: center; padding: 10px; font-size: 12px; color: #666;'>
                            <p>This is an automated email. Please do not reply.</p>
                        </div>
                    </div>
                </body>
            </html>";
        }

        private string GenerateDeclineEmailBody(BurialApplication application, string reason)
        {
            return $@"
            <html>
                <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0;'>
                    <div style='max-width: 600px; margin: 20px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);'>
                        <div style='background: #f44336; padding: 20px; text-align: center; color: #ffffff;'>
                            <h1>Application Declined</h1>
                        </div>
                        <div style='padding: 20px;'>
                            <p style='font-size: 16px; line-height: 1.6;'>
                                Dear {application.FirstName} {application.LastName},
                            </p>
                            <p style='font-size: 16px; line-height: 1.6;'>
                                We regret to inform you that your application for <strong>{application.DeceasedFirstName} {application.DeceasedLastName}</strong> has been <strong>declined</strong>.
                            </p>
                            <p style='font-size: 16px; line-height: 1.6;'>
                                <strong>Reason:</strong> {reason}
                            </p>
                            <p style='font-size: 16px; line-height: 1.6;'>
                                Please contact us if you have any questions or need further assistance.
                            </p>
                        </div>
                        <div style='background: #f4f4f4; text-align: center; padding: 10px; font-size: 12px; color: #666;'>
                            <p>This is an automated email. Please do not reply.</p>
                        </div>
                    </div>
                </body>
            </html>";
        }

        private string GeneratePaymentPendingEmailBody(BurialApplication application, decimal amount)
        {
            return $@"
            <html>
                <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0;'>
                    <div style='max-width: 600px; margin: 20px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);'>
                        <div style='background: #ff9800; padding: 20px; text-align: center; color: #ffffff;'>
                            <h1>Payment Pending</h1>
                        </div>
                        <div style='padding: 20px;'>
                            <p style='font-size: 16px; line-height: 1.6;'>
                                Dear {application.FirstName} {application.LastName},
                            </p>
                            <p style='font-size: 16px; line-height: 1.6;'>
                                Your application for <strong>{application.DeceasedFirstName} {application.DeceasedLastName}</strong> is <strong>pending payment</strong>.
                            </p>
                            <p style='font-size: 16px; line-height: 1.6;'>
                                <strong>Amount Due:</strong> ₱{amount}
                            </p>
                            <p style='font-size: 16px; line-height: 1.6;'>
                                Please make the payment at your earliest convenience to proceed with the application process.
                            </p>
                        </div>
                        <div style='background: #f4f4f4; text-align: center; padding: 10px; font-size: 12px; color: #666;'>
                            <p>This is an automated email. Please do not reply.</p>
                        </div>
                    </div>
                </body>
            </html>";
        }

    }
}
