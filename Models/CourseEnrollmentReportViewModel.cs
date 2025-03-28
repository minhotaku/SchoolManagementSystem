using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class CourseEnrollmentReportViewModel
    {
        [Display(Name = "Mã Khóa học")]
        public string CourseId { get; set; } = "";

        [Display(Name = "Tên Khóa học")]
        public string CourseName { get; set; } = "";

        [Display(Name = "Giảng viên")]
        public string FacultyUsername { get; set; } = "";

        [Display(Name = "Số Tín chỉ")]
        public int Credits { get; set; }

        [Display(Name = "Số SV Đăng ký")]
        public int EnrollmentCount { get; set; }
    }
}