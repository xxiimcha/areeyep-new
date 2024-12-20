using AreEyeP.Data;
using AreEyeP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
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

        // Helper Method: Get Logged-In User
        private User GetLoggedInUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return null;

            return _context.Users.FirstOrDefault(u => u.Id == userId);
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
            var user = await _context.Users.FindAsync(updatedUser.Id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            if (ModelState.IsValid)
            {
                user.Name = updatedUser.Name;
                user.Email = updatedUser.Email;

                // Update the password only if it's provided
                if (!string.IsNullOrEmpty(updatedUser.Password))
                {
                    user.Password = updatedUser.Password; // Add hashing if required
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "User updated successfully!" });
            }

            return Json(new { success = false, message = "Invalid data provided." });
        }

        // POST: User/UpdateProfile (this is for updating user information by the user)
        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] User updatedUser)
        {
            try
            {
                Console.WriteLine("UpdateProfile called with data:");
                Console.WriteLine($"Name: {updatedUser.Name}, Email: {updatedUser.Email}");

                var loggedInUser = GetLoggedInUser();
                if (loggedInUser == null)
                {
                    Console.WriteLine("Logged-in user not found or not logged in.");
                    return Json(new { success = false, message = "User not found or not logged in." });
                }

                Console.WriteLine($"Logged-in user ID: {loggedInUser.Id}, Name: {loggedInUser.Name}, Email: {loggedInUser.Email}");

                // Remove Role and Password from ModelState validation
                ModelState.Remove("Role");
                ModelState.Remove("Password");

                if (ModelState.IsValid)
                {
                    loggedInUser.Name = updatedUser.Name;
                    loggedInUser.Email = updatedUser.Email;

                    Console.WriteLine("Updating user information in the database...");
                    _context.Users.Update(loggedInUser);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("Profile updated successfully.");
                    return Json(new { success = true, message = "Profile updated successfully!" });
                }
                else
                {
                    Console.WriteLine("ModelState is invalid.");
                    foreach (var key in ModelState.Keys)
                    {
                        foreach (var error in ModelState[key].Errors)
                        {
                            Console.WriteLine($"ModelState Error - Key: {key}, Error: {error.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred in UpdateProfile: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return Json(new { success = false, message = "An unexpected error occurred while updating the profile." });
            }

            Console.WriteLine("Returning invalid data response.");
            return Json(new { success = false, message = "Invalid data provided." });
        }

        // POST: User/ChangePassword (this is for updating user password by the user)
        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            var loggedInUser = GetLoggedInUser();
            if (loggedInUser == null)
            {
                return Json(new { success = false, message = "User not found or not logged in." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided." });
            }

            // Replace with your password verification logic (e.g., hash comparison)
            if (loggedInUser.Password != model.CurrentPassword)
            {
                return Json(new { success = false, message = "Current password is incorrect." });
            }

            // Replace with password hashing if needed
            loggedInUser.Password = model.NewPassword;

            _context.Users.Update(loggedInUser);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Password changed successfully!" });
        }
    }

    public class ChangePasswordModel
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string NewPassword { get; set; }
    }
}
