using Microsoft.AspNetCore.Mvc;
using AreEyeP.Models;
using AreEyeP.Data;
using AreEyeP.Helpers;
using System;
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
            SeedAdminUser();
        }

        private void SeedAdminUser()
        {
            if (!_context.Users.Any(u => u.Role == "admin"))
            {
                var adminUser = new User
                {
                    Name = "System Administrator",
                    Email = "admin@example.com",
                    Password = "Admin@123",
                    Role = "admin"
                };

                _context.Users.Add(adminUser);
                _context.SaveChanges();
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

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
                    HttpContext.Session.SetString("UserRole", user.Role);
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserName", user.Name);

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

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegistrationViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool isRegistrationSuccessful = SaveNewUser(model);

                if (isRegistrationSuccessful)
                {
                    try
                    {
                        // Generate personalized welcome email
                        string subject = "Welcome to AreEyeP!";
                        string body = GenerateWelcomeEmailBody(model.FirstName, model.LastName);

                        // Send the welcome email
                        EmailHelper.SendEmail(model.Email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending email: {ex.Message}");
                        TempData["ErrorMessage"] = "Your account was created, but we couldn't send a welcome email. Please contact support.";
                    }

                    TempData["SuccessMessage"] = "Your account has been registered successfully! Please check your email for a welcome message.";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "There was an error during registration.");
                }
            }

            return View(model);
        }

        private User ValidateUser(string email, string password)
        {
            return _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
        }

        private bool SaveNewUser(RegistrationViewModel model)
        {
            try
            {
                var user = new User
                {
                    Name = $"{model.FirstName} {model.LastName}",
                    Email = model.Email,
                    Password = model.Password,
                    Role = "client"
                };
                _context.Users.Add(user);
                _context.SaveChanges();

                model.UserId = user.Id;

                _context.Clients.Add(model);
                _context.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving new user: {ex.Message}");
                return false;
            }
        }

        private string GenerateWelcomeEmailBody(string firstName, string lastName)
        {
            return $@"
                <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 10px; padding: 20px;'>
                            <div style='text-align: center;'>
                                <img src='https://via.placeholder.com/150' alt='AreEyeP Logo' style='max-width: 150px; margin-bottom: 20px;' />
                            </div>
                            <h1 style='color: #2c3e50; text-align: center;'>Welcome, {firstName} {lastName}!</h1>
                            <p style='font-size: 16px;'>Thank you for joining <strong>AreEyeP</strong>. Your account has been successfully created.</p>
                            <p style='font-size: 16px;'>You can now log in to your account and explore our services.</p>
                            <p style='font-size: 16px;'>If you have any questions, feel free to reach out to us at <a href='mailto:support@areeyep.com' style='color: #3498db;'>support@areeyep.com</a>.</p>
                            <hr style='border-top: 1px solid #ddd;' />
                            <p style='font-size: 14px; text-align: center;'>Best regards,<br /><strong>The AreEyeP Team</strong></p>
                            <p style='font-size: 12px; text-align: center; color: #999;'>This is an automated email. Please do not reply.</p>
                        </div>
                    </body>
                </html>";
        }
    }
}
