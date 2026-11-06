
using System.Security.Claims;
using ClassLibrary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agri_Web.MVC.Controllers
{
    [Authorize]
    public class DevicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DevicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.Devices.AsNoTracking();

            if (User.IsInRole("Farmer"))
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!int.TryParse(userId, out int farmerId))
                    return Forbid();

                var farmer = await _context.Farmers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.FarmerId == farmerId);

                if (farmer == null)
                    return Forbid();

                query = query.Where(
                    x => x.FarmName == farmer.FarmName);
            }

            return View(await query.ToListAsync());
        }

        [Authorize(Roles = "Admin,Agronomist")]
        public IActionResult Create()
        {
            return View(new Device());
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Agronomist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Device device)
        {
            if (!ModelState.IsValid)
                return View(device);

            device.DeviceId = 0;
            device.Status = "Offline";
            device.LastUpdated = DateTime.UtcNow;

            _context.Devices.Add(device);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Device registered successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var device = await _context.Devices
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DeviceId == id);

            if (device == null)
                return NotFound();

            if (!await CanAccessDevice(device))
                return Forbid();

            return View(device);
        }

        [Authorize(Roles = "Admin,Agronomist")]
        public async Task<IActionResult> Edit(int id)
        {
            var device = await _context.Devices.FindAsync(id);

            if (device == null)
                return NotFound();

            return View(device);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Agronomist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string deviceName,
            string deviceType,
            string farmName)
        {
            var device = await _context.Devices.FindAsync(id);

            if (device == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(deviceName) ||
                string.IsNullOrWhiteSpace(deviceType) ||
                string.IsNullOrWhiteSpace(farmName))
            {
                ModelState.AddModelError(
                    "",
                    "All device fields are required.");

                return View(device);
            }

            device.DeviceName = deviceName.Trim();
            device.DeviceType = deviceType.Trim();
            device.FarmName = farmName.Trim();
            device.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Device updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var device = await _context.Devices.FindAsync(id);

            if (device == null)
                return NotFound();

            _context.Devices.Remove(device);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Device deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> CanAccessDevice(Device device)
        {
            if (!User.IsInRole("Farmer"))
                return true;

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int farmerId))
                return false;

            var farmer = await _context.Farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FarmerId == farmerId);

            return farmer != null &&
                   farmer.FarmName == device.FarmName;
        }
    }
}
