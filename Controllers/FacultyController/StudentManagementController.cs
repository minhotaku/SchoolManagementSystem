using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces;
using System.Linq;

namespace SchoolManagementSystem.Controllers.FacultyControllers
{
    public class StudentManagementController : Controller
    {
        private readonly IFacultyService _facultyService;
        private readonly IUnitOfWork _unitOfWork;

        public StudentManagementController()
        {
            _facultyService = FacultyService.GetInstance();
            _unitOfWork = UnitOfWork.GetInstance();
        }

        public IActionResult Index(string courseId)
        {
            // Kiểm tra đăng nhập và vai trò
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "StudentManagement", new { courseId }) });
            }

            // Lấy FacultyId từ Session
            var userId = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "StudentManagement", new { courseId }) });
            }

            var facultyId = _facultyService.GetFacultyIdByUserId(userId);
            if (string.IsNullOrEmpty(facultyId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrEmpty(courseId))
            {
                return NotFound();
            }

            if (!_facultyService.CanManageCourse(facultyId, courseId))
            {
                return Forbid();
            }

            var enrollments = _facultyService.GetEnrollmentsByCourse(courseId) ?? new List<Enrollment>();
            var studentDetails = new List<(Enrollment Enrollment, Student Student, decimal AverageScore, string Classification, string SchoolProgramName)>();

            foreach (var enrollment in enrollments)
            {
                var student = _facultyService.GetStudentById(enrollment.StudentId);
                if (student != null)
                {
                    var averageScore = _facultyService.CalculateAverageScore(enrollment.EnrollmentId);
                    var classification = _facultyService.ClassifyResult(averageScore);
                    var schoolProgramName = _facultyService.GetSchoolProgramName(student.SchoolProgramId);
                    studentDetails.Add((enrollment, student, averageScore, classification, schoolProgramName));
                }
            }

            ViewBag.CourseId = courseId;
            return View(studentDetails);
        }

        [HttpGet]
        public IActionResult SendNotification(string studentId, string courseId)
        {
            // Kiểm tra đăng nhập và vai trò
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("SendNotification", "StudentManagement", new { studentId, courseId }) });
            }

            // Lấy FacultyId từ Session
            var userId = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("SendNotification", "StudentManagement", new { studentId, courseId }) });
            }

            var facultyId = _facultyService.GetFacultyIdByUserId(userId);
            if (string.IsNullOrEmpty(facultyId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(courseId))
            {
                return NotFound();
            }

            var student = _facultyService.GetStudentById(studentId);
            if (student == null)
            {
                return NotFound();
            }

            ViewBag.StudentId = studentId;
            ViewBag.UserId = student.UserId;
            ViewBag.CourseId = courseId;
            return View();
        }

        [HttpPost]
        public IActionResult SendNotification(string userId, string message, string courseId)
        {
            // Kiểm tra đăng nhập và vai trò
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("SendNotification", "StudentManagement") });
            }

            // Lấy FacultyId từ Session
            var userIdFromSession = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userIdFromSession))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("SendNotification", "StudentManagement") });
            }

            var facultyId = _facultyService.GetFacultyIdByUserId(userIdFromSession);
            if (string.IsNullOrEmpty(facultyId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(message) || string.IsNullOrEmpty(courseId))
            {
                return BadRequest("Invalid input.");
            }

            if (!_facultyService.CanManageCourse(facultyId, courseId))
            {
                return Forbid();
            }

            _facultyService.SendNotification(userId, message);
            return RedirectToAction("Index", new { courseId });
        }
    }
}