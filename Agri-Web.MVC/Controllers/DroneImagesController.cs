
using Agri_Web.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Agri_Web.MVC.Controllers
{
    public class DroneImagesController : Controller
    {
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<DroneImagesController> _logger;

        public DroneImagesController(
            IBlobStorageService blobStorageService,
            ILogger<DroneImagesController> logger)
        {
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index(string farmName = "Farm001")
        {
            ViewBag.FarmName = farmName;
            return View();
        }

        [HttpGet]
        public IActionResult Upload(string farmName = "Farm001")
        {
            ViewBag.FarmName = farmName;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            IFormFile image,
            string farmName)
        {
            if (image == null || image.Length == 0)
            {
                ModelState.AddModelError(
                    "image",
                    "Please select a drone image.");

                ViewBag.FarmName = farmName;
                return View();
            }

            if (string.IsNullOrWhiteSpace(farmName))
            {
                ModelState.AddModelError(
                    "farmName",
                    "Farm name is required.");

                return View();
            }

            try
            {
                string blobPath =
                    await _blobStorageService.UploadImageAsync(
                        image,
                        farmName);

                TempData["SuccessMessage"] =
                    "Drone image uploaded successfully. " +
                    "The Blob Trigger will automatically create an audit record.";

                TempData["BlobPath"] = blobPath;

                return RedirectToAction(
                    nameof(Index),
                    new { farmName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Drone image upload failed.");

                ModelState.AddModelError(
                    "",
                    ex.Message);

                ViewBag.FarmName = farmName;
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Images(
            string farmName = "Farm001")
        {
            try
            {
                var images =
                    await _blobStorageService.GetImagesAsync(farmName);

                ViewBag.FarmName = farmName;

                return View("Index", images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to retrieve drone images.");

                TempData["ErrorMessage"] =
                    "Unable to retrieve uploaded images.";

                return View("Index", new List<string>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            string farmName,
            string imageName)
        {
            try
            {
                bool deleted =
                    await _blobStorageService.DeleteImageAsync(
                        farmName,
                        imageName);

                TempData[deleted ? "SuccessMessage" : "ErrorMessage"] =
                    deleted
                        ? "Image deleted successfully."
                        : "The image could not be found.";

                return RedirectToAction(
                    nameof(Images),
                    new { farmName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to delete drone image.");

                TempData["ErrorMessage"] =
                    "An error occurred while deleting the image.";

                return RedirectToAction(
                    nameof(Images),
                    new { farmName });
            }
        }
    }
}
