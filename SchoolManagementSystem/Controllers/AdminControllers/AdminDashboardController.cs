using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Models;
using System.Linq;
using SchoolManagementSystem.Utils;

namespace SchoolManagementSystem.Controllers.AdminControllers
{
    [Authorize(RoleConstants.Admin)]
    public class AdminDashboardController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        // Retrieve user information from Session (Example)
        // private string? CurrentAdminUsername => HttpContext.Session.GetString("_Username");

        public AdminDashboardController()
        {
            _unitOfWork = UnitOfWork.GetInstance();
        }

        // GET: /AdminDashboard/Index
        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                // Retrieve basic statistical data
                int totalStudents = _unitOfWork.Students.GetAll()?.Count() ?? 0;
                int totalFaculty = _unitOfWork.Faculty.GetAll()?.Count() ?? 0;
                int totalAdmins = _unitOfWork.Admins.GetAll()?.Count() ?? 0;
                int totalCourses = _unitOfWork.Courses.GetAll()?.Count() ?? 0;
                int totalPrograms = _unitOfWork.SchoolPrograms.GetAll()?.Count() ?? 0;
                int totalEnrollments = _unitOfWork.Enrollments.GetAll()?.Count() ?? 0;

                // Create a ViewModel for the Dashboard (Recommended)
                var viewModel = new AdminDashboardViewModel
                {
                    TotalStudents = totalStudents,
                    TotalFaculty = totalFaculty,
                    TotalAdmins = totalAdmins,
                    TotalCourses = totalCourses,
                    TotalPrograms = totalPrograms,
                    TotalEnrollments = totalEnrollments,
                };

                // ViewBag.TotalStudents = totalStudents;
                // ViewBag.TotalFaculty = totalFaculty;
                // ...

                return View("~/Views/Admin/Dashboard/Index.cshtml", viewModel);
            }
            catch (System.Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error loading Admin Dashboard: {ex.Message}");
                TempData["ErrorMessage"] = "Unable to load Dashboard data.";
                // Return the View with an empty ViewModel or handle the error differently
                return View("~/Views/Admin/Dashboard/Index.cshtml", new AdminDashboardViewModel());
            }
        }
    }
}
