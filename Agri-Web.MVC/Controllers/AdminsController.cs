
using Agri_Web.MVC.Models;
using ClassLibrary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agri_Web.MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Admin> _hasher;

        public AdminsController(
            ApplicationDbContext context,
            IPasswordHasher<Admin> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Admins
                .AsNoTracking()
                .ToListAsync());
        }

        public IActionResult Create()
        {
            return View(new UserManagementViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            UserManagementViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string username = model.Username.Trim();

            bool exists =
                await _context.Admins.AnyAsync(x => x.Username == username) ||
                await _context.Farmers.AnyAsync(x => x.Username == username) ||
                await _context.Agronomists.AnyAsync(x => x.Username == username);

            if (exists)
            {
                ModelState.AddModelError(
                    "Username",
                    "Username already exists.");

                return View(model);
            }

            var admin = new Admin
            {
                Username = username,
                Role = "Admin"
            };

            admin.Password = _hasher.HashPassword(
                admin,
                model.Password);

            _context.Admins.Add(admin);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Administrator created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var admin = await _context.Admins.FindAsync(id);

            if (admin == null)
                return NotFound();

            // Prevent an administrator from deleting their own account.
            string? currentId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (currentId == id.ToString())
            {
                TempData["ErrorMessage"] =
                    "You cannot delete your own administrator account.";

                return RedirectToAction(nameof(Index));
            }

            _context.Admins.Remove(admin);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
