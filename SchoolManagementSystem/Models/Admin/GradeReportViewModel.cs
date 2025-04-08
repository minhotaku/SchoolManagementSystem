using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SchoolManagementSystem.Models
{
    public class GradeReportViewModel
    {
        public string GradeId { get; set; } = "";
        [Display(Name = "Component")]
        public string Component { get; set; } = "";
        [Display(Name = "Score")]
        [DisplayFormat(DataFormatString = "{0:N1}")]
        public decimal Score { get; set; }

        public string EnrollmentId { get; set; } = "";
        [Display(Name = "Semester")]
        public string Semester { get; set; } = "";

        public string StudentId { get; set; } = "";
        [Display(Name = "Student ID")]
        public string StudentCode { get; set; } = "";
        [Display(Name = "Student Name")]
        public string StudentUsername { get; set; } = "";

        public string CourseId { get; set; } = "";
        [Display(Name = "Course ID")]
        public string CourseCode { get; set; } = "";
        [Display(Name = "Course Name")]
        public string CourseName { get; set; } = "";

        public string FormattedScore => Score.ToString("N1", CultureInfo.InvariantCulture);
    }
}