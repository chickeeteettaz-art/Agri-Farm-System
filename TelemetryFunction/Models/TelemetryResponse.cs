using System;
using System.Collections.Generic;
using System.Text;

namespace TelemetryFunction.Models
{
    public class TelemetryResponse
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
