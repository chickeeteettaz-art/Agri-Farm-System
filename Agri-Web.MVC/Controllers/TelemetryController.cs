
using Agri_Web.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Agri_Web.MVC.Controllers
{
    public class TelemetryController : Controller
    {
        private readonly ITelemetryService _telemetryService;
        private readonly ILogger<TelemetryController> _logger;

        public TelemetryController(
            ITelemetryService telemetryService,
            ILogger<TelemetryController> logger)
        {
            _telemetryService = telemetryService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string farmName = "Farm001")
        {
            try
            {
                var telemetry =
                    await _telemetryService.GetTelemetryAsync(farmName);

                ViewBag.FarmName = farmName;

                return View(telemetry);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to retrieve telemetry for {FarmName}.",
                    farmName);

                ViewBag.FarmName = farmName;
                ViewBag.ErrorMessage =
                    "Unable to retrieve sensor readings. Please try again.";

                return View(new List<Models.TelemetryViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Refresh(string farmName)
        {
            return await Index(farmName);
        }
    }
}
