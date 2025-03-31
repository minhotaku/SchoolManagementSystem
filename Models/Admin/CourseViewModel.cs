using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class CourseViewModel
    {
        [Display(Name = "Mã Khóa học")]
        public string CourseId { get; set; }

        [Display(Name = "Tên Khóa học")]
        public string CourseName { get; set; }

        [Display(Name = "Số tín chỉ")]
        public int Credits { get; set; }

        public string FacultyId { get; set; }

        [Display(Name = "Giảng viên")]
        public string FacultyUsername { get; set; }
    }
}