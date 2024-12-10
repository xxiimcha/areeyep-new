using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data; // Replace with your actual namespace for DbContext
using System.Linq;

namespace AreEyeP.Controllers
{
    [Route("Deceased")]
    public class DeceasedController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DeceasedController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetDetails/{id}")]
        public IActionResult GetDetails(int id)
        {
            var deceased = _context.Deceased
                .Where(d => d.Id == id)
                .Select(d => new
                {
                    success = true,
                    d.FirstName,
                    d.LastName,
                    DateOfBirth = d.DateOfBirth.ToString("yyyy-MM-dd"),
                    DateOfDeath = d.DateOfDeath.ToString("yyyy-MM-dd"),
                    d.Address,
                    d.CauseOfDeath,
                    d.Gender
                })
                .FirstOrDefault();

            if (deceased == null)
            {
                return NotFound(new { success = false, message = "Deceased not found" });
            }

            return Ok(deceased);
        }

        [HttpGet("GetAll")]
        public IActionResult GetAll()
        {
            var deceasedList = _context.Deceased
                .Select(d => new
                {
                    d.Id,
                    d.FirstName,
                    d.LastName,
                    DateOfBirth = d.DateOfBirth.ToString("yyyy-MM-dd"),
                    DateOfDeath = d.DateOfDeath.ToString("yyyy-MM-dd"),
                    d.Address,
                    d.CauseOfDeath,
                    d.Gender
                })
                .ToList();

            if (!deceasedList.Any())
            {
                return Ok(new { success = false, message = "No deceased records found." });
            }

            return Ok(new { success = true, deceased = deceasedList });
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
