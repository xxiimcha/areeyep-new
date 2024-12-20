using AreEyeP.Data;
using AreEyeP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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

        // POST: User/DeleteUser (this is for deleting users)
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

        // GET: User/GetUserById (this is for fetching user details for editing)
        [HttpGet]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                return Json(new
                {
                    success = true,
                    user = new
                    {
                        user.Id,
                        user.Name,
                        user.Email,
                        user.Role
                    }
                });
            }
            return Json(new { success = false, message = "User not found." });
        }

        // POST: User/EditUser (this is for updating user details)
        [HttpPost]
        public async Task<IActionResult> EditUser(User updatedUser)
        {
            if (!ModelState.IsValid)
            {
                // Remove validation for Password field during updates
                ModelState.Remove("Password");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    Console.WriteLine("ModelState Errors: " + string.Join(", ", errors));
                    return Json(new { success = false, message = "Invalid data provided.", errors });
                }
            }

            var user = await _context.Users.FindAsync(updatedUser.Id);
            if (user != null)
            {
                user.Name = updatedUser.Name;
                user.Email = updatedUser.Email;
                user.Role = updatedUser.Role;

                // Update the password only if it's provided
                if (!string.IsNullOrEmpty(updatedUser.Password))
                {
                    user.Password = updatedUser.Password; // Add hashing if required
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "User updated successfully!" });
            }

            return Json(new { success = false, message = "User not found." });
        }
    }
}
