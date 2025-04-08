using System;
using System.Collections.Generic;
using System.Linq;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Utils;

namespace SchoolManagementSystem.Controllers.AdminControllers
{
    [Authorize(RoleConstants.Admin)]
    public class AdminUserManagementController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IUnitOfWork _unitOfWork;

        // Current user information (Temporary)
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
                        SchoolProgramName = programInfo?.SchoolProgramName ?? (studentInfo != null ? "(Undefined Program)" : null)
                    };
                }).ToList();

                return View("~/Views/Admin/AdminUserManagement/Index.cshtml", userViewModels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error accessing Admin User Management Index: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading the user list.";
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
                TempData["ErrorMessage"] = "Error loading school program list.";
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
                TempData["ErrorMessage"] = "Invalid input data. Please check again.";
                return View("~/Views/Admin/AdminUserManagement/Create.cshtml", model);
            }

            if (model.Role.Equals("Student", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(model.SchoolProgramId))
            {
                ModelState.AddModelError(nameof(model.SchoolProgramId), "Please select a school program for the student.");
                TempData["ErrorMessage"] = "Invalid input data. Please check again.";
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
                    TempData["SuccessMessage"] = $"Successfully created user '{model.Username}' with role {model.Role}.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' successfully created user '{model.Username}' (ID: {userId}).");
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Error creating user. Username may already exist or school program is invalid.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' failed to create user '{model.Username}'. Service returned null.");
                    return View("~/Views/Admin/AdminUserManagement/Create.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error creating user '{model.Username}': {ex.Message}");
                TempData["ErrorMessage"] = "A system error occurred while creating the user.";
                return View("~/Views/Admin/AdminUserManagement/Create.cshtml", model);
            }
        }

        // GET
        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("User ID not found.");

            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' attempting to load edit page for user ID '{id}'.");

            try
            {
                var user = _userManagementService.GetUserById(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = $"User with ID {id} not found.";
                    return RedirectToAction("Index");
                }

                var model = new UserEditViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    CurrentRole = user.Role // Assign CurrentRole
                };

                bool isStudent = user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase);
                if (isStudent)
                {
                    var student = _unitOfWork.Students.GetByUserId(id);
                    model.SchoolProgramId = student?.SchoolProgramId;
                    if (student == null) System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Warning: Student record not found for user ID '{id}'.");
                }

                // Always load Programs if student or prepare empty ViewBag
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
                        TempData["ErrorMessage"] = "Error loading school program list.";
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
                TempData["ErrorMessage"] = "An error occurred while loading user information for editing.";
                return RedirectToAction("Index");
            }
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, UserEditViewModel model)
        {
            if (id != model.UserId) return BadRequest("User ID mismatch.");

            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' attempting to save changes for user ID '{id}'. Submitted Password: '{(string.IsNullOrEmpty(model.Password) ? "[Empty]" : "[Provided]")}'");

            bool isStudent = model.CurrentRole.Equals("Student", StringComparison.OrdinalIgnoreCase);

            // Reload ViewBag if Student
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



            // --- Add manual password length check IF password is provided ---
            const int minPasswordLength = 6;
            if (!string.IsNullOrEmpty(model.Password) && model.Password.Length < minPasswordLength)
            {
                // Add error to ModelState if the entered password is too short
                ModelState.AddModelError(nameof(model.Password), $"New password must be at least {minPasswordLength} characters long.");
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


            // Check overall ModelState (including manual password error if any)
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
                TempData["ErrorMessage"] = "Invalid input data. Please re-check the fields with errors.";
                return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
            }

            // Check SchoolProgramId if Student
            if (isStudent && string.IsNullOrWhiteSpace(model.SchoolProgramId))
            {
                ModelState.AddModelError(nameof(model.SchoolProgramId), "Please select a school program for the student.");
                TempData["ErrorMessage"] = "Invalid input data. Please check again.";
                return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
            }

            try
            {
                var existingUser = _userManagementService.GetUserById(id);
                if (existingUser == null)
                {
                    TempData["ErrorMessage"] = $"User with ID {id} not found for update.";
                    return RedirectToAction("Index");
                }

                existingUser.Username = model.Username;

                // Only hash and update password if provided AND passed length check
                if (!string.IsNullOrEmpty(model.Password) &&
                    (!ModelState.ContainsKey(nameof(model.Password)) || !ModelState[nameof(model.Password)].Errors.Any()))
                {
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' updated password for user ID '{id}'.");
                }

                bool userUpdateResult = _userManagementService.UpdateUser(existingUser);

                if (!userUpdateResult)
                {
                    TempData["ErrorMessage"] = "Error updating basic user information.";
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
                                    TempData["ErrorMessage"] = "Error updating school program.";
                                }
                            }
                            else
                            {
                                specificInfoUpdateResult = false;
                                TempData["ErrorMessage"] = "Selected school program is invalid.";
                                ModelState.AddModelError(nameof(model.SchoolProgramId), "Invalid school program.");
                                return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
                            }
                        }
                    }
                    else
                    {
                        specificInfoUpdateResult = false;
                        System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error: Student record not found for user ID '{id}'.");
                        TempData["ErrorMessage"] = "Error: Corresponding student record not found.";
                    }
                }

                if (specificInfoUpdateResult)
                {
                    TempData["SuccessMessage"] = $"Successfully updated user information for '{model.Username}'.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' successfully updated user ID '{id}'.");
                    return RedirectToAction("Index");
                }
                else
                {
                    // Error already set in TempData/ModelState
                    return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error saving changes for user ID '{id}': {ex.Message}");
                TempData["ErrorMessage"] = "A system error occurred while updating the user.";
                return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
            }
        }


        // GET: /AdminUserManagement/Delete/{id}
        [HttpGet]
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("User ID not found.");

            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' viewing delete confirmation for user ID '{id}'.");

            try
            {
                var user = _userManagementService.GetUserById(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = $"User with ID {id} not found for deletion.";
                    return RedirectToAction("Index");
                }
                return View("~/Views/Admin/AdminUserManagement/Delete.cshtml", user);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error loading user '{id}' for delete confirmation: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading user information for deletion.";
                return RedirectToAction("Index");
            }
        }

        // POST: /AdminUserManagement/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("Invalid User ID.");

            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' confirming deletion for user ID '{id}'.");

            try
            {
                var userToDelete = _userManagementService.GetUserById(id);
                string usernameToDelete = userToDelete?.Username ?? $"ID {id}";

                if (userToDelete == null)
                {
                    TempData["ErrorMessage"] = $"User '{usernameToDelete}' not found. May have already been deleted.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Delete failed: User ID '{id}' not found.");
                    return RedirectToAction("Index");
                }

                bool result = _userManagementService.DeleteUser(id);

                if (result)
                {
                    TempData["SuccessMessage"] = $"Successfully deleted user '{usernameToDelete}'.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Successfully deleted user ID '{id}' ('{usernameToDelete}').");
                }
                else
                {
                    TempData["ErrorMessage"] = $"Error deleting user '{usernameToDelete}'.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Delete failed for user ID '{id}'. Service returned false.");
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error deleting user ID '{id}': {ex.Message}");
                TempData["ErrorMessage"] = "A system error occurred while deleting the user.";
                return RedirectToAction("Index");
            }
        }
    }
}