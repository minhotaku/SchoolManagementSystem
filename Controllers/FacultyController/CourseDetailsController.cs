using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces;

namespace SchoolManagementSystem.Controllers.FacultyControllers
{
    public class CourseDetailsController : Controller
    {
        private readonly IFacultyService _facultyService;

        public CourseDetailsController()
        {
            _facultyService = FacultyService.GetInstance();
        }

        public IActionResult Details(string id)
        {
            // Kiểm tra đăng nhập và vai trò
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", "CourseDetails", new { id }) });
            }

            // Lấy FacultyId từ Session
            var userId = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", "CourseDetails", new { id }) });
            }

            var facultyId = _facultyService.GetFacultyIdByUserId(userId);
            if (string.IsNullOrEmpty(facultyId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var course = _facultyService.GetCourseDetails(id);
            if (course == null)
            {
                return NotFound();
            }

            if (!_facultyService.CanManageCourse(facultyId, id))
            {
                return Forbid();
            }

            return View("~/Views/Faculty/CourseDetails/Details.cshtml", course);  // Chỉ định đường dẫn view
        }
    }
}