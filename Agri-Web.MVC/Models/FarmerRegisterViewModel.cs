using System.ComponentModel.DataAnnotations;

namespace Agri_Web.MVC.Models
{
    public class FarmerRegisterViewModel
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Farm Name")]
        public string FarmName { get; set; } = string.Empty;
    }
}