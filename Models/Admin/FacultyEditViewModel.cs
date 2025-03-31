using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class FacultyEditViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [Display(Name = "Mã Giảng viên")]
        public string FacultyId { get; set; } // Readonly

        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Display(Name = "Mật khẩu mới (để trống nếu không đổi)")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}