using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data;
using AreEyeP.Models;
using System.Linq;

namespace AreEyeP.Controllers
{
    public class CatacombController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CatacombController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Catacomb
        [HttpGet]
        public IActionResult Catacombs()
        {
            var catacombs = _context.Catacombs.ToList();
            ViewBag.GeneratedCatacombID = GenerateCatacombID(); // Pass the generated ID to the view
            return View(catacombs); // Assuming this is your main page for displaying catacombs
        }

        // POST: /Catacomb/Create
        [HttpPost]
        public IActionResult Create([FromBody] Catacomb model) // [FromBody] ensures the JSON body is bound correctly
        {
            if (ModelState.IsValid)
            {
                // Generate the Catacomb ID if not already provided
                if (string.IsNullOrEmpty(model.CatacombID))
                {
                    model.CatacombID = GenerateCatacombID();
                }

                // Set other fields
                model.DateCreated = DateTime.UtcNow;
                model.AvailabilityStatus = "Available"; // Set default status

                // Save to database
                _context.Catacombs.Add(model);
                _context.SaveChanges();

                return Json(new { success = true, catacombID = model.CatacombID }); // Return the new Catacomb ID
            }

            return Json(new { success = false, message = "Invalid data" });
        }

        // Method to generate the CatacombID
        private string GenerateCatacombID()
        {
            var latestCatacomb = _context.Catacombs
                .OrderByDescending(c => c.Id)
                .FirstOrDefault();

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
    }
}
