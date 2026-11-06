using Azure;
using Azure.Data.Tables;

namespace ClassLibrary.Data
{
    public class Telemetry : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;

        public string RowKey { get; set; } = string.Empty;

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string SensorId { get; set; } = string.Empty;

        public string FarmName { get; set; } = string.Empty;

        public double SoilMoisture { get; set; }

        public double Temperature { get; set; }

        public double PHLevel { get; set; }

        public DateTime RecordedAt { get; set; }
            = DateTime.UtcNow;
    }
}