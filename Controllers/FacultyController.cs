using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces;

namespace SchoolManagementSystem.Controllers
{
    public class FacultyController : Controller
    {
        private readonly IFacultyService _facultyService;

        public FacultyController()
        {
            _facultyService = FacultyService.GetInstance();
        }

        public IActionResult Index()
        {
            // Kiểm tra đăng nhập và vai trò
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Faculty") });
            }

            // Lấy FacultyId từ Session
            var userId = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Faculty") });
            }

            var facultyId = _facultyService.GetFacultyIdByUserId(userId);
            if (string.IsNullOrEmpty(facultyId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var courses = _facultyService.GetCoursesByFaculty(facultyId);
            return View(courses);
        }
    }
}