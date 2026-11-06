using Agri_Web.MVC.Models;

namespace Agri_Web.MVC.Services.Interfaces
{
    public interface IProcessIrrigationService
    {
        Task<IrrigationResultViewModel> ProcessCommandAsync(
            IrrigationCommandViewModel command,
            string commandId);
    }
}