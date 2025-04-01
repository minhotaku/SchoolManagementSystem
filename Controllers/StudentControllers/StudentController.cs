using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Utils;

namespace SchoolManagementSystem.Controllers.StudentControllers
{
    [Authorize(RoleConstants.Student)]
    public class StudentController : Controller
    {
        private readonly IStudentService _studentService;
        
        // Session key constants
        private const string SessionKeyUserId = "_UserId";
        private const string SessionKeyUsername = "_Username";
        private const string SessionKeyUserRole = "_UserRole";

        public StudentController()
        {
            _studentService = StudentService.GetInstance();
        }

        // GET: Student/Dashboard
        public IActionResult Dashboard()
        {
            try
            {
                var userId = HttpContext.Session.GetString(SessionKeyUserId);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account");
                }

                var viewModel = _studentService.GetStudentDashboard(userId);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Student/Courses
        public IActionResult Courses()
        {
            try
            {
                var userId = HttpContext.Session.GetString(SessionKeyUserId);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account");
                }

                var student = _studentService.GetStudentDashboard(userId);
                var courses = _studentService.GetStudentCourses(student.StudentId);
                
                return View(courses);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Student/CourseDetail/{courseId}
        public IActionResult CourseDetail(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Mã khóa học không hợp lệ.";
                    return RedirectToAction("Courses");
                }

                var userId = HttpContext.Session.GetString(SessionKeyUserId);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account");
                }

                var student = _studentService.GetStudentDashboard(userId);
                var courses = _studentService.GetStudentCourses(student.StudentId);
                var course = courses.FirstOrDefault(c => c.CourseId == id);

                if (course == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin khóa học.";
                    return RedirectToAction("Courses");
                }

                return View(course);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Student/Grades
        public IActionResult Grades()
        {
            try
            {
                var userId = HttpContext.Session.GetString(SessionKeyUserId);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account");
                }

                var student = _studentService.GetStudentDashboard(userId);
                var grades = _studentService.GetStudentGrades(student.StudentId);
                
                return View(grades);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Student/GradeDetail/{enrollmentId}
        public IActionResult GradeDetail(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Mã đăng ký không hợp lệ.";
                    return RedirectToAction("Grades");
                }

                var userId = HttpContext.Session.GetString(SessionKeyUserId);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account");
                }

                var student = _studentService.GetStudentDashboard(userId);
                var grades = _studentService.GetStudentGrades(student.StudentId);
                var grade = grades.FirstOrDefault(g => g.EnrollmentId == id);

                if (grade == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin điểm số.";
                    return RedirectToAction("Grades");
                }

                return View(grade);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Student/Notifications
        public IActionResult Notifications()
        {
            try
            {
                var userId = HttpContext.Session.GetString(SessionKeyUserId);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account");
                }

                var notifications = _studentService.GetStudentNotifications(userId);
                return View(notifications);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: Student/Profile
        public IActionResult Profile()
        {
            try
            {
                var userId = HttpContext.Session.GetString(SessionKeyUserId);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account");
                }

                var student = _studentService.GetStudentDashboard(userId);
                var profile = _studentService.GetStudentProfile(student.StudentId);
                
                return View(profile);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }
    }
}