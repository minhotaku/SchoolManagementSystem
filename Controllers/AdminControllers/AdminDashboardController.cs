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
        // Lấy thông tin người dùng từ Session (Ví dụ)
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
                // Lấy các số liệu thống kê cơ bản
                int totalStudents = _unitOfWork.Students.GetAll()?.Count() ?? 0;
                int totalFaculty = _unitOfWork.Faculty.GetAll()?.Count() ?? 0;
                int totalAdmins = _unitOfWork.Admins.GetAll()?.Count() ?? 0;
                int totalCourses = _unitOfWork.Courses.GetAll()?.Count() ?? 0;
                int totalPrograms = _unitOfWork.SchoolPrograms.GetAll()?.Count() ?? 0;
                int totalEnrollments = _unitOfWork.Enrollments.GetAll()?.Count() ?? 0;

                // Tạo một ViewModel cho Dashboard (Nên làm)
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
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine($"Error loading Admin Dashboard: {ex.Message}");
                TempData["ErrorMessage"] = "Không thể tải dữ liệu Dashboard.";
                // Trả về View với ViewModel rỗng hoặc xử lý lỗi khác
                return View("~/Views/Admin/Dashboard/Index.cshtml", new AdminDashboardViewModel());
            }
        }
    }
}