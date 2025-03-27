using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class UserEditViewModel
    {
        public string UserId { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Display(Name = "Mật khẩu mới (để trống nếu không đổi)")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự.", MinimumLength = 6)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vai trò không được để trống")]
        [Display(Name = "Vai trò")]
        public string Role { get; set; }

        // Lưu vai trò hiện tại để so sánh khi update
        public string CurrentRole { get; set; }

        [Display(Name = "Chương trình học")]
        public string SchoolProgramId { get; set; }
    }
}