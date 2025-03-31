using System;
using System.Collections.Generic;
using System.Linq;
using BCrypt.Net; // Cần để hash password khi tạo/sửa
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Cần cho SelectList
using SchoolManagementSystem.Data; // Để lấy UnitOfWork
using SchoolManagementSystem.Entities; // Để dùng User, Student,...
using SchoolManagementSystem.Services.Implementation; // Để lấy UserManagementService instance
using SchoolManagementSystem.Services.Interfaces; // Để dùng IUserManagementService
using SchoolManagementSystem.Models; // Namespace cho ViewModels

namespace SchoolManagementSystem.Controllers.AdminControllers
{
    public class AdminUserManagementController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IUnitOfWork _unitOfWork;

        // Thông tin người dùng hiện tại (Tạm thời)
        private readonly DateTime _currentDateTime = DateTime.UtcNow;
        private readonly string _currentUserLogin = "admin_son";

        public AdminUserManagementController()
        {
            _userManagementService = UserManagementService.GetInstance();
            _unitOfWork = UnitOfWork.GetInstance();
        }

        // GET: /AdminUserManagement/Index
        [HttpGet]
        public IActionResult Index()
        {
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' accessed Admin User Management Index.");
            try
            {
                var users = _userManagementService.GetAllUsers()?.ToList() ?? new List<User>();
                var students = _unitOfWork.Students.GetAll()?.ToList() ?? new List<Student>();
                var faculties = _unitOfWork.Faculty.GetAll()?.ToList() ?? new List<Faculty>();
                var admins = _unitOfWork.Admins.GetAll()?.ToList() ?? new List<Admin>();
                var programs = _unitOfWork.SchoolPrograms.GetAll()?.ToList() ?? new List<SchoolProgram>();

                var userViewModels = users.Select(u => {
                    Student studentInfo = students.FirstOrDefault(s => s.UserId == u.UserId);
                    Faculty facultyInfo = faculties.FirstOrDefault(f => f.UserId == u.UserId);
                    Admin adminInfo = admins.FirstOrDefault(a => a.UserId == u.UserId);
                    SchoolProgram programInfo = (studentInfo != null) ? programs.FirstOrDefault(p => p.SchoolProgramId == studentInfo.SchoolProgramId) : null;

                    return new AdminUserViewModel
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        Role = u.Role,
                        StudentId = studentInfo?.StudentId,
                        FacultyId = facultyInfo?.FacultyId,
                        AdminId = adminInfo?.AdminId,
                        SchoolProgramId = studentInfo?.SchoolProgramId,
                        SchoolProgramName = programInfo?.SchoolProgramName ?? (studentInfo != null ? "(Chương trình không xác định)" : null)
                    };
                }).ToList();

                return View("~/Views/Admin/AdminUserManagement/Index.cshtml", userViewModels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error accessing Admin User Management Index: {ex.Message}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách người dùng.";
                return View("~/Views/Admin/AdminUserManagement/Index.cshtml", new List<AdminUserViewModel>());
            }
        }

        // GET: /AdminUserManagement/Create
        [HttpGet]
        public IActionResult Create()
        {
            try
            {
                var programs = _unitOfWork.SchoolPrograms.GetAll();
                ViewBag.SchoolPrograms = new SelectList(programs ?? new List<SchoolProgram>(), "SchoolProgramId", "SchoolProgramName");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error loading programs for Create User: {ex.Message}");
                ViewBag.SchoolPrograms = new SelectList(new List<SchoolProgram>());
                TempData["ErrorMessage"] = "Lỗi tải danh sách chương trình học.";
            }
            return View("~/Views/Admin/AdminUserManagement/Create.cshtml");
        }

        // POST: /AdminUserManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(UserCreateViewModel model)
        {
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' attempting to create user '{model.Username}'.");

            try
            {
                var programs = _unitOfWork.SchoolPrograms.GetAll();
                ViewBag.SchoolPrograms = new SelectList(programs ?? new List<SchoolProgram>(), "SchoolProgramId", "SchoolProgramName", model.SchoolProgramId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error reloading programs for Create User POST: {ex.Message}");
                ViewBag.SchoolPrograms = new SelectList(new List<SchoolProgram>());
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại.";
                return View("~/Views/Admin/AdminUserManagement/Create.cshtml", model);
            }

            if (model.Role.Equals("Student", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(model.SchoolProgramId))
            {
                ModelState.AddModelError(nameof(model.SchoolProgramId), "Vui lòng chọn chương trình học cho sinh viên.");
                TempData["ErrorMessage"] = "Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại.";
                return View("~/Views/Admin/AdminUserManagement/Create.cshtml", model);
            }

            try
            {
                Dictionary<string, string> additionalInfo = null;
                if (model.Role.Equals("Student", StringComparison.OrdinalIgnoreCase))
                {
                    additionalInfo = new Dictionary<string, string> { { "SchoolProgramId", model.SchoolProgramId } };
                }

                var userId = _userManagementService.RegisterUser(model.Username, model.Password, model.Role, additionalInfo);

                if (userId != null)
                {
                    TempData["SuccessMessage"] = $"Đã tạo thành công người dùng '{model.Username}' với vai trò {model.Role}.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' successfully created user '{model.Username}' (ID: {userId}).");
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi khi tạo người dùng. Tên đăng nhập có thể đã tồn tại hoặc chương trình học không hợp lệ.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' failed to create user '{model.Username}'. Service returned null.");
                    return View("~/Views/Admin/AdminUserManagement/Create.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error creating user '{model.Username}': {ex.Message}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi tạo người dùng.";
                return View("~/Views/Admin/AdminUserManagement/Create.cshtml", model);
            }
        }

        // GET
        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Không tìm thấy ID người dùng.");

            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' attempting to load edit page for user ID '{id}'.");

            try
            {
                var user = _userManagementService.GetUserById(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = $"Không tìm thấy người dùng với ID {id}.";
                    return RedirectToAction("Index");
                }

                var model = new UserEditViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    CurrentRole = user.Role // Gán CurrentRole
                };

                bool isStudent = user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase);
                if (isStudent)
                {
                    var student = _unitOfWork.Students.GetByUserId(id);
                    model.SchoolProgramId = student?.SchoolProgramId;
                    if (student == null) System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Warning: Student record not found for user ID '{id}'.");
                }

                // Luôn tải Programs nếu là student hoặc chuẩn bị sẵn ViewBag rỗng
                if (isStudent)
                {
                    try
                    {
                        var programs = _unitOfWork.SchoolPrograms.GetAll();
                        ViewBag.SchoolPrograms = new SelectList(programs ?? new List<SchoolProgram>(), "SchoolProgramId", "SchoolProgramName", model.SchoolProgramId);
                    }
                    catch (Exception progEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error loading programs for Edit User GET: {progEx.Message}");
                        ViewBag.SchoolPrograms = new SelectList(new List<SchoolProgram>());
                        TempData["ErrorMessage"] = "Lỗi tải danh sách chương trình học.";
                    }
                }
                else
                {
                    ViewBag.SchoolPrograms = new SelectList(new List<SchoolProgram>());
                }

                return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error loading user '{id}' for edit: {ex.Message}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải thông tin người dùng để chỉnh sửa.";
                return RedirectToAction("Index");
            }
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, UserEditViewModel model)
        {
            if (id != model.UserId) return BadRequest("ID người dùng không khớp.");

            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' attempting to save changes for user ID '{id}'. Submitted Password: '{(string.IsNullOrEmpty(model.Password) ? "[Empty]" : "[Provided]")}'");

            bool isStudent = model.CurrentRole.Equals("Student", StringComparison.OrdinalIgnoreCase);

            // Reload ViewBag nếu là Student
            if (isStudent)
            {
                try
                {
                    var programs = _unitOfWork.SchoolPrograms.GetAll();
                    ViewBag.SchoolPrograms = new SelectList(programs ?? new List<SchoolProgram>(), "SchoolProgramId", "SchoolProgramName", model.SchoolProgramId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error reloading programs for Edit User POST: {ex.Message}");
                    ViewBag.SchoolPrograms = new SelectList(new List<SchoolProgram>());
                }
            }
            else
            {
                ViewBag.SchoolPrograms = new SelectList(new List<SchoolProgram>());
            }



            // --- Thêm kiểm tra độ dài mật khẩu thủ công NẾU mật khẩu được cung cấp ---
            const int minPasswordLength = 6;
            if (!string.IsNullOrEmpty(model.Password) && model.Password.Length < minPasswordLength)
            {
                // Thêm lỗi vào ModelState nếu mật khẩu nhập vào quá ngắn
                ModelState.AddModelError(nameof(model.Password), $"Mật khẩu mới phải có ít nhất {minPasswordLength} ký tự.");
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Password validation failed for user ID '{id}': Too short.");
            }
            else if (!string.IsNullOrEmpty(model.Password))
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Password provided for user ID '{id}' and passed length check.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Password field empty for user ID '{id}', skipping length check.");
            }


            // Kiểm tra ModelState tổng thể (bao gồm cả lỗi mật khẩu thủ công nếu có)
            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] ModelState invalid for user ID '{id}'. Errors:");
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
                TempData["ErrorMessage"] = "Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại các trường báo lỗi.";
                return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
            }

            // Kiểm tra SchoolProgramId nếu là Student
            if (isStudent && string.IsNullOrWhiteSpace(model.SchoolProgramId))
            {
                ModelState.AddModelError(nameof(model.SchoolProgramId), "Vui lòng chọn chương trình học cho sinh viên.");
                TempData["ErrorMessage"] = "Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại.";
                return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
            }

            try
            {
                var existingUser = _userManagementService.GetUserById(id);
                if (existingUser == null)
                {
                    TempData["ErrorMessage"] = $"Không tìm thấy người dùng với ID {id} để cập nhật.";
                    return RedirectToAction("Index");
                }

                existingUser.Username = model.Username;

                // Chỉ hash và cập nhật mật khẩu nếu được cung cấp VÀ đã vượt qua kiểm tra độ dài
                if (!string.IsNullOrEmpty(model.Password) &&
                    (!ModelState.ContainsKey(nameof(model.Password)) || !ModelState[nameof(model.Password)].Errors.Any()))
                {
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' updated password for user ID '{id}'.");
                }

                bool userUpdateResult = _userManagementService.UpdateUser(existingUser);

                if (!userUpdateResult)
                {
                    TempData["ErrorMessage"] = "Lỗi khi cập nhật thông tin người dùng cơ bản.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' failed to update basic info for user ID '{id}'.");
                    return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
                }

                bool specificInfoUpdateResult = true;
                if (isStudent)
                {
                    var student = _unitOfWork.Students.GetByUserId(id);
                    if (student != null)
                    {
                        if (student.SchoolProgramId != model.SchoolProgramId)
                        {
                            var programExists = _unitOfWork.SchoolPrograms.GetById(model.SchoolProgramId) != null;
                            if (programExists)
                            {
                                student.SchoolProgramId = model.SchoolProgramId;
                                try
                                {
                                    _unitOfWork.Students.Update(student);
                                    _unitOfWork.SaveChanges();
                                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' updated SchoolProgramId for student user ID '{id}'.");
                                }
                                catch (Exception studentEx)
                                {
                                    specificInfoUpdateResult = false;
                                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error saving student info for user ID '{id}': {studentEx.Message}");
                                    TempData["ErrorMessage"] = "Lỗi khi cập nhật chương trình học.";
                                }
                            }
                            else
                            {
                                specificInfoUpdateResult = false;
                                TempData["ErrorMessage"] = "Chương trình học được chọn không hợp lệ.";
                                ModelState.AddModelError(nameof(model.SchoolProgramId), "Chương trình học không hợp lệ.");
                                return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
                            }
                        }
                    }
                    else
                    {
                        specificInfoUpdateResult = false;
                        System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error: Student record not found for user ID '{id}'.");
                        TempData["ErrorMessage"] = "Lỗi: Không tìm thấy bản ghi sinh viên tương ứng.";
                    }
                }

                if (specificInfoUpdateResult)
                {
                    TempData["SuccessMessage"] = $"Đã cập nhật thành công thông tin người dùng '{model.Username}'.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' successfully updated user ID '{id}'.");
                    return RedirectToAction("Index");
                }
                else
                {
                    // Lỗi đã được đặt trong TempData/ModelState
                    return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error saving changes for user ID '{id}': {ex.Message}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi cập nhật người dùng.";
                return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
            }
        }


        // GET: /AdminUserManagement/Delete/{id}
        [HttpGet]
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Không tìm thấy ID người dùng.");

            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' viewing delete confirmation for user ID '{id}'.");

            try
            {
                var user = _userManagementService.GetUserById(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = $"Không tìm thấy người dùng với ID {id} để xóa.";
                    return RedirectToAction("Index");
                }
                return View("~/Views/Admin/AdminUserManagement/Delete.cshtml", user);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error loading user '{id}' for delete confirmation: {ex.Message}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải thông tin người dùng để xóa.";
                return RedirectToAction("Index");
            }
        }

        // POST: /AdminUserManagement/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("ID người dùng không hợp lệ.");

            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' confirming deletion for user ID '{id}'.");

            try
            {
                var userToDelete = _userManagementService.GetUserById(id);
                string usernameToDelete = userToDelete?.Username ?? $"ID {id}";

                if (userToDelete == null)
                {
                    TempData["ErrorMessage"] = $"Không tìm thấy người dùng '{usernameToDelete}'. Có thể đã bị xóa.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Delete failed: User ID '{id}' not found.");
                    return RedirectToAction("Index");
                }

                bool result = _userManagementService.DeleteUser(id);

                if (result)
                {
                    TempData["SuccessMessage"] = $"Đã xóa thành công người dùng '{usernameToDelete}'.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Successfully deleted user ID '{id}' ('{usernameToDelete}').");
                }
                else
                {
                    TempData["ErrorMessage"] = $"Lỗi khi xóa người dùng '{usernameToDelete}'.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Delete failed for user ID '{id}'. Service returned false.");
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error deleting user ID '{id}': {ex.Message}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi xóa người dùng.";
                return RedirectToAction("Index");
            }
        }
    }
}