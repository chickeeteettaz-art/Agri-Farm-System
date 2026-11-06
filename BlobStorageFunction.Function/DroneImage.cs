using Azure;
using Azure.Data.Tables;
using System;

namespace ClassLibrary.Data
{
    public class DroneImage : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;

        public string RowKey { get; set; } = string.Empty;

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string ImageName { get; set; } = string.Empty;

        public string FarmName { get; set; } = string.Empty;

        public string BlobUrl { get; set; } = string.Empty;

        public string Status { get; set; } = "Uploaded";

        public DateTime UploadedAt { get; set; }
            = DateTime.UtcNow;
    }
}