
using Agri_Web.MVC.Models;
using Agri_Web.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Agri_Web.MVC.Controllers
{
    public class IrrigationController : Controller
    {
        private readonly IIrrigationService _irrigationService;
        private readonly IProcessIrrigationService _processService;
        private readonly ILogger<IrrigationController> _logger;

        public IrrigationController(
            IIrrigationService irrigationService,
            IProcessIrrigationService processService,
            ILogger<IrrigationController> logger)
        {
            _irrigationService = irrigationService;
            _processService = processService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new IrrigationCommandViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QueueCommand(
            IrrigationCommandViewModel command)
        {
            if (command.Command == "STOP_IRRIGATION")
            {
                command.DurationMinutes = 0;
            }

            if (!ModelState.IsValid)
            {
                return View("Index", command);
            }

            try
            {
                var result =
                    await _irrigationService.SendCommandAsync(command);

                return View("Result", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to queue irrigation command.");

                ModelState.AddModelError(
                    "",
                    "Unable to submit the irrigation command: " + ex.Message);

                return View("Index", command);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCommand(
            IrrigationCommandViewModel command,
            string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
            {
                TempData["ErrorMessage"] =
                    "A command ID is required for processing.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result =
                    await _processService.ProcessCommandAsync(
                        command,
                        commandId);

                return View("Result", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to process irrigation command.");

                TempData["ErrorMessage"] =
                    "Unable to process the command: " + ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }
    }
}
