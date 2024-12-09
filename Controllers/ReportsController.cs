using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data;
using System.Linq;

namespace AreEyeP.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Reports/
        public IActionResult Index()
        {
            // Fetch burial application statuses
            var applicationStatuses = _context.BurialApplications
                .GroupBy(a => a.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToList();

            // Fetch financial data from payments
            var paymentsData = _context.ClientPayments
                .GroupBy(p => p.PaymentDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .ToList();

            // Pass data to the "LGU/Reports.cshtml" view
            return View("~/Views/LGU/Reports.cshtml", new
            {
                ApplicationStatuses = applicationStatuses,
                PaymentsData = paymentsData
            });
        }
    }
}
