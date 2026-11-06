
using ClassLibrary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agri_Web.MVC.Controllers
{
    [Authorize]
    public class FarmersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FarmersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var farmers = await _context.Farmers
                .AsNoTracking()
                .ToListAsync();

            return View(farmers);
        }

        public async Task<IActionResult> Details(int id)
        {
            if (User.IsInRole("Farmer") &&
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    != id.ToString())
            {
                return Forbid();
            }

            var farmer = await _context.Farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FarmerId == id);

            if (farmer == null)
                return NotFound();

            return View(farmer);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return RedirectToAction(
                "Create",
                "Auth");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var farmer = await _context.Farmers.FindAsync(id);

            if (farmer == null)
                return NotFound();

            _context.Farmers.Remove(farmer);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
