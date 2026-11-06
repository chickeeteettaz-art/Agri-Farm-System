namespace Agri_Web.MVC.Models
{
    public class IrrigationCommandViewModel
    {
        public string DeviceId { get; set; } = string.Empty;

        public string Command { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }

        public string? RequestedByUserId { get; set; }
    }
}