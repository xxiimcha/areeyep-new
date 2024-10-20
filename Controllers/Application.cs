using Microsoft.AspNetCore.Mvc;
using AreEyeP.Models;
using System.Linq;
using System.Threading.Tasks;
using AreEyeP.Data;

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
                _context.SaveChanges();
                return Json(new { success = true, message = "Application status updated successfully." });
            }
            return Json(new { success = false, message = "Application not found." });
        }

        // Model for the status update
        public class UpdateStatusModel
        {
            public int Id { get; set; }
            public string Status { get; set; }
        }

        // GET: /Application/GetDetails?id=1
        [HttpGet]
        public JsonResult GetDetails(int id)
        {
            var application = _context.BurialApplications.FirstOrDefault(a => a.Id == id);
            if (application != null)
            {
                return Json(new
                {
                    success = true,
                    application = new
                    {
                        application.FirstName,
                        application.LastName,
                        DateOfDeath = application.DateOfDeath.ToString("yyyy-MM-dd"),
                        DateOfBurial = application.DateOfBurial.ToString("yyyy-MM-dd"),
                        application.SpecialInstructions,
                        application.CauseOfDeath,
                        application.Religion,
                        application.Address,
                        application.Status
                        // Add more fields as needed
                    }
                });
            }

            return Json(new { success = false });
        }
    }
}
