using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SchoolManagementSystem.Models
{
    public class EnrollmentCreateViewModel
    {
        [Required(ErrorMessage = "Please select a student.")]
        [Display(Name = "Student")]
        public string StudentId { get; set; }

        [Required(ErrorMessage = "Please select a course.")]
        [Display(Name = "Course")]
        public string CourseId { get; set; }

        [Required(ErrorMessage = "Please enter a semester.")]
        [Display(Name = "Semester")]
        [RegularExpression(@"^(Fall|Spring|Summer)\s\d{4}$", ErrorMessage = "Invalid semester format (e.g., Fall 2024, Spring 2025).")]
        public string Semester { get; set; }

        public SelectList? StudentList { get; set; }
        public SelectList? CourseList { get; set; }
    }
}