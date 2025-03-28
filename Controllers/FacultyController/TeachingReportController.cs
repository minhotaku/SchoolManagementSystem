using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces;
using System.Collections.Generic;

namespace SchoolManagementSystem.Controllers.FacultyControllers
{
    public class TeachingReportController : Controller
    {
        private readonly IFacultyService _facultyService;

        public TeachingReportController()
        {
            _facultyService = FacultyService.GetInstance();
        }

        public IActionResult Index()
        {
            // Kiểm tra đăng nhập và vai trò
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "TeachingReport") });
            }

            // Lấy FacultyId từ Session
            var userId = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "TeachingReport") });
            }

            var facultyId = _facultyService.GetFacultyIdByUserId(userId);
            if (string.IsNullOrEmpty(facultyId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var courses = _facultyService.GetCoursesByFaculty(facultyId) ?? new List<Course>();
            var reportData = new List<(Course Course, int StudentCount, decimal ClassAverage, Dictionary<string, int> ClassificationStats)>();

            foreach (var course in courses)
            {
                var enrollments = _facultyService.GetEnrollmentsByCourse(course.CourseId) ?? new List<Enrollment>();
                var (classAverage, classificationStats) = _facultyService.GetCourseStatistics(course.CourseId);
                var roundedClassAverage = Math.Round(classAverage, 2); // Làm tròn điểm trung bình tới 2 chữ số thập phân
                reportData.Add((course, enrollments.Count(), roundedClassAverage, classificationStats));
            }

            return View(reportData);
        }
    }
}