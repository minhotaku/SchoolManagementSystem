using System.Collections.Generic;

namespace SchoolManagementSystem.Models.Student
{
    public class GradeViewModel
    {
        public string EnrollmentId { get; set; }
        public string CourseId { get; set; }
        public string CourseName { get; set; }
        public string Semester { get; set; }
        public decimal AverageScore { get; set; }
        public List<GradeComponentViewModel> Components { get; set; }
    }

    public class GradeComponentViewModel
    {
        public string Component { get; set; }
        public decimal Score { get; set; }
    }
}