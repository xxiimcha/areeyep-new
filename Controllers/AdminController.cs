using AreEyeP.Data;
using AreEyeP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace AreEyeP.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Dashboard
        public IActionResult Dashboard()
        {
            return View();
        }

        // GET: Admin/User
        public async Task<IActionResult> User()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // GET: /Admin/Catacombs
        public IActionResult Catacombs()
        {
            var catacombs = _context.Catacombs.ToList();
            return View(catacombs);
        }

        // POST: Admin/AddUser
        [HttpPost]
        public async Task<IActionResult> AddUser(User user)
        {
            if (ModelState.IsValid)
            {
                _context.Users.Add(user); // Add the user to the DbSet
                await _context.SaveChangesAsync(); // Save changes to the database
                return Json(new { success = true, message = "User added successfully!" });
            }

            return Json(new { success = false, message = "Failed to add user." });
        }

        // POST: Admin/DeleteUser
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(User));
        }

        // GET: Admin/Payments
        public async Task<IActionResult> Payments()
        {
            var payments = await _context.Payments.ToListAsync();
            return View(payments);
        }

        // POST: Admin/AddPaymentDetails
        [HttpPost]
        public async Task<IActionResult> AddPaymentDetails(Payment payment, IFormFile QrCode)
        {
            if (ModelState.IsValid)
            {
                // Handle file upload for QR code
                if (QrCode != null && QrCode.Length > 0)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", QrCode.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await QrCode.CopyToAsync(stream);
                    }
                    payment.QrCodePath = "/uploads/" + QrCode.FileName; // Save the relative path
                }

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Payments));
            }
            return View("Payments", await _context.Payments.ToListAsync());
        }
    }
}
