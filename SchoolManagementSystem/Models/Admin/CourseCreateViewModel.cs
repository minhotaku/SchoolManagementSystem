using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SchoolManagementSystem.Models
{
    public class CourseCreateViewModel
    {

        [Required(ErrorMessage = "Course name cannot be empty.")]
        [Display(Name = "Course Name")]
        public string CourseName { get; set; }

        [Required(ErrorMessage = "Credits cannot be empty.")]
        [Range(1, 10, ErrorMessage = "Credits must be from 1 to 10.")]
        [Display(Name = "Credits")]
        public int Credits { get; set; }

        [Required(ErrorMessage = "Please select the faculty in charge.")]
        [Display(Name = "Faculty in charge")]
        public string FacultyId { get; set; }

        public SelectList? FacultyList { get; set; }
    }
}