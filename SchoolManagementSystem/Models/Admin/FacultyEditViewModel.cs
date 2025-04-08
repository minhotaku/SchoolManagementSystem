using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class FacultyEditViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [Display(Name = "Faculty ID")]
        public string FacultyId { get; set; }

        [Required(ErrorMessage = "Username cannot be empty.")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Display(Name = "New password (leave blank to keep current password)")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}