using System;
using System.Collections.Generic;
using System.Linq;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Implementation; // Cần để dùng User Service
using SchoolManagementSystem.Services.Interfaces;   // Cần để dùng User Service
using SchoolManagementSystem.Models;

namespace SchoolManagementSystem.Controllers.AdminControllers
{
    public class FacultyManagementController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        // Thông tin người dùng hiện tại (Tạm thời)
        private readonly DateTime _currentDateTime = DateTime.UtcNow;
        private readonly string _currentUserLogin = "admin_son";

        public FacultyManagementController()
        {
            _unitOfWork = UnitOfWork.GetInstance();
        }

        // GET: /FacultyManagement/Index
        [HttpGet]
        public IActionResult Index()
        {
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' accessed Faculty Management Index.");
            try
            {
                // Lấy dữ liệu User và Faculty trực tiếp từ UnitOfWork
                var allUsers = _unitOfWork.Users.GetAll()?.ToList() ?? new List<User>();
                var allFaculties = _unitOfWork.Faculty.GetAll()?.ToList() ?? new List<Faculty>();
                System.Diagnostics.Debug.WriteLine($"Found {allUsers.Count} users, {allFaculties.Count} faculty records.");

                // Join dữ liệu
                var facultyViewModels = (
                    from user in allUsers
                    where user.Role.Equals("Faculty", StringComparison.OrdinalIgnoreCase) // Chỉ lấy user là Faculty
                    join facultyMember in allFaculties
                        on user.UserId equals facultyMember.UserId // Join bằng UserId
                    orderby user.Username
                    select new FacultyViewModel
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        FacultyId = facultyMember.FacultyId
                    }
                ).ToList();

                System.Diagnostics.Debug.WriteLine($"Generated {facultyViewModels.Count} FacultyViewModels.");

                // Debugging join (Optional but recommended)
                var facultyUserIds = allUsers.Where(u => u.Role.Equals("Faculty", StringComparison.OrdinalIgnoreCase)).Select(u => u.UserId).ToList();
                var joinedUserIds = facultyViewModels.Select(fvm => fvm.UserId).ToList();
                var missingJoinUserIds = facultyUserIds.Except(joinedUserIds).ToList();
                if (missingJoinUserIds.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] WARNING: Could not create ViewModel for faculty UserIDs: {string.Join(", ", missingJoinUserIds)}");
                    // Log chi tiết hơn nếu cần
                }

                return View("~/Views/Admin/FacultyManagement/Index.cshtml", facultyViewModels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Faculty Index: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Lỗi tải danh sách giảng viên.";
                return View("~/Views/Admin/FacultyManagement/Index.cshtml", new List<FacultyViewModel>());
            }
        }

        // GET: /FacultyManagement/Details/{facultyId}
        [HttpGet]
        public IActionResult Details(string id) // Nhận FacultyId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Mã giảng viên không hợp lệ.");
            System.Diagnostics.Debug.WriteLine($"Viewing details for Faculty ID '{id}'.");
            try
            {
                var facultyMember = _unitOfWork.Faculty.GetById(id); // Tìm Faculty bằng FacultyId
                if (facultyMember == null) { TempData["ErrorMessage"] = $"Không tìm thấy giảng viên {id}."; return RedirectToAction("Index"); }

                var user = UserManagementService.GetInstance().GetUserById(facultyMember.UserId); // Tìm User bằng UserId
                if (user == null) { TempData["ErrorMessage"] = $"Lỗi user cho giảng viên {id}."; return RedirectToAction("Index"); }

                var viewModel = new FacultyViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FacultyId = facultyMember.FacultyId
                    // Thêm các trường khác nếu có
                };
                return View("~/Views/Admin/FacultyManagement/Details.cshtml", viewModel);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error details faculty {id}: {ex.Message}"); TempData["ErrorMessage"] = "Lỗi xem chi tiết."; return RedirectToAction("Index"); }
        }

        // GET: /FacultyManagement/Edit/{facultyId}
        [HttpGet]
        public IActionResult Edit(string id) // Nhận FacultyId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Mã giảng viên không hợp lệ.");
            System.Diagnostics.Debug.WriteLine($"Loading edit page for Faculty ID '{id}'.");
            try
            {
                var facultyMember = _unitOfWork.Faculty.GetById(id); // Tìm Faculty bằng FacultyId
                if (facultyMember == null) { TempData["ErrorMessage"] = $"Không tìm thấy giảng viên {id}."; return RedirectToAction("Index"); }

                var user = UserManagementService.GetInstance().GetUserById(facultyMember.UserId); // Tìm User bằng UserId
                if (user == null) { TempData["ErrorMessage"] = $"Lỗi user cho giảng viên {id}."; return RedirectToAction("Index"); }

                var model = new FacultyEditViewModel
                {
                    UserId = user.UserId,
                    FacultyId = facultyMember.FacultyId,
                    Username = user.Username
                    // Password để trống
                };
                return View("~/Views/Admin/FacultyManagement/Edit.cshtml", model);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading edit faculty {id}: {ex.Message}"); TempData["ErrorMessage"] = "Lỗi tải trang sửa."; return RedirectToAction("Index"); }
        }

        // POST: /FacultyManagement/Edit/{facultyId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, FacultyEditViewModel model) // id là FacultyId
        {
            if (id != model.FacultyId) return BadRequest("Mã giảng viên không khớp.");
            System.Diagnostics.Debug.WriteLine($"Saving changes for Faculty ID '{id}'. ...");

            // Kiểm tra độ dài mật khẩu nếu có nhập
            const int minPasswordLength = 6;
            if (!string.IsNullOrEmpty(model.Password) && model.Password.Length < minPasswordLength) { ModelState.AddModelError(nameof(model.Password), $"Mật khẩu mới ít nhất {minPasswordLength} ký tự."); }

            if (!ModelState.IsValid) { LogModelStateErrors(id); TempData["ErrorMessage"] = "Dữ liệu không hợp lệ."; return View("~/Views/Admin/FacultyManagement/Edit.cshtml", model); }

            try
            {
                // Lấy thông tin User hiện tại (không cần lấy Faculty vì không sửa gì ở bảng Faculty)
                var existingUser = UserManagementService.GetInstance().GetUserById(model.UserId); // Dùng UserId từ model (hidden field)
                if (existingUser == null) { TempData["ErrorMessage"] = $"Lỗi: Không tìm thấy tài khoản user ID {model.UserId}."; return RedirectToAction("Index"); }
                if (!existingUser.Role.Equals("Faculty", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = "Người dùng này không phải là giảng viên."; return RedirectToAction("Index"); } // Kiểm tra lại vai trò


                bool userChanged = false;

                // Cập nhật User Entity
                if (existingUser.Username != model.Username) { existingUser.Username = model.Username; userChanged = true; }
                if (!string.IsNullOrEmpty(model.Password) && (!ModelState.ContainsKey(nameof(model.Password)) || !ModelState[nameof(model.Password)].Errors.Any())) { existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password); userChanged = true; }

                // Lưu thay đổi User nếu có
                if (userChanged)
                {
                    bool userUpdateSuccess = UserManagementService.GetInstance().UpdateUser(existingUser); // Service sẽ lo việc Save User Repo
                    if (!userUpdateSuccess) { TempData["ErrorMessage"] = "Lỗi cập nhật tài khoản giảng viên."; return View("~/Views/Admin/FacultyManagement/Edit.cshtml", model); }
                    System.Diagnostics.Debug.WriteLine($"User info updated for faculty User ID {model.UserId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No changes detected for faculty User ID {model.UserId}");
                }

                TempData["SuccessMessage"] = $"Đã cập nhật thành công giảng viên '{model.Username}'.";
                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Lỗi hệ thống khi cập nhật."; System.Diagnostics.Debug.WriteLine($"Error saving faculty {id}: {ex.Message}"); return View("~/Views/Admin/FacultyManagement/Edit.cshtml", model); }
        }

        // GET: /FacultyManagement/Delete/{userId}
        [HttpGet]
        public IActionResult Delete(string id) // Nhận UserId từ link trong Index
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("ID người dùng không hợp lệ.");
            System.Diagnostics.Debug.WriteLine($"Viewing delete confirmation for Faculty with User ID '{id}'.");
            try
            {
                var user = UserManagementService.GetInstance().GetUserById(id);
                if (user == null || !user.Role.Equals("Faculty", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = $"Không tìm thấy giảng viên với user ID {id}."; return RedirectToAction("Index"); }

                var facultyMember = _unitOfWork.Faculty.GetByUserId(id); // Lấy FacultyId để hiển thị nếu muốn
                ViewBag.FacultyIdToDelete = facultyMember?.FacultyId;

                return View("~/Views/Admin/FacultyManagement/Delete.cshtml", user); // Dùng View Delete hiển thị User
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Lỗi tải trang xóa."; System.Diagnostics.Debug.WriteLine($"Error loading delete faculty user {id}: {ex.Message}"); return RedirectToAction("Index"); }
        }

        // POST: /FacultyManagement/Delete/{userId}
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
                if (userToDelete == null || !userToDelete.Role.Equals("Faculty", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = $"Không tìm thấy giảng viên '{usernameToDelete}'."; return RedirectToAction("Index"); }

                bool result = UserManagementService.GetInstance().DeleteUser(id); // Service sẽ xóa cả User và Faculty record
                if (result) { TempData["SuccessMessage"] = $"Đã xóa giảng viên '{usernameToDelete}'."; }
                else { TempData["ErrorMessage"] = $"Lỗi xóa giảng viên '{usernameToDelete}'."; }
                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Lỗi hệ thống khi xóa."; System.Diagnostics.Debug.WriteLine($"Error deleting faculty user {id}: {ex.Message}"); return RedirectToAction("Index"); }
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