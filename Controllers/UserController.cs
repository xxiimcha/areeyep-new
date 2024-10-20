using AreEyeP.Data;
using AreEyeP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace AreEyeP.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: User/Index (this can show the list of users)
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // POST: User/AddUser (this is for adding new users)
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

        [HttpPost]
        public async Task<IActionResult> DeleteUser([FromBody] int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "User deleted successfully." });
            }
            return Json(new { success = false, message = "User not found." });
        }

    }
}
