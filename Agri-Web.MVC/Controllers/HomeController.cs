
using System.Security.Claims;
using ClassLibrary.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agri_Web.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Visitors see the public landing page.
            if (User.Identity?.IsAuthenticated != true)
            {
                return View();
            }

            string role = User.FindFirstValue(
                ClaimTypes.Role) ?? string.Empty;

            ViewBag.Username = User.Identity.Name;
            ViewBag.Role = role;

            // Farmer dashboard
            if (role == "Farmer")
            {
                string? userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (int.TryParse(userId, out int farmerId))
                {
                    var farmer = await _context.Farmers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x => x.FarmerId == farmerId);

                    if (farmer != null)
                    {
                        ViewBag.FarmName = farmer.FarmName;

                        ViewBag.DeviceCount =
                            await _context.Devices.CountAsync(
                                x => x.FarmName == farmer.FarmName);
                    }
                }
            }

            // Administrator dashboard
            if (role == "Admin")
            {
                ViewBag.FarmerCount =
                    await _context.Farmers.CountAsync();

                ViewBag.AgronomistCount =
                    await _context.Agronomists.CountAsync();

                ViewBag.AdminCount =
                    await _context.Admins.CountAsync();

                ViewBag.DeviceCount =
                    await _context.Devices.CountAsync();
            }

            // Agronomist dashboard
            if (role == "Agronomist")
            {
                ViewBag.DeviceCount =
                    await _context.Devices.CountAsync();
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
