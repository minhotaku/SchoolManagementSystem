using Microsoft.AspNetCore.Mvc; // Import the MVC framework.
using SchoolManagementSystem.Data; // Import the data access layer.
using SchoolManagementSystem.Models; // Import the models.
using System.Linq; // Provides support for queries that operate on objects that implement IEnumerable<T>.
using SchoolManagementSystem.Utils; // Import utility classes.

namespace SchoolManagementSystem.Controllers.AdminControllers
{
    /// <summary>
    /// Controller for the Admin Dashboard.  Handles requests related to the admin dashboard functionality.
    /// </summary>
    [Authorize(RoleConstants.Admin)] // Authorize attribute to restrict access to users with the "Admin" role.
    public class AdminDashboardController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Constructor to initialize the controller with necessary dependencies.  
        /// Currently, it initializes the UnitOfWork using the singleton pattern.
        /// </summary>
        public AdminDashboardController()
        {
            _unitOfWork = UnitOfWork.GetInstance(); // Get the singleton instance of UnitOfWork.
        }

        // GET: /AdminDashboard/Index
        /// <summary>
        /// Handles the request to display the admin dashboard.
        /// Retrieves statistical data from the database and passes it to the view.
        /// </summary>
        /// <returns>The Admin Dashboard View populated with statistical data. If an error occurs, returns the view with an empty model and an error message.</returns>
        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                // Retrieve basic statistical data from the database using UnitOfWork
                // Get the total of each data. If GetAll() returns null, default to 0.
                int totalStudents = _unitOfWork.Students.GetAll()?.Count() ?? 0;
                int totalFaculty = _unitOfWork.Faculty.GetAll()?.Count() ?? 0;
                int totalAdmins = _unitOfWork.Admins.GetAll()?.Count() ?? 0;
                int totalCourses = _unitOfWork.Courses.GetAll()?.Count() ?? 0;
                int totalPrograms = _unitOfWork.SchoolPrograms.GetAll()?.Count() ?? 0;
                int totalEnrollments = _unitOfWork.Enrollments.GetAll()?.Count() ?? 0;

                // Create a ViewModel for the Dashboard to pass data to the view.
                var viewModel = new AdminDashboardViewModel
                {
                    TotalStudents = totalStudents,
                    TotalFaculty = totalFaculty,
                    TotalAdmins = totalAdmins,
                    TotalCourses = totalCourses,
                    TotalPrograms = totalPrograms,
                    TotalEnrollments = totalEnrollments,
                };

                // Return the View with the populated ViewModel
                return View("~/Views/Admin/Dashboard/Index.cshtml", viewModel);
            }
            catch (System.Exception ex)
            {
                // Log the error to the debug output for troubleshooting.
                System.Diagnostics.Debug.WriteLine($"Error loading Admin Dashboard: {ex.Message}");

                // Set an error message to be displayed to the user.
                TempData["ErrorMessage"] = "Unable to load Dashboard data.";

                // Return the View with an empty ViewModel or handle the error differently
                return View("~/Views/Admin/Dashboard/Index.cshtml", new AdminDashboardViewModel());
            }
        }
    }
}