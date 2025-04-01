using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class EnrollmentViewModel
    {
        [Display(Name = "Enrollment ID")]
        public string EnrollmentId { get; set; }

        public string StudentId { get; set; }
        [Display(Name = "Student ID")]
        public string StudentCode { get; set; }
        [Display(Name = "Student Name")]
        public string StudentUsername { get; set; }

        public string CourseId { get; set; }
        [Display(Name = "Course ID")]
        public string CourseCode { get; set; }
        [Display(Name = "Course Name")]
        public string CourseName { get; set; }
        [Display(Name = "Credits")]
        public int Credits { get; set; }

        public string FacultyUsername { get; set; }

        [Display(Name = "Semester")]
        public string Semester { get; set; }

    }
}