using System;
using System.Collections.Generic;
using System.Linq;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Implementation; // Dùng để lấy User
using SchoolManagementSystem.Services.Interfaces;   // Dùng để lấy User
using SchoolManagementSystem.Models;

namespace SchoolManagementSystem.Controllers.AdminControllers
{
    public class StudentManagementController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        // Thông tin người dùng hiện tại (Tạm thời)
        private readonly DateTime _currentDateTime = DateTime.UtcNow;
        private readonly string _currentUserLogin = "admin_son";

        public StudentManagementController()
        {
            _unitOfWork = UnitOfWork.GetInstance();
        }

        // GET: /StudentManagement/Index
        // Logic Index đã hoạt động đúng, giữ nguyên
        [HttpGet]
        public IActionResult Index()
        {
            // ... (Code Index đã sửa lỗi join và hoạt động đúng) ...
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' accessed Student Management Index.");
            try
            {
                var allUsers = _unitOfWork.Users.GetAll()?.ToList() ?? new List<User>();
                var allStudents = _unitOfWork.Students.GetAll()?.ToList() ?? new List<Student>();
                var allPrograms = _unitOfWork.SchoolPrograms.GetAll()?.OrderBy(p => p.SchoolProgramName).ToList() ?? new List<SchoolProgram>();

                var studentViewModels = (
                    from user in allUsers
                    where user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase)
                    join student in allStudents on user.UserId equals student.UserId
                    join program in allPrograms on student.SchoolProgramId equals program.SchoolProgramId into programGroup
                    from prog in programGroup.DefaultIfEmpty()
                    orderby user.Username
                    select new StudentViewModel
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        StudentId = student.StudentId,
                        SchoolProgramId = student.SchoolProgramId,
                        SchoolProgramName = prog?.SchoolProgramName ?? "(Chương trình không xác định)"
                    }).ToList();

                // ... (Logging và ViewBag.ProgramsList) ...
                ViewBag.ProgramsList = new SelectList(allPrograms, "SchoolProgramId", "SchoolProgramName");
                System.Diagnostics.Debug.WriteLine($"Generated {studentViewModels.Count} StudentViewModels.");


                return View("~/Views/Admin/StudentManagement/Index.cshtml", studentViewModels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Student Index: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Lỗi tải danh sách sinh viên.";
                ViewBag.ProgramsList = new SelectList(new List<SchoolProgram>());
                return View("~/Views/Admin/StudentManagement/Index.cshtml", new List<StudentViewModel>());
            }
        }

        // *** SỬA LẠI GET Details - Đảm bảo gán giá trị ViewModel ***
        // GET: /StudentManagement/Details/{studentId}
        [HttpGet]
        public IActionResult Details(string id) // Nhận StudentId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Mã sinh viên không hợp lệ.");
            System.Diagnostics.Debug.WriteLine($"Viewing details for Student ID '{id}'.");
            try
            {
                var student = _unitOfWork.Students.GetById(id);
                if (student == null) { TempData["ErrorMessage"] = $"Không tìm thấy sinh viên {id}."; return RedirectToAction("Index"); }
                var user = UserManagementService.GetInstance().GetUserById(student.UserId);
                if (user == null) { TempData["ErrorMessage"] = $"Lỗi user cho sinh viên {id}."; return RedirectToAction("Index"); }
                var program = _unitOfWork.SchoolPrograms.GetById(student.SchoolProgramId);

                // *** Đảm bảo gán đầy đủ các thuộc tính cho ViewModel ***
                var viewModel = new StudentViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    StudentId = student.StudentId,
                    SchoolProgramId = student.SchoolProgramId,
                    SchoolProgramName = program?.SchoolProgramName ?? "(Chương trình không xác định)"
                };

                return View("~/Views/Admin/StudentManagement/Details.cshtml", viewModel);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error details student {id}: {ex.Message}"); TempData["ErrorMessage"] = "Lỗi xem chi tiết."; return RedirectToAction("Index"); }
        }


        // *** SỬA LẠI GET Edit - Đảm bảo gán giá trị ViewModel ***
        // GET: /StudentManagement/Edit/{studentId}
        [HttpGet]
        public IActionResult Edit(string id) // Nhận StudentId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Mã sinh viên không hợp lệ.");
            System.Diagnostics.Debug.WriteLine($"Loading edit page for Student ID '{id}'.");
            try
            {
                var student = _unitOfWork.Students.GetById(id);
                if (student == null) { TempData["ErrorMessage"] = $"Không tìm thấy sinh viên {id}."; return RedirectToAction("Index"); }
                var user = UserManagementService.GetInstance().GetUserById(student.UserId);
                if (user == null) { TempData["ErrorMessage"] = $"Lỗi user cho sinh viên {id}."; return RedirectToAction("Index"); }

                // *** Đảm bảo gán đầy đủ các thuộc tính cho ViewModel ***
                var model = new StudentEditViewModel
                {
                    UserId = user.UserId,
                    StudentId = student.StudentId,
                    Username = user.Username,
                    SchoolProgramId = student.SchoolProgramId
                    // Password để trống
                };

                LoadSchoolProgramsList(model.SchoolProgramId); // Load dropdown programs

                return View("~/Views/Admin/StudentManagement/Edit.cshtml", model);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading edit student {id}: {ex.Message}"); TempData["ErrorMessage"] = "Lỗi tải trang sửa."; return RedirectToAction("Index"); }
        }

        // POST: /StudentManagement/Edit/{studentId}
        // Logic không đổi so với lần sửa trước
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, StudentEditViewModel model) // id là StudentId
        {
            if (id != model.StudentId) return BadRequest("Mã sinh viên không khớp.");
            System.Diagnostics.Debug.WriteLine($"Saving changes for Student ID '{id}'. ...");
            LoadSchoolProgramsList(model.SchoolProgramId);

            const int minPasswordLength = 6;
            if (!string.IsNullOrEmpty(model.Password) && model.Password.Length < minPasswordLength) { ModelState.AddModelError(nameof(model.Password), $"Mật khẩu mới ít nhất {minPasswordLength} ký tự."); }

            if (!ModelState.IsValid) { LogModelStateErrors(id); TempData["ErrorMessage"] = "Dữ liệu không hợp lệ."; return View("~/Views/Admin/StudentManagement/Edit.cshtml", model); }
            var selectedProgram = _unitOfWork.SchoolPrograms.GetById(model.SchoolProgramId); // Kiểm tra program tồn tại
            if (selectedProgram == null) { ModelState.AddModelError(nameof(model.SchoolProgramId), "Chương trình học không hợp lệ."); TempData["ErrorMessage"] = "Chương trình học không hợp lệ."; return View("~/Views/Admin/StudentManagement/Edit.cshtml", model); }

            try
            {
                var existingStudent = _unitOfWork.Students.GetById(id);
                if (existingStudent == null) { TempData["ErrorMessage"] = $"Không tìm thấy sinh viên {id}."; return RedirectToAction("Index"); }
                var existingUser = UserManagementService.GetInstance().GetUserById(existingStudent.UserId);
                if (existingUser == null) { TempData["ErrorMessage"] = $"Lỗi user cho sinh viên {id}."; return RedirectToAction("Index"); }

                bool userChanged = false; bool studentChanged = false;

                if (existingUser.Username != model.Username) { existingUser.Username = model.Username; userChanged = true; }
                if (!string.IsNullOrEmpty(model.Password) && (!ModelState.ContainsKey(nameof(model.Password)) || !ModelState[nameof(model.Password)].Errors.Any())) { existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password); userChanged = true; }
                if (existingStudent.SchoolProgramId != model.SchoolProgramId) { existingStudent.SchoolProgramId = model.SchoolProgramId; studentChanged = true; }

                if (userChanged)
                {
                    bool userUpdateSuccess = UserManagementService.GetInstance().UpdateUser(existingUser);
                    if (!userUpdateSuccess) { TempData["ErrorMessage"] = "Lỗi cập nhật tài khoản."; return View("~/Views/Admin/StudentManagement/Edit.cshtml", model); }
                }
                if (studentChanged)
                {
                    try { _unitOfWork.Students.Update(existingStudent); _unitOfWork.SaveChanges(); }
                    catch (Exception studentEx) { TempData["ErrorMessage"] = "Lỗi cập nhật chương trình học."; System.Diagnostics.Debug.WriteLine($"Error saving student info: {studentEx.Message}"); }
                }

                if (!TempData.ContainsKey("ErrorMessage")) TempData["SuccessMessage"] = $"Đã cập nhật sinh viên '{model.Username}'.";
                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Lỗi hệ thống khi cập nhật."; System.Diagnostics.Debug.WriteLine($"Error saving student: {ex.Message}"); return View("~/Views/Admin/StudentManagement/Edit.cshtml", model); }
        }


        // GET: /StudentManagement/Delete/{userId}
        // Logic không đổi so với lần sửa trước
        [HttpGet]
        public IActionResult Delete(string id) // Nhận UserId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("ID người dùng không hợp lệ.");
            System.Diagnostics.Debug.WriteLine($"Viewing delete confirmation for Student User ID '{id}'.");
            try
            {
                var user = UserManagementService.GetInstance().GetUserById(id);
                if (user == null || !user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = $"Không tìm thấy sinh viên với user ID {id}."; return RedirectToAction("Index"); }
                var student = _unitOfWork.Students.GetByUserId(id);
                ViewBag.StudentIdToDelete = student?.StudentId;
                return View("~/Views/Admin/StudentManagement/Delete.cshtml", user);
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Lỗi tải trang xóa."; System.Diagnostics.Debug.WriteLine($"Error loading delete student user {id}: {ex.Message}"); return RedirectToAction("Index"); }
        }

        // POST: /StudentManagement/Delete/{userId}
        // Logic không đổi so với lần sửa trước
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
                if (userToDelete == null || !userToDelete.Role.Equals("Student", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = $"Không tìm thấy sinh viên '{usernameToDelete}'."; return RedirectToAction("Index"); }
                bool result = UserManagementService.GetInstance().DeleteUser(id);
                if (result) { TempData["SuccessMessage"] = $"Đã xóa sinh viên '{usernameToDelete}'."; } else { TempData["ErrorMessage"] = $"Lỗi xóa sinh viên '{usernameToDelete}'."; }
                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Lỗi hệ thống khi xóa."; System.Diagnostics.Debug.WriteLine($"Error deleting student user {id}: {ex.Message}"); return RedirectToAction("Index"); }
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