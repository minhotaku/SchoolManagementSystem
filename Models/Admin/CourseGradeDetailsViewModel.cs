using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SchoolManagementSystem.Models;

namespace SchoolManagementSystem.Models
{
    public class CourseGradeDetailsViewModel
    {
        public string CourseId { get; set; } = "";
        [Display(Name = "Course Code")]
        public string CourseCode { get; set; } = "";
        [Display(Name = "Course Name")]
        public string CourseName { get; set; } = "";
        [Display(Name = "Credits")]
        public int Credits { get; set; }
        public string EnrollmentId { get; set; } = "";

        public List<GradeComponentViewModel> Grades { get; set; }

        public CourseGradeDetailsViewModel()
        {
            Grades = new List<GradeComponentViewModel>();
        }
    }
}