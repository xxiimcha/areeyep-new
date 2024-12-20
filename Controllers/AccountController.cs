using Microsoft.AspNetCore.Mvc;
using AreEyeP.Models;
using AreEyeP.Data; // Ensure this is the namespace where your ApplicationDbContext is located
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace AreEyeP.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
            SeedAdminUser(); // Seed admin user upon controller instantiation
        }
        private void SeedAdminUser()
        {
            // Check if any admin user already exists
            if (!_context.Users.Any(u => u.Role == "admin"))
            {
                var adminUser = new User
                {
                    Name = "System Administrator",
                    Email = "admin@example.com",
                    Password = "Admin@123", // Default password (Consider hashing this in production)
                    Role = "admin"
                };

                _context.Users.Add(adminUser);
                _context.SaveChanges();
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            // Clear the user session
            HttpContext.Session.Clear();

            // Redirect to the login page
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = ValidateUser(model.Email, model.Password);

                if (user != null)
                {
                    // Set user session data
                    HttpContext.Session.SetString("UserRole", user.Role);
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserName", user.Name);

                    // Redirect based on role
                    switch (user.Role.Trim().ToLower())
                    {
                        case "admin":
                            return RedirectToAction("Dashboard", "Admin");
                        case "staff":
                            return RedirectToAction("Dashboard", "Staff");
                        case "lgu":
                            return RedirectToAction("Dashboard", "LGU");
                        case "client":
                            return RedirectToAction("Dashboard", "Client");
                        default:
                            TempData["ErrorMessage"] = "Invalid role.";
                            return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid email or password.";
                    return RedirectToAction("Index", "Home");
                }
            }

            TempData["ErrorMessage"] = "Please fill in all required fields.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public IActionResult Register(RegistrationViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Insert logic to save the new user to the database
                bool isRegistrationSuccessful = SaveNewUser(model);

                if (isRegistrationSuccessful)
                {
                    // Redirect to login after successful registration
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "There was an error during registration.");
                }
            }

            return View(model);
        }

        // Method for validating user credentials
        private User ValidateUser(string email, string password)
        {
            // Check against Users table in the database
            return _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
        }

        // Method for saving a new user
        private bool SaveNewUser(RegistrationViewModel model)
        {
            try
            {
                // Add the new user to the Users table
                var user = new User
                {
                    Name = $"{model.FirstName} {model.LastName}",
                    Email = model.Email,
                    Password = model.Password,
                    Role = "client" // Set role as 'client'
                };
                _context.Users.Add(user);

                // Save changes to get the user Id
                _context.SaveChanges();

                // Attach the userId to the Client table entry
                model.Id = user.Id;

                // Add the new user to the Client table using RegistrationViewModel
                _context.Clients.Add(model);

                // Save changes to the database
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
