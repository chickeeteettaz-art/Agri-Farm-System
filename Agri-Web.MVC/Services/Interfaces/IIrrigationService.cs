using Agri_Web.MVC.Models;

namespace Agri_Web.MVC.Services.Interfaces
{
    public interface IIrrigationService
    {
        Task<IrrigationResultViewModel> SendCommandAsync(
            IrrigationCommandViewModel command);
    }
}