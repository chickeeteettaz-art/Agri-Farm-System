using System.ComponentModel.DataAnnotations;

namespace ClassLibrary.Data
{
    public class Admin
    {
        [Key]
        public int AdminId { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "Admin";
    }
}