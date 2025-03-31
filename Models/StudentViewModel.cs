using System.Collections.Generic;

namespace SchoolManagementSystem.Models
{
    public class StudentViewModel
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string ProgramName { get; set; }
        public List<CourseViewModel> Courses { get; set; }
        public List<GradeViewModel> RecentGrades { get; set; }
        public List<NotificationViewModel> Notifications { get; set; }
    }
}