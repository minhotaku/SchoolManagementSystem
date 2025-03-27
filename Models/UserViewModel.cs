using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class UserViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Display(Name = "Vai trò")]
        public string Role { get; set; }

        [Display(Name = "Chương trình học")]
        public string SchoolProgramName { get; set; }

        [Display(Name = "Ngày tạo")]
        public string CreatedDate { get; set; }

        [Display(Name = "Người tạo")]
        public string CreatedBy { get; set; }
    }
}