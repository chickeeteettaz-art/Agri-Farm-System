using System.ComponentModel.DataAnnotations;

namespace Agri_Web.MVC.Models
{
    public class DeviceViewModel
    {
        public int DeviceId { get; set; }

        [Required]
        [StringLength(100)]
        public string DeviceName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string DeviceType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FarmName { get; set; } = string.Empty;

        public string Status { get; set; } = "Offline";

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}