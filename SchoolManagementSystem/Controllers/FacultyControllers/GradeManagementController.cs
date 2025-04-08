using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces;
using System.Linq;

namespace SchoolManagementSystem.Controllers.FacultyControllers
{
    [Route("faculty/grade-management")]  // Định nghĩa route cho GradeManagementController
    public class GradeManagementController : Controller
    {
        private readonly IFacultyService _facultyService;
        private readonly IUnitOfWork _unitOfWork;

        public GradeManagementController()
        {
            _facultyService = FacultyService.GetInstance();
            _unitOfWork = UnitOfWork.GetInstance();
        }

        [Route("")]  // Route: faculty/grade-management
        [Route("index")]  // Route: faculty/grade-management/index
        public IActionResult Index(string courseId)
        {
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "GradeManagement", new { courseId }) });
            }

            var userId = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "GradeManagement", new { courseId }) });
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
            ViewBag.CourseId = courseId;
            return View("~/Views/Faculty/GradeManagement/Index.cshtml", enrollments);
        }

        [HttpGet]
        [Route("manage-grades/{enrollmentId}")]  // Route: faculty/grade-management/manage-grades/{enrollmentId}
        public IActionResult ManageGrades(string enrollmentId)
        {
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("ManageGrades", "GradeManagement", new { enrollmentId }) });
            }

            var userId = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("ManageGrades", "GradeManagement", new { enrollmentId }) });
            }

            var facultyId = _facultyService.GetFacultyIdByUserId(userId);
            if (string.IsNullOrEmpty(facultyId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrEmpty(enrollmentId))
            {
                return NotFound();
            }

            var enrollment = _unitOfWork.Enrollments.GetById(enrollmentId);
            if (!_facultyService.CanManageCourse(facultyId, enrollment.CourseId))
            {
                return Forbid();
            }

            var grades = _facultyService.GetGradesByEnrollment(enrollmentId);
            var averageScore = _facultyService.CalculateAverageScore(enrollmentId);
            var roundedAverageScore = Math.Round(averageScore, 2);
            var classification = _facultyService.ClassifyResult(roundedAverageScore);

            ViewBag.EnrollmentId = enrollmentId;
            ViewBag.StudentId = enrollment.StudentId;
            ViewBag.CourseId = enrollment.CourseId;
            ViewBag.AverageScore = roundedAverageScore;
            ViewBag.Classification = classification;
            return View("~/Views/Faculty/GradeManagement/ManageGrades.cshtml", grades);
        }

        [HttpGet]
        [Route("add-grade/{enrollmentId}")]  // Route: faculty/grade-management/add-grade/{enrollmentId}
        public IActionResult AddGrade(string enrollmentId)
        {
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("AddGrade", "GradeManagement", new { enrollmentId }) });
            }

            var userId = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("AddGrade", "GradeManagement", new { enrollmentId }) });
            }

            var facultyId = _facultyService.GetFacultyIdByUserId(userId);
            if (string.IsNullOrEmpty(facultyId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrEmpty(enrollmentId))
            {
                return NotFound();
            }

            var enrollment = _unitOfWork.Enrollments.GetById(enrollmentId);
            if (!_facultyService.CanManageCourse(facultyId, enrollment.CourseId))
            {
                return Forbid();
            }

            var grade = new Grade { EnrollmentId = enrollmentId };
            return View("~/Views/Faculty/GradeManagement/AddGrade.cshtml", grade);
        }

        [HttpPost]
        [Route("add-grade/{enrollmentId}")]  // Sửa route để đồng bộ với GET
        public IActionResult AddGrade(string enrollmentId, Grade grade)
        {
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("AddGrade", "GradeManagement", new { enrollmentId }) });
            }

            var userId = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("AddGrade", "GradeManagement", new { enrollmentId }) });
            }

            var facultyId = _facultyService.GetFacultyIdByUserId(userId);
            if (string.IsNullOrEmpty(facultyId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ModelState.Remove("GradeId");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    System.Diagnostics.Debug.WriteLine($"ModelState Error: {error}");
                }
                return View("~/Views/Faculty/GradeManagement/AddGrade.cshtml", grade);
            }

            var enrollment = _unitOfWork.Enrollments.GetById(grade.EnrollmentId);
            if (enrollment == null)
            {
                return NotFound("Enrollment not found.");
            }

            if (!_facultyService.CanManageCourse(facultyId, enrollment.CourseId))
            {
                return Forbid();
            }

            _facultyService.AddGrade(grade);
            return RedirectToAction("ManageGrades", new { enrollmentId = grade.EnrollmentId });
        }

        [HttpGet]
        [Route("edit-grade/{id}")]  // Route: faculty/grade-management/edit-grade/{id}
        public IActionResult EditGrade(string id)
        {
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("EditGrade", "GradeManagement", new { id }) });
            }

            var userId = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("EditGrade", "GradeManagement", new { id }) });
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

            var grade = _unitOfWork.Grades.GetById(id);
            if (grade == null)
            {
                return NotFound();
            }

            var enrollment = _unitOfWork.Enrollments.GetById(grade.EnrollmentId);
            if (!_facultyService.CanManageCourse(facultyId, enrollment.CourseId))
            {
                return Forbid();
            }

            return View("~/Views/Faculty/GradeManagement/EditGrade.cshtml", grade);
        }

        [HttpPost]
        [Route("edit-grade/{id}")]  // Sửa route để đồng bộ với GET
        public IActionResult EditGrade(string id, Grade grade)
        {
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("EditGrade", "GradeManagement", new { id }) });
            }

            var userId = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("EditGrade", "GradeManagement", new { id }) });
            }

            var facultyId = _facultyService.GetFacultyIdByUserId(userId);
            if (string.IsNullOrEmpty(facultyId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (ModelState.IsValid)
            {
                var enrollment = _unitOfWork.Enrollments.GetById(grade.EnrollmentId);
                if (!_facultyService.CanManageCourse(facultyId, enrollment.CourseId))
                {
                    return Forbid();
                }

                _facultyService.UpdateGrade(grade);
                return RedirectToAction("ManageGrades", new { enrollmentId = grade.EnrollmentId });
            }

            return View("~/Views/Faculty/GradeManagement/EditGrade.cshtml", grade);
        }

        [HttpPost]
        [Route("delete-grade")]  // Route: faculty/grade-management/delete-grade (POST)
        public IActionResult DeleteGrade(string id)
        {
            var userRole = HttpContext.Session.GetString("_UserRole");
            if (userRole != "Faculty")
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("DeleteGrade", "GradeManagement", new { id }) });
            }

            var userId = HttpContext.Session.GetString("_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("DeleteGrade", "GradeManagement", new { id }) });
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

            var grade = _unitOfWork.Grades.GetById(id);
            if (grade == null)
            {
                return NotFound();
            }

            var enrollment = _unitOfWork.Enrollments.GetById(grade.EnrollmentId);
            if (!_facultyService.CanManageCourse(facultyId, enrollment.CourseId))
            {
                return Forbid();
            }

            _facultyService.DeleteGrade(id);
            return RedirectToAction("ManageGrades", new { enrollmentId = grade.EnrollmentId });
        }
    }
}