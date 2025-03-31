using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class FacultyViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Display(Name = "Mã Giảng viên")]
        public string FacultyId { get; set; }

    }
}