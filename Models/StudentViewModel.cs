using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class StudentViewModel
    {
        public string UserId { get; set; }
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Display(Name = "Mã Sinh viên")]
        public string StudentId { get; set; }
        public string SchoolProgramId { get; set; }

        [Display(Name = "Chương trình học")]
        public string SchoolProgramName { get; set; }
    }
}