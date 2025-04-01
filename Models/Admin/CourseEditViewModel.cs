using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SchoolManagementSystem.Models
{
    public class CourseEditViewModel
    {
        [Required]
        [Display(Name = "Course ID")]
        public string CourseId { get; set; }

        [Required(ErrorMessage = "Course name cannot be empty.")]
        [Display(Name = "Course Name")]
        public string CourseName { get; set; }

        [Required(ErrorMessage = "Credits cannot be empty.")]
        [Range(1, 10, ErrorMessage = "Credits must be between 1 and 10.")]
        [Display(Name = "Credits")]
        public int Credits { get; set; }

        [Required(ErrorMessage = "Please select a faculty in charge.")]
        [Display(Name = "Faculty in Charge")]
        public string FacultyId { get; set; }

        public SelectList? FacultyList { get; set; }
    }
}