using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SchoolManagementSystem.Models; // Đảm bảo using

namespace SchoolManagementSystem.Models
{
    public class StudentProgressViewModel
    {
        [Display(Name = "Mã SV")]
        public string StudentId { get; set; } = "";
        [Display(Name = "Tên Sinh viên")]
        public string StudentUsername { get; set; } = "";

        public Dictionary<string, List<CourseGradeDetailsViewModel>> SemesterGrades { get; set; }

        public StudentProgressViewModel()
        {
            // Khởi tạo Dictionary
            SemesterGrades = new Dictionary<string, List<CourseGradeDetailsViewModel>>();
        }
    }
}