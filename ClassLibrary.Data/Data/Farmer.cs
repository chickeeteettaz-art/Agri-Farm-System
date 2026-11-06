using System.ComponentModel.DataAnnotations;

namespace ClassLibrary.Data
{
    public class Farmer
    {
        [Key]
        public int FarmerId { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "Farmer";

        public string FarmName { get; set; } = string.Empty;
    }
}