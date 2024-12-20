using System.Linq; // For LINQ queries
using System.Threading.Tasks; // For async methods
using Microsoft.AspNetCore.Mvc;
using AreEyeP.Data;
using Microsoft.EntityFrameworkCore;

public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Fetch total users and client accounts
        var totalUsers = await _context.Users.CountAsync();
        var clientAccounts = await _context.Users.CountAsync(u => u.Role.ToLower() == "client");

        // Pass data to the view using ViewBag or ViewData
        ViewBag.TotalUsers = totalUsers;
        ViewBag.ClientAccounts = clientAccounts;

        return View();
    }
}
