using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SchoolManagementSystem.Models;

namespace SchoolManagementSystem.Models
{
    public class StudentProgressViewModel
    {
        [Display(Name = "Student ID")]
        public string StudentId { get; set; } = "";
        [Display(Name = "Student Name")]
        public string StudentUsername { get; set; } = "";

        public Dictionary<string, List<CourseGradeDetailsViewModel>> SemesterGrades { get; set; }

        public StudentProgressViewModel()
        {
            // Initialize Dictionary
            SemesterGrades = new Dictionary<string, List<CourseGradeDetailsViewModel>>();
        }
    }
}