using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    // ViewModel để hiển thị thông tin người dùng trong danh sách Admin
    public class AdminUserViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Display(Name = "Vai trò")]
        public string Role { get; set; } // "Student", "Faculty", "Admin"

        // Thông tin bổ sung (có thể null)
        public string StudentId { get; set; }
        public string FacultyId { get; set; }
        public string AdminId { get; set; }

        public string SchoolProgramId { get; set; }

        [Display(Name = "Chương trình học")]
        public string SchoolProgramName { get; set; } // Lấy từ SchoolProgram repo

        // Có thể thêm các thông tin khác nếu cần: Email, Ngày tạo, Trạng thái,...
    }
}