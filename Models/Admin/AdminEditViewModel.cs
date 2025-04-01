using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class AdminEditViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [Display(Name = "Admin ID")]
        public string AdminId { get; set; }

        [Required(ErrorMessage = "Username cannot be empty.")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Display(Name = "New password (leave empty to keep current password)")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}