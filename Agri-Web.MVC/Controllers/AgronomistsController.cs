
using Agri_Web.MVC.Models;
using ClassLibrary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agri_Web.MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AgronomistsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Agronomist> _hasher;

        public AgronomistsController(
            ApplicationDbContext context,
            IPasswordHasher<Agronomist> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Agronomists
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

            if (await UsernameExists(username))
            {
                ModelState.AddModelError(
                    "Username",
                    "This username is already registered.");

                return View(model);
            }

            var agronomist = new Agronomist
            {
                Username = username,
                Role = "Agronomist"
            };

            agronomist.Password = _hasher.HashPassword(
                agronomist,
                model.Password);

            _context.Agronomists.Add(agronomist);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Agronomist account created successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var agronomist = await _context.Agronomists.FindAsync(id);

            if (agronomist == null)
                return NotFound();

            return View(agronomist);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string username)
        {
            var agronomist = await _context.Agronomists.FindAsync(id);

            if (agronomist == null)
                return NotFound();

            username = username.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Username is required.");

                return View(agronomist);
            }

            bool duplicate = await _context.Agronomists
                .AnyAsync(x => x.Username == username &&
                               x.AgronomistId != id);

            if (duplicate)
            {
                ModelState.AddModelError(
                    "Username",
                    "This username is already in use.");

                return View(agronomist);
            }

            if (await _context.Farmers.AnyAsync(x => x.Username == username) ||
                await _context.Admins.AnyAsync(x => x.Username == username))
            {
                ModelState.AddModelError(
                    "Username",
                    "This username is already in use.");

                return View(agronomist);
            }

            agronomist.Username = username;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Agronomist updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var agronomist = await _context.Agronomists.FindAsync(id);

            if (agronomist == null)
                return NotFound();

            _context.Agronomists.Remove(agronomist);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> UsernameExists(string username)
        {
            return await _context.Farmers.AnyAsync(x => x.Username == username)
                || await _context.Admins.AnyAsync(x => x.Username == username)
                || await _context.Agronomists.AnyAsync(x => x.Username == username);
        }
    }
}
