using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class CourseViewModel
    {
        [Display(Name = "Course ID")]
        public string CourseId { get; set; }

        [Display(Name = "Course Name")]
        public string CourseName { get; set; }

        [Display(Name = "Credits")]
        public int Credits { get; set; }

        public string FacultyId { get; set; }

        [Display(Name = "Faculty")]
        public string FacultyUsername { get; set; }
    }
}