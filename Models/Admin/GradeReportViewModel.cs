using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SchoolManagementSystem.Models
{
    public class GradeReportViewModel
    {
        public string GradeId { get; set; } = "";
        [Display(Name = "Thành phần")]
        public string Component { get; set; } = "";
        [Display(Name = "Điểm số")]
        [DisplayFormat(DataFormatString = "{0:N1}")]
        public decimal Score { get; set; }

        public string EnrollmentId { get; set; } = "";
        [Display(Name = "Học kỳ")]
        public string Semester { get; set; } = "";

        public string StudentId { get; set; } = "";
        [Display(Name = "Mã SV")]
        public string StudentCode { get; set; } = "";
        [Display(Name = "Tên SV")]
        public string StudentUsername { get; set; } = "";

        public string CourseId { get; set; } = "";
        [Display(Name = "Mã KH")]
        public string CourseCode { get; set; } = "";
        [Display(Name = "Tên Khóa học")]
        public string CourseName { get; set; } = "";

        public string FormattedScore => Score.ToString("N1", CultureInfo.InvariantCulture);
    }
}