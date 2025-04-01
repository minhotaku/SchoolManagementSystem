using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SchoolManagementSystem.Models
{
    public class StudentEditViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [Display(Name = "Mã Sinh viên")]
        public string StudentId { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn chương trình học.")]
        [Display(Name = "Chương trình học")]
        public string SchoolProgramId { get; set; }

        [Display(Name = "Mật khẩu mới (để trống nếu không đổi)")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        public SelectList? SchoolProgramList { get; set; }
    }
}