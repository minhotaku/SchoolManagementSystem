using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Username cannot be empty")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password cannot be empty")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [StringLength(100, ErrorMessage = "The password must be at least {2} characters long.", MinimumLength = 6)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Role cannot be empty")]
        [Display(Name = "Role")]
        public string Role { get; set; }

        [Display(Name = "School Program")]
        public string SchoolProgramId { get; set; }
    }
}