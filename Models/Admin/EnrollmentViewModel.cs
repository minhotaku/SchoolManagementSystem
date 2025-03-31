using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class EnrollmentViewModel
    {
        [Display(Name = "Mã ĐK")]
        public string EnrollmentId { get; set; }

        public string StudentId { get; set; }
        [Display(Name = "Mã SV")]
        public string StudentCode { get; set; }
        [Display(Name = "Tên SV")]
        public string StudentUsername { get; set; }

        public string CourseId { get; set; }
        [Display(Name = "Mã KH")]
        public string CourseCode { get; set; }
        [Display(Name = "Tên Khóa học")]
        public string CourseName { get; set; }
        [Display(Name = "Số TC")]
        public int Credits { get; set; }

        public string FacultyUsername { get; set; }

        [Display(Name = "Học kỳ")]
        public string Semester { get; set; }

    }
}