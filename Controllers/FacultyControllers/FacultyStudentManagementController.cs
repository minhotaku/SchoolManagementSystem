using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces;
using System.Linq;
using System.IO;

namespace SchoolManagementSystem.Controllers.FacultyControllers
{
    [Route("faculty/student-management", Name = "FacultyStudentManagement")]  // Giữ route như cũ
    public class FacultyStudentManagementController : Controller
    {
        private readonly IFacultyService _facultyService;
        private readonly IUnitOfWork _unitOfWork;

        public FacultyStudentManagementController()
        {
            _facultyService = FacultyService.GetInstance();
            _unitOfWork = UnitOfWork.GetInstance();
        }

        private string GetUsernameFromUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return "N/A";
            }

            string csvFilePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "CSV", "users.csv");
            System.Diagnostics.Debug.WriteLine($"Looking for users.csv at: {csvFilePath}");

            if (!System.IO.File.Exists(csvFilePath))
            {
                System.Diagnostics.Debug.WriteLine("users.csv not found!");
                return "N/A";
            }

            var lines = System.IO.File.ReadAllLines(csvFilePath);
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var columns = line.Split(',');
                if (columns.Length >= 2 && columns[0].Trim() == userId.Trim())
                {
                    System.Diagnostics.Debug.WriteLine($"Found Username: {columns[1].Trim()} for UserId: {userId}");
                    return columns[1].Trim();
                }
            }

            System.Diagnostics.Debug.WriteLine($"UserId {userId} not found in users.csv!");
            return "N/A";
        }

        [Route("")]  // Route: faculty/student-management
        [Route("index")]  // Route: faculty/student-management/index
        public IActionResult Index(string courseId)
        {
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "FacultyStudentManagement", new { courseId, area = "Faculty" }) });
            }

            var userId = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "FacultyStudentManagement", new { courseId, area = "Faculty" }) });
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
                    var roundedAverageScore = Math.Round(averageScore, 2);
                    var classification = _facultyService.ClassifyResult(roundedAverageScore);
                    var schoolProgramName = _facultyService.GetSchoolProgramName(student.SchoolProgramId);
                    var username = GetUsernameFromUserId(student.UserId);
                    studentDetails.Add((enrollment, student, roundedAverageScore, classification, schoolProgramName, username));
                }
            }

            ViewBag.CourseId = courseId;
            return View("~/Views/Faculty/FacultyStudentManagement/Index.cshtml", studentDetails);
        }

        [HttpGet]
        [Route("send-notification/{studentId}/{courseId}")]  // Route: faculty/student-management/send-notification/{studentId}/{courseId}
        public IActionResult SendNotification(string studentId, string courseId)
        {
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("SendNotification", "FacultyStudentManagement", new { studentId, courseId, area = "Faculty" }) });
            }

            var userId = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("SendNotification", "FacultyStudentManagement", new { studentId, courseId, area = "Faculty" }) });
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
            return View("~/Views/Faculty/FacultyStudentManagement/SendNotification.cshtml");
        }

        [HttpPost]
        [Route("send-notification/{studentId}/{courseId}")]  // Sửa route để đồng bộ với GET
        public IActionResult SendNotification(string studentId, string courseId, string userId, string message)
        {
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("SendNotification", "FacultyStudentManagement", new { studentId, courseId, area = "Faculty" }) });
            }

            var userIdFromSession = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userIdFromSession))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("SendNotification", "FacultyStudentManagement", new { studentId, courseId, area = "Faculty" }) });
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
            return RedirectToAction("Index", new { courseId, area = "Faculty" });
        }
    }
}