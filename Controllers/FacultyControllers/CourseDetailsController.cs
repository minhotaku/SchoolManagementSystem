using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces;

namespace SchoolManagementSystem.Controllers.FacultyControllers
{
    [Route("faculty/course-details")]  // Định nghĩa route cho CourseDetailsController
    public class CourseDetailsController : Controller
    {
        private readonly IFacultyService _facultyService;

        public CourseDetailsController()
        {
            _facultyService = FacultyService.GetInstance();
        }

        [Route("{id}")]  // Route: faculty/course-details/{id}
        public IActionResult Details(string id)
        {
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", "CourseDetails", new { id }) });
            }

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

            return View("~/Views/Facultys/CourseDetails/Details.cshtml", course);
        }
    }
}