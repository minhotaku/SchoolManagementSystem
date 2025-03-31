using System;
using System.Collections.Generic;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces;

namespace SchoolManagementSystem.Controllers
{
    public class UserManagementController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IUnitOfWork _unitOfWork;

        // Ngày giờ hiện tại UTC và thông tin người dùng hiện tại
        private readonly DateTime _currentDateTime = new DateTime(2025, 03, 31, 10, 28, 53);
        private readonly string _currentUserLogin = "minhotaku";

        /// <summary>
        /// Khởi tạo controller quản lý người dùng
        /// </summary>
        public UserManagementController()
        {
            _userManagementService = UserManagementService.GetInstance();
            _unitOfWork = UnitOfWork.GetInstance();

            // Set the layout for all actions in this controller
            ViewData["Layout"] = "_AdminLayout";
        }

        /// <summary>
        /// Hiển thị danh sách người dùng
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            // Ghi log hoạt động
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} accessed user management index");

            // Lấy tất cả người dùng để hiển thị
            var users = _userManagementService.GetAllUsers();
            return View(users);
        }

        /// <summary>
        /// Hiển thị form đăng ký người dùng mới
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            // Chuẩn bị danh sách chương trình học cho form đăng ký
            ViewBag.SchoolPrograms = _unitOfWork.SchoolPrograms.GetAll();

            // Ghi log hoạt động
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} accessed user registration form");

            return View();
        }

        /// <summary>
        /// Xử lý đăng ký người dùng mới
        /// </summary>
        [HttpPost]
        public IActionResult Register(string username, string password, string role, string schoolProgramId)
        {
            // Ghi log hoạt động
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} attempted to register a new user with role {role}");

            // Kiểm tra tính hợp lệ của dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin bắt buộc.";
                ViewBag.SchoolPrograms = _unitOfWork.SchoolPrograms.GetAll();
                return View();
            }

            // Chuẩn bị thông tin bổ sung cho sinh viên
            Dictionary<string, string> additionalInfo = null;
            if (role.ToLower() == "student" && !string.IsNullOrWhiteSpace(schoolProgramId))
            {
                additionalInfo = new Dictionary<string, string>
                {
                    { "SchoolProgramId", schoolProgramId }
                };
            }

            // Gọi service để đăng ký người dùng mới
            var userId = _userManagementService.RegisterUser(username, password, role, additionalInfo);
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Lỗi đăng ký người dùng. Vui lòng kiểm tra tên đăng nhập đã tồn tại hoặc thông tin không hợp lệ.";
                ViewBag.SchoolPrograms = _unitOfWork.SchoolPrograms.GetAll();
                return View();
            }

            // Hiển thị thông báo thành công
            TempData["SuccessMessage"] = $"Đã đăng ký thành công người dùng với vai trò {role}.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa thông tin người dùng
        /// </summary>
        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            // Lấy thông tin người dùng cần chỉnh sửa
            var user = _userManagementService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }

            // Ghi log hoạt động
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} accessed edit form for user ID {id}");

            return View(user);
        }

        /// <summary>
        /// Xử lý cập nhật thông tin người dùng
        /// </summary>
        [HttpPost]
        public IActionResult Edit(User user, string newPassword)
        {
            // Ghi log hoạt động
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} attempted to update user ID {user?.UserId}");

            if (user == null || string.IsNullOrWhiteSpace(user.UserId))
            {
                return NotFound();
            }

            // Lấy thông tin người dùng hiện tại
            var existingUser = _userManagementService.GetUserById(user.UserId);
            if (existingUser == null)
            {
                return NotFound();
            }

            // Cập nhật thông tin người dùng
            existingUser.Username = user.Username;

            // Cập nhật mật khẩu nếu có
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            }

            // Lưu thông tin cập nhật
            bool result = _userManagementService.UpdateUser(existingUser);
            if (!result)
            {
                TempData["ErrorMessage"] = "Lỗi cập nhật người dùng.";
                return View(user);
            }

            // Hiển thị thông báo thành công
            TempData["SuccessMessage"] = "Đã cập nhật thông tin người dùng thành công.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa người dùng
        /// </summary>
        [HttpGet]
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            // Lấy thông tin người dùng cần xóa
            var user = _userManagementService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }

            // Ghi log hoạt động
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} accessed delete confirmation for user ID {id}");

            return View(user);
        }

        /// <summary>
        /// Xử lý xóa người dùng
        /// </summary>
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(string id)
        {
            // Ghi log hoạt động
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} attempted to delete user ID {id}");

            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            // Xóa người dùng và các thông tin liên quan
            bool result = _userManagementService.DeleteUser(id);
            if (!result)
            {
                TempData["ErrorMessage"] = "Lỗi xóa người dùng.";
                return RedirectToAction("Index");
            }

            // Hiển thị thông báo thành công
            TempData["SuccessMessage"] = "Đã xóa người dùng thành công.";
            return RedirectToAction("Index");
        }
    }
}