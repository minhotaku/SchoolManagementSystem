using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Models;

namespace SchoolManagementSystem.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly ISessionService _sessionService;
        protected readonly IUnitOfWork _unitOfWork;

        public BaseController()
        {
            // Initialize with HttpContextAccessor when available at runtime
            _sessionService = SessionService.GetInstance();
            _unitOfWork = UnitOfWork.GetInstance();
        }

        // Helper methods for permission checks
        protected bool IsAuthenticated => _sessionService.IsAuthenticated();
        protected bool IsAdmin => _sessionService.IsInRole(RoleConstants.Admin);
        protected bool IsFaculty => _sessionService.IsInRole(RoleConstants.Faculty);
        protected bool IsStudent => _sessionService.IsInRole(RoleConstants.Student);
        protected string CurrentUserId => _sessionService.GetUserId();
        protected string CurrentUsername => _sessionService.GetUsername();
        protected string CurrentUserRole => _sessionService.GetUserRole();

        // Additional helper methods for common permission scenarios
        protected bool CanManageUsers => IsAdmin;
        protected bool CanManageCourses => IsAdmin || IsFaculty;
        protected bool CanViewStudentRecords(string studentId)
        {
            if (IsAdmin || IsFaculty)
                return true;

            if (IsStudent)
                return _sessionService.GetUserId() == _unitOfWork.Students.GetById(studentId)?.UserId;

            return false;
        }
    }
}