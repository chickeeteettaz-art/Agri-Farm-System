namespace ClassLibrary.Data
{
    public class IrrigationCommand
    {
        public string CommandId { get; set; }
            = Guid.NewGuid().ToString();

        public int DeviceId { get; set; }

        public string Command { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}