namespace ProcessIrrigationFunction.Models
{
    public class IrrigationCommand
    {
        public string CommandId { get; set; } = string.Empty;

        public string DeviceId { get; set; } = string.Empty;

        public string Command { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string? RequestedByUserId { get; set; }
    }
}