using System.ComponentModel.DataAnnotations;

namespace ClassLibrary.Data
{
    public class Device
    {
        [Key]
        public int DeviceId { get; set; }

        public string DeviceName { get; set; } = string.Empty;

        public string DeviceType { get; set; } = string.Empty;

        public string FarmName { get; set; } = string.Empty;

        public string Status { get; set; } = "Offline";

        public DateTime LastUpdated { get; set; }
            = DateTime.UtcNow;
    }
}