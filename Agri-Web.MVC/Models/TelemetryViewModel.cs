namespace Agri_Web.MVC.Models
{
    public class TelemetryViewModel
    {
        public string FarmName { get; set; } = string.Empty;

        public string SensorId { get; set; } = string.Empty;

        public string FieldId { get; set; } = string.Empty;

        public double SoilMoisture { get; set; }

        public double Temperature { get; set; }

        public double PHLevel { get; set; }

        public DateTimeOffset RecordedAt { get; set; }
    }
}