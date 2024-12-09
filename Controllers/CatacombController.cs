using AreEyeP.Data;
using AreEyeP.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

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
        public IActionResult Index()
        {
            var catacombs = _context.Catacombs.ToList();
            return View(catacombs);
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

        // POST: /Catacomb/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Catacomb model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Set the Catacomb ID if not already set
                    if (string.IsNullOrEmpty(model.CatacombID))
                    {
                        model.CatacombID = GenerateCatacombID();
                    }

                    // Set default values
                    model.AvailabilityStatus = "Available";
                    model.DateCreated = DateTime.UtcNow;

                    // Add to the database
                    _context.Catacombs.Add(model);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, catacombID = model.CatacombID });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Error: {ex.Message}" });
                }
            }

            var errorMessages = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Failed to add Catacomb. Check your input.", errors = errorMessages });
        }

        // GET: /Catacomb/Details/{id}
        [HttpGet]
        public IActionResult Details(int id)
        {
            var catacomb = _context.Catacombs.FirstOrDefault(c => c.Id == id);
            if (catacomb == null)
            {
                return Json(new { success = false, message = "Catacomb not found." });
            }

            return Json(new { success = true, catacomb });
        }

        // PUT: /Catacomb/Edit/{id}
        [HttpPut]
        public async Task<IActionResult> Edit(int id, [FromBody] Catacomb updatedCatacomb)
        {
            if (ModelState.IsValid)
            {
                var existingCatacomb = await _context.Catacombs.FindAsync(id);
                if (existingCatacomb == null)
                {
                    return Json(new { success = false, message = "Catacomb not found." });
                }

                try
                {
                    // Update fields
                    existingCatacomb.CatacombName = updatedCatacomb.CatacombName;
                    existingCatacomb.Location = updatedCatacomb.Location;
                    existingCatacomb.DateCreated = updatedCatacomb.DateCreated;

                    _context.Catacombs.Update(existingCatacomb);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Catacomb updated successfully." });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Error: {ex.Message}" });
                }
            }

            var errorMessages = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Failed to update Catacomb. Check your input.", errors = errorMessages });
        }

        // DELETE: /Catacomb/Delete/{id}
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var catacomb = await _context.Catacombs.FindAsync(id);
            if (catacomb == null)
            {
                return Json(new { success = false, message = "Catacomb not found." });
            }

            try
            {
                _context.Catacombs.Remove(catacomb);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Catacomb deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public JsonResult GetLocations()
        {
            // Fetch all fields from the Catacombs table
            var catacombs = _context.Catacombs
                .Select(c => new
                {
                    c.Id,
                    c.CatacombID,
                    c.CatacombName,
                    c.Location,
                    c.AvailabilityStatus,
                    c.DateCreated,
                    c.DeceasedInformation // Assuming this is a nullable column
                })
                .ToList();

            // Process the data to extract latitude and longitude from the Location field
            var locations = catacombs
                .Where(c => !string.IsNullOrEmpty(c.Location) && c.Location.Contains(","))
                .Select(c =>
                {
                    var cleanedLocation = c.Location.Replace(", ", ","); // Remove spaces after commas
                    var coordinates = cleanedLocation.Split(',');
                    if (coordinates.Length == 2 &&
                        double.TryParse(coordinates[0].Trim(), out double latitude) &&
                        double.TryParse(coordinates[1].Trim(), out double longitude))
                    {
                        return new
                        {
                            c.Id,
                            c.CatacombID,
                            c.CatacombName,
                            Latitude = latitude,
                            Longitude = longitude,
                            c.AvailabilityStatus,
                            c.DateCreated,
                            c.DeceasedInformation
                        };
                    }

                    return null; // Exclude rows with invalid Location data
                })
                .Where(c => c != null) // Remove null results
                .ToList();

            return Json(new { success = true, locations });
        }
    }
}
