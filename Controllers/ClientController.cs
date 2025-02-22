﻿using Microsoft.AspNetCore.Mvc;
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

            // Fetch deceased records linked to the logged-in user along with the DateOfRenewal from BurialApplications
            var relatives = await _context.Deceased
                .Where(d => _context.BurialApplications
                    .Any(b => b.Id == d.ApplicationId && b.UserId == userId))
                .Join(
                    _context.BurialApplications,
                    d => d.ApplicationId,
                    b => b.Id,
                    (d, b) => new
                    {
                        d.Id,
                        d.FirstName,
                        d.LastName,
                        d.DateOfBirth,
                        d.DateOfDeath,
                        d.Address,
                        d.CauseOfDeath,
                        d.Gender,
                        d.ApplicationId,
                        DateOfRenewal = b.DateOfRenewal // Include DateOfRenewal from BurialApplications
                    }
                )
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
                                                PaymentMessage = sr.PaymentRequired ? "Payment is required" : "Pay upon completion",
                                                Amount = _context.ClientPayments
                                                    .Where(cp => cp.ServiceRequestId == sr.Id)
                                                    .OrderByDescending(cp => cp.PaymentDate)
                                                    .Select(cp => cp.Amount)
                                                    .FirstOrDefault() // Get the latest payment amount
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

        [HttpPost]
        public async Task<IActionResult> MakePayment(int serviceRequestId, decimal amount, string paymentMethod, IFormFile paymentProof)
        {
            try
            {
                // Validate the input parameters
                if (amount <= 0)
                {
                    return Json(new { success = false, message = "Invalid payment amount." });
                }

                if (string.IsNullOrEmpty(paymentMethod))
                {
                    return Json(new { success = false, message = "Payment method is required." });
                }

                // Find the service request
                var serviceRequest = await _context.ServiceRequests.FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

                if (serviceRequest == null || !serviceRequest.PaymentRequired)
                {
                    return Json(new { success = false, message = "Invalid service request or payment is not required." });
                }

                // Find the existing payment record
                var payment = await _context.ClientPayments.FirstOrDefaultAsync(p => p.ServiceRequestId == serviceRequestId);

                if (payment == null)
                {
                    return Json(new { success = false, message = "No existing payment record found for this service request." });
                }

                // Update the payment record
                payment.Amount = amount;
                payment.PaymentMethod = paymentMethod;
                payment.Status = "Completed";
                payment.PaymentDate = DateTime.UtcNow;

                // Handle payment proof upload if provided
                if (paymentProof != null && paymentProof.Length > 0)
                {
                    var uploadDirectory = Path.Combine("uploads/payment-proofs");
                    if (!Directory.Exists(uploadDirectory))
                    {
                        Directory.CreateDirectory(uploadDirectory);
                    }

                    var filePath = Path.Combine(uploadDirectory, Guid.NewGuid() + Path.GetExtension(paymentProof.FileName));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await paymentProof.CopyToAsync(stream);
                    }
                    payment.PaymentProof = filePath; // Save the file path in the database
                }

                // Update the service request to mark it as paid
                serviceRequest.PaymentRequired = false;

                // Save changes
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Payment updated successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating payment: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = "An error occurred while updating the payment." });
            }
        }


        [HttpGet("/Client/GetPaymentMethods/{serviceRequestId}")]
        public IActionResult GetPaymentMethods(int serviceRequestId)
        {
            try
            {
                // Fetch payment methods from the Payments table
                var paymentMethods = _context.Payments
                    .Select(p => new
                    {
                        paymentMode = p.PaymentMode,
                        qrCodePath = p.QrCodePath,
                        bankName = p.BankName,
                        accountNumber = p.AccountNumber
                    })
                    .ToList();

                // Fetch the amount to be paid for the specific ServiceRequestId
                var paymentAmount = _context.ClientPayments
                    .Where(cp => cp.ServiceRequestId == serviceRequestId)
                    .OrderByDescending(cp => cp.PaymentDate)
                    .Select(cp => cp.Amount)
                    .FirstOrDefault();

                return Json(new { success = true, methods = paymentMethods, amount = paymentAmount });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching payment methods: {ex.Message}");
                return Json(new { success = false, message = "Failed to fetch payment methods or payment amount." });
            }
        }
    }
}