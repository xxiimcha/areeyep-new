﻿using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data; // Ensure you have the right namespace for your DbContext
using AreEyeP.Models;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace AreEyeP.Controllers
{
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context; // Reference to your database context

        public StaffController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Staff/Dashboard
        public IActionResult Dashboard()
        {
            // Check if user is logged in and has a valid role
            if (!IsAuthorized("staff"))
            {
                return RedirectToAction("Login", "Account");
            }

            // Logic to fetch data related to the staff's dashboard
            return View();
        }

        // GET: /Staff/ManageProfile
        public IActionResult Profile()
        {
            // Check if user is logged in
            var user = GetLoggedInUser();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(user);
        }

        // GET: /Staff/Services
        public IActionResult Services()
        {
            if (!IsAuthorized("staff"))
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch services data for staff (replace with actual logic if necessary)
            return View();
        }

        // GET: /Staff/Catacombs
        public IActionResult Catacombs()
        {
            if (!IsAuthorized("staff"))
            {
                return RedirectToAction("Login", "Account");
            }

            var catacombs = _context.Catacombs.ToList();

            // Generate the next Catacomb ID
            string generatedCatacombID = GenerateCatacombID();
            ViewBag.GeneratedCatacombID = generatedCatacombID;

            return View(catacombs);
        }

        // Method to generate the CatacombID
        private string GenerateCatacombID()
        {
            var latestCatacomb = _context.Catacombs.OrderByDescending(c => c.Id).FirstOrDefault();

            int nextNumber = 1; // Start with 1 if no records found

            if (latestCatacomb != null && !string.IsNullOrEmpty(latestCatacomb.CatacombID))
            {
                var currentId = latestCatacomb.CatacombID.Replace("CTM-", "");
                if (int.TryParse(currentId, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            return $"CTM-{nextNumber:D3}";
        }

        // POST: /Staff/AddCatacomb
        [HttpPost]
        public IActionResult AddCatacomb(Catacomb catacomb)
        {
            if (!IsAuthorized("staff"))
            {
                return Json(new { success = false, message = "Unauthorized access." });
            }

            if (ModelState.IsValid)
            {
                catacomb.CatacombID = GenerateCatacombID();
                _context.Catacombs.Add(catacomb);
                _context.SaveChanges();
                return Json(new { success = true, message = "Catacomb added successfully." });
            }
            return Json(new { success = false, message = "Failed to add catacomb. Please check your input." });
        }

        // POST: /Staff/EditCatacomb
        [HttpPost]
        public IActionResult EditCatacomb(Catacomb catacomb)
        {
            if (!IsAuthorized("staff"))
            {
                return Json(new { success = false, message = "Unauthorized access." });
            }

            if (ModelState.IsValid)
            {
                var existingCatacomb = _context.Catacombs.FirstOrDefault(c => c.Id == catacomb.Id);
                if (existingCatacomb != null)
                {
                    existingCatacomb.CatacombName = catacomb.CatacombName;
                    existingCatacomb.Location = catacomb.Location;
                    existingCatacomb.DateCreated = catacomb.DateCreated;

                    _context.SaveChanges();
                    return Json(new { success = true, message = "Catacomb updated successfully." });
                }
                return Json(new { success = false, message = "Catacomb not found." });
            }
            return Json(new { success = false, message = "Failed to update catacomb. Please check your input." });
        }

        // POST: /Staff/DeleteCatacomb
        [HttpPost]
        public IActionResult DeleteCatacomb(int id)
        {
            if (!IsAuthorized("staff"))
            {
                return Json(new { success = false, message = "Unauthorized access." });
            }

            var catacomb = _context.Catacombs.FirstOrDefault(c => c.Id == id);
            if (catacomb != null)
            {
                _context.Catacombs.Remove(catacomb);
                _context.SaveChanges();
                return Json(new { success = true, message = "Catacomb deleted successfully." });
            }
            return Json(new { success = false, message = "Failed to delete catacomb." });
        }

        // GET: /Staff/Schedule
        public IActionResult Schedule()
        {
            return View(); // Make sure this returns the correct view name
        }


        // GET: /Staff/Notifications
        public IActionResult Notifications()
        {
            if (!IsAuthorized("staff"))
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch notifications for the staff (replace with actual logic if necessary)
            return View();
        }

        // Utility method to check authorization
        private bool IsAuthorized(string role)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole != null && userRole.ToLower() == role.ToLower();
        }

        // Utility method to get the logged-in user
        private User GetLoggedInUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            return _context.Users.FirstOrDefault(u => u.Id == userId);
        }
    }
}