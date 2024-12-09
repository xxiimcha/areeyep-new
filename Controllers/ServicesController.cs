using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data;
using AreEyeP.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AreEyeP.Controllers
{
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Services
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services.ToListAsync();
            // Explicitly specifying the path to the view in the Shared folder
            return View("/Views/Shared/Services.cshtml", services);
        }

        // POST: Services/AddService
        [HttpPost]
        public async Task<IActionResult> AddService(Service service)
        {
            if (ModelState.IsValid)
            {
                _context.Services.Add(service);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Service added successfully!" });
            }

            return Json(new { success = false, message = "Invalid service details provided." });
        }

        // POST: Services/DeleteService
        [HttpPost]
        public async Task<IActionResult> DeleteService([FromBody] int id)
        {
            Console.WriteLine($"DeleteService called with id: {id}");

            if (id <= 0)
            {
                Console.WriteLine("Invalid service ID received.");
                return Json(new { success = false, message = "Invalid service ID." });
            }

            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id);
            if (service != null)
            {
                Console.WriteLine($"Service found: {service.ServiceName}");
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Service deleted successfully!" });
            }

            Console.WriteLine($"Service not found for id: {id}");
            return Json(new { success = false, message = "Service not found." });
        }

    }
}
