
using System.Security.Claims;
using Agri_Web.MVC.Models;
using ClassLibrary.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agri_Web.MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Farmer> _farmerHasher;
        private readonly IPasswordHasher<Agronomist> _agronomistHasher;
        private readonly IPasswordHasher<Admin> _adminHasher;

        public AuthController(
            ApplicationDbContext context,
            IPasswordHasher<Farmer> farmerHasher,
            IPasswordHasher<Agronomist> agronomistHasher,
            IPasswordHasher<Admin> adminHasher)
        {
            _context = context;
            _farmerHasher = farmerHasher;
            _agronomistHasher = agronomistHasher;
            _adminHasher = adminHasher;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            FarmerRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string username = model.Username.Trim();

            bool farmerExists = await _context.Farmers
                .AnyAsync(x => x.Username == username);

            bool agronomistExists = await _context.Agronomists
                .AnyAsync(x => x.Username == username);

            bool adminExists = await _context.Admins
                .AnyAsync(x => x.Username == username);

            if (farmerExists || agronomistExists || adminExists)
            {
                ModelState.AddModelError(
                    "Username",
                    "This username is already registered.");

                return View(model);
            }

            var farmer = new Farmer
            {
                Username = username,
                FarmName = model.FarmName.Trim(),
                Role = "Farmer"
            };

            // Store the password hash, never the raw password.
            farmer.Password = _farmerHasher.HashPassword(
                farmer,
                model.Password);

            _context.Farmers.Add(farmer);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your account was created successfully. Please log in.";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            string username = model.Username.Trim();

            // Check Farmer accounts.
            var farmer = await _context.Farmers
                .SingleOrDefaultAsync(x => x.Username == username);

            if (farmer != null)
            {
                var result = _farmerHasher.VerifyHashedPassword(
                    farmer,
                    farmer.Password,
                    model.Password);

                if (result != PasswordVerificationResult.Failed)
                {
                    // Upgrade old hashes if the hashing algorithm changes.
                    if (result == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        farmer.Password = _farmerHasher.HashPassword(
                            farmer, model.Password);

                        await _context.SaveChangesAsync();
                    }

                    return await SignInUser(
                        farmer.FarmerId.ToString(),
                        farmer.Username,
                        "Farmer",
                        model.RememberMe,
                        returnUrl);
                }
            }

            // Check Agronomist accounts.
            var agronomist = await _context.Agronomists
                .SingleOrDefaultAsync(x => x.Username == username);

            if (agronomist != null)
            {
                var result = _agronomistHasher.VerifyHashedPassword(
                    agronomist,
                    agronomist.Password,
                    model.Password);

                if (result != PasswordVerificationResult.Failed)
                {
                    if (result == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        agronomist.Password = _agronomistHasher.HashPassword(
                            agronomist, model.Password);

                        await _context.SaveChangesAsync();
                    }

                    return await SignInUser(
                        agronomist.AgronomistId.ToString(),
                        agronomist.Username,
                        "Agronomist",
                        model.RememberMe,
                        returnUrl);
                }
            }

            // Check Administrator accounts.
            var admin = await _context.Admins
                .SingleOrDefaultAsync(x => x.Username == username);

            if (admin != null)
            {
                var result = _adminHasher.VerifyHashedPassword(
                    admin,
                    admin.Password,
                    model.Password);

                if (result != PasswordVerificationResult.Failed)
                {
                    if (result == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        admin.Password = _adminHasher.HashPassword(
                            admin, model.Password);

                        await _context.SaveChangesAsync();
                    }

                    return await SignInUser(
                        admin.AdminId.ToString(),
                        admin.Username,
                        "Admin",
                        model.RememberMe,
                        returnUrl);
                }
            }

            ModelState.AddModelError(
                "",
                "Invalid username or password.");

            return View(model);
        }

        private async Task<IActionResult> SignInUser(
            string userId,
            string username,
            string role,
            bool rememberMe,
            string? returnUrl)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return role switch
            {
                "Admin" => RedirectToAction("Index", "Admins"),
                "Agronomist" => RedirectToAction("Index", "Telemetry"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
