using System;
using System.Collections.Generic;
using System.Linq;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces; 
using SchoolManagementSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using SchoolManagementSystem.Utils;

namespace SchoolManagementSystem.Controllers.AdminControllers
{
    [Authorize(RoleConstants.Admin)]
    public class AdminManagementController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        // Thông tin người dùng hiện tại (Tạm thời)
        private readonly DateTime _currentDateTime = DateTime.UtcNow;
        private readonly string _currentUserLogin = "admin_son";

        public AdminManagementController()
        {
            _unitOfWork = UnitOfWork.GetInstance();
        }

        // GET: /AdminManagement/Index
        [HttpGet]
        public IActionResult Index()
        {
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' accessed Admin Account Management Index.");
            try
            {
                var allUsers = _unitOfWork.Users.GetAll()?.ToList() ?? new List<User>();
                var allAdmins = _unitOfWork.Admins.GetAll()?.ToList() ?? new List<Admin>();
                System.Diagnostics.Debug.WriteLine($"Found {allUsers.Count} users, {allAdmins.Count} admin records.");

                var adminViewModels = (
                    from user in allUsers
                    where user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) // Chỉ lấy user là Admin
                    join adminRecord in allAdmins
                        on user.UserId equals adminRecord.UserId // Join bằng UserId
                    orderby user.Username
                    select new AdminViewModel
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        AdminId = adminRecord.AdminId
                    }
                ).ToList();

                System.Diagnostics.Debug.WriteLine($"Generated {adminViewModels.Count} AdminViewModels.");

                // Optional Debugging for join
                var adminUserIds = allUsers.Where(u => u.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)).Select(u => u.UserId).ToList();
                var joinedUserIds = adminViewModels.Select(fvm => fvm.UserId).ToList();
                var missingJoinUserIds = adminUserIds.Except(joinedUserIds).ToList();
                if (missingJoinUserIds.Any()) { System.Diagnostics.Debug.WriteLine($"WARNING: Could not create ViewModel for admin UserIDs: {string.Join(", ", missingJoinUserIds)}"); }

                return View("~/Views/Admin/AdminManagement/Index.cshtml", adminViewModels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Admin Index: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Lỗi tải danh sách quản trị viên.";
                return View("~/Views/Admin/AdminManagement/Index.cshtml", new List<AdminViewModel>());
            }
        }

        // GET: /AdminManagement/Details/{adminId}
        [HttpGet]
        public IActionResult Details(string id) // Nhận AdminId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Mã admin không hợp lệ.");
            System.Diagnostics.Debug.WriteLine($"Viewing details for Admin ID '{id}'.");
            try
            {
                var adminRecord = _unitOfWork.Admins.GetById(id); // Tìm Admin bằng AdminId
                if (adminRecord == null) { TempData["ErrorMessage"] = $"Không tìm thấy admin {id}."; return RedirectToAction("Index"); }

                var user = UserManagementService.GetInstance().GetUserById(adminRecord.UserId); // Tìm User bằng UserId
                if (user == null) { TempData["ErrorMessage"] = $"Lỗi user cho admin {id}."; return RedirectToAction("Index"); }

                var viewModel = new AdminViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    AdminId = adminRecord.AdminId
                };
                return View("~/Views/Admin/AdminManagement/Details.cshtml", viewModel);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error details admin {id}: {ex.Message}"); TempData["ErrorMessage"] = "Lỗi xem chi tiết."; return RedirectToAction("Index"); }
        }

        // GET: /AdminManagement/Edit/{adminId}
        [HttpGet]
        public IActionResult Edit(string id) // Nhận AdminId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Mã admin không hợp lệ.");
            System.Diagnostics.Debug.WriteLine($"Loading edit page for Admin ID '{id}'.");
            try
            {
                var adminRecord = _unitOfWork.Admins.GetById(id); // Tìm Admin bằng AdminId
                if (adminRecord == null) { TempData["ErrorMessage"] = $"Không tìm thấy admin {id}."; return RedirectToAction("Index"); }

                var user = UserManagementService.GetInstance().GetUserById(adminRecord.UserId); // Tìm User bằng UserId
                if (user == null) { TempData["ErrorMessage"] = $"Lỗi user cho admin {id}."; return RedirectToAction("Index"); }

                var model = new AdminEditViewModel
                {
                    UserId = user.UserId,
                    AdminId = adminRecord.AdminId,
                    Username = user.Username
                    // Password để trống
                };
                return View("~/Views/Admin/AdminManagement/Edit.cshtml", model);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading edit admin {id}: {ex.Message}"); TempData["ErrorMessage"] = "Lỗi tải trang sửa."; return RedirectToAction("Index"); }
        }

        // POST: /AdminManagement/Edit/{adminId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, AdminEditViewModel model) // id là AdminId
        {
            if (id != model.AdminId) return BadRequest("Mã admin không khớp.");
            System.Diagnostics.Debug.WriteLine($"Saving changes for Admin ID '{id}'. ...");

            const int minPasswordLength = 6;
            if (!string.IsNullOrEmpty(model.Password) && model.Password.Length < minPasswordLength) { ModelState.AddModelError(nameof(model.Password), $"Mật khẩu mới ít nhất {minPasswordLength} ký tự."); }

            if (!ModelState.IsValid) { LogModelStateErrors(id); TempData["ErrorMessage"] = "Dữ liệu không hợp lệ."; return View("~/Views/Admin/AdminManagement/Edit.cshtml", model); }

            try
            {
                var existingUser = UserManagementService.GetInstance().GetUserById(model.UserId);
                if (existingUser == null) { TempData["ErrorMessage"] = $"Lỗi: Không tìm thấy tài khoản user ID {model.UserId}."; return RedirectToAction("Index"); }
                if (!existingUser.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = "Người dùng này không phải là admin."; return RedirectToAction("Index"); }

                bool userChanged = false;

                if (existingUser.Username != model.Username) { existingUser.Username = model.Username; userChanged = true; }
                if (!string.IsNullOrEmpty(model.Password) && (!ModelState.ContainsKey(nameof(model.Password)) || !ModelState[nameof(model.Password)].Errors.Any())) { existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password); userChanged = true; }

                if (userChanged)
                {
                    bool userUpdateSuccess = UserManagementService.GetInstance().UpdateUser(existingUser);
                    if (!userUpdateSuccess) { TempData["ErrorMessage"] = "Lỗi cập nhật tài khoản admin."; return View("~/Views/Admin/AdminManagement/Edit.cshtml", model); }
                    System.Diagnostics.Debug.WriteLine($"User info updated for admin User ID {model.UserId}");
                }
                else { System.Diagnostics.Debug.WriteLine($"No changes detected for admin User ID {model.UserId}"); }

                TempData["SuccessMessage"] = $"Đã cập nhật thành công admin '{model.Username}'.";
                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Lỗi hệ thống khi cập nhật."; System.Diagnostics.Debug.WriteLine($"Error saving admin {id}: {ex.Message}"); return View("~/Views/Admin/AdminManagement/Edit.cshtml", model); }
        }

        // GET: /AdminManagement/Delete/{userId}
        [HttpGet]
        public IActionResult Delete(string id) // Nhận UserId từ link trong Index
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("ID người dùng không hợp lệ.");
            System.Diagnostics.Debug.WriteLine($"Viewing delete confirmation for Admin with User ID '{id}'.");
            try
            {
                var user = UserManagementService.GetInstance().GetUserById(id);
                if (user == null || !user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = $"Không tìm thấy admin với user ID {id}."; return RedirectToAction("Index"); }

                var adminRecord = _unitOfWork.Admins.GetByUserId(id);
                ViewBag.AdminIdToDelete = adminRecord?.AdminId;

                return View("~/Views/Admin/AdminManagement/Delete.cshtml", user);
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Lỗi tải trang xóa."; System.Diagnostics.Debug.WriteLine($"Error loading delete admin user {id}: {ex.Message}"); return RedirectToAction("Index"); }
        }

        // POST: /AdminManagement/Delete/{userId}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id) // id là UserId
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("ID người dùng không hợp lệ.");
            System.Diagnostics.Debug.WriteLine($"Confirming deletion for User ID '{id}'.");
            try
            {
                var userToDelete = UserManagementService.GetInstance().GetUserById(id);
                string usernameToDelete = userToDelete?.Username ?? $"User ID {id}";
                if (userToDelete == null || !userToDelete.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = $"Không tìm thấy admin '{usernameToDelete}'."; return RedirectToAction("Index"); }

                var allAdmins = _unitOfWork.Admins.GetAll();
                if (allAdmins.Count() <= 1)
                {
                    TempData["ErrorMessage"] = "Không thể xóa tài khoản quản trị viên cuối cùng.";
                    return RedirectToAction("Index");
                }
                if (userToDelete.UserId == HttpContext.Session.GetString("_UserId"))
                { // Giả sử SessionKeyUserId là "_UserId"
                    TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản của mình.";
                    return RedirectToAction("Index");
                }

                bool result = UserManagementService.GetInstance().DeleteUser(id); // Service sẽ xóa cả User và Admin record
                if (result) { TempData["SuccessMessage"] = $"Đã xóa admin '{usernameToDelete}'."; }
                else { TempData["ErrorMessage"] = $"Lỗi xóa admin '{usernameToDelete}'."; }
                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Lỗi hệ thống khi xóa."; System.Diagnostics.Debug.WriteLine($"Error deleting admin user {id}: {ex.Message}"); return RedirectToAction("Index"); }
        }

        // --- Helper Methods ---
        private void LoadSchoolProgramsList(string? selectedProgramId = null)
        {
            try { var programs = _unitOfWork.SchoolPrograms.GetAll()?.OrderBy(p => p.SchoolProgramName).ToList() ?? new List<SchoolProgram>(); ViewBag.SchoolProgramsList = new SelectList(programs, "SchoolProgramId", "SchoolProgramName", selectedProgramId); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading programs: {ex.Message}"); ViewBag.SchoolProgramsList = new SelectList(new List<SchoolProgram>()); TempData["ErrorMessageLoading"] = "Lỗi tải chương trình học."; }
        }
        private void LogModelStateErrors(string contextId)
        {
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] ModelState invalid for ID '{contextId}'. Errors:");
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                if (state.Errors.Any())
                {
                    foreach (var error in state.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - Key: {key}, Error: {error.ErrorMessage}");
                    }
                }
            }
        }
    }
}