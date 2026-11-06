using Agri_Web.MVC.Models;

namespace Agri_Web.MVC.Services.Interfaces
{
    public interface ITelemetryService
    {
        Task<List<TelemetryViewModel>> GetTelemetryAsync(
            string farmName);
    }
}