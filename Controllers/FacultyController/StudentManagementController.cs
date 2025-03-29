﻿using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces;
using System.Linq;
using System.IO;

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

        // Hàm đọc Username từ users.csv dựa trên UserId
        private string GetUsernameFromUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return "N/A"; // Trả về "N/A" nếu userId rỗng
            }

            // Sửa đường dẫn file users.csv để khớp với vị trí thực tế
            string csvFilePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "CSV", "users.csv");
            System.Diagnostics.Debug.WriteLine($"Looking for users.csv at: {csvFilePath}"); // Ghi log đường dẫn file

            if (!System.IO.File.Exists(csvFilePath))
            {
                System.Diagnostics.Debug.WriteLine("users.csv not found!"); // Ghi log nếu file không tồn tại
                return "N/A";
            }

            var lines = System.IO.File.ReadAllLines(csvFilePath);
            foreach (var line in lines.Skip(1)) // Bỏ qua dòng tiêu đề
            {
                if (string.IsNullOrWhiteSpace(line)) // Kiểm tra dòng rỗng
                {
                    continue;
                }

                var columns = line.Split(',');
                if (columns.Length >= 2 && columns[0].Trim() == userId.Trim()) // So sánh UserId
                {
                    System.Diagnostics.Debug.WriteLine($"Found Username: {columns[1].Trim()} for UserId: {userId}"); // Ghi log khi tìm thấy
                    return columns[1].Trim(); // Trả về Username
                }
            }

            System.Diagnostics.Debug.WriteLine($"UserId {userId} not found in users.csv!"); // Ghi log nếu không tìm thấy
            return "N/A"; // Trả về "N/A" nếu không tìm thấy UserId
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
            var studentDetails = new List<(Enrollment Enrollment, Student Student, decimal AverageScore, string Classification, string SchoolProgramName, string Username)>();

            foreach (var enrollment in enrollments)
            {
                var student = _facultyService.GetStudentById(enrollment.StudentId);
                if (student != null)
                {
                    var averageScore = _facultyService.CalculateAverageScore(enrollment.EnrollmentId);
                    var roundedAverageScore = Math.Round(averageScore, 2); // Làm tròn điểm trung bình tới 2 chữ số thập phân
                    var classification = _facultyService.ClassifyResult(roundedAverageScore);
                    var schoolProgramName = _facultyService.GetSchoolProgramName(student.SchoolProgramId);
                    var username = GetUsernameFromUserId(student.UserId); // Lấy Username từ UserId
                    studentDetails.Add((enrollment, student, roundedAverageScore, classification, schoolProgramName, username));
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