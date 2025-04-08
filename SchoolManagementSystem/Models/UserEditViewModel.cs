using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class UserEditViewModel
    {
        public string UserId { get; set; }

        [Required(ErrorMessage = "Username cannot be empty")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Display(Name = "New password (leave blank to not change)")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required]
        public string CurrentRole { get; set; }

        [Display(Name = "Role")]
        public string Role => CurrentRole;

        [Display(Name = "School Program")]
        public string? SchoolProgramId { get; set; }
    }
}