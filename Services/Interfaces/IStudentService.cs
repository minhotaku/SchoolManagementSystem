using System.Collections.Generic;
using SchoolManagementSystem.Models;

namespace SchoolManagementSystem.Services.Interfaces
{
    public interface IStudentService
    {
        StudentViewModel GetStudentDashboard(string userId);
        List<CourseViewModel> GetStudentCourses(string studentId);
        List<GradeViewModel> GetStudentGrades(string studentId);
        List<NotificationViewModel> GetStudentNotifications(string userId);
        StudentProfileViewModel GetStudentProfile(string studentId);
    }
}