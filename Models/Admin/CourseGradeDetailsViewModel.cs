using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SchoolManagementSystem.Models;

namespace SchoolManagementSystem.Models
{
    public class CourseGradeDetailsViewModel
    {
        public string CourseId { get; set; } = "";
        [Display(Name = "Mã KH")]
        public string CourseCode { get; set; } = "";
        [Display(Name = "Tên Khóa học")]
        public string CourseName { get; set; } = "";
        [Display(Name = "Số TC")]
        public int Credits { get; set; }
        public string EnrollmentId { get; set; } = "";

        public List<GradeComponentViewModel> Grades { get; set; }

        public CourseGradeDetailsViewModel()
        {
            // Khởi tạo List
            Grades = new List<GradeComponentViewModel>();
        }
    }
}