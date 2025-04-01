using System;
using System.Collections.Generic;
using System.Linq;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Implementation; // Used to get User
using SchoolManagementSystem.Services.Interfaces;   // Used to get User
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Utils;

namespace SchoolManagementSystem.Controllers.Controllers
{
    [Authorize(RoleConstants.Admin)]
    public class StudentManagementController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        // Current user information (Temporary)
        private readonly DateTime _currentDateTime = DateTime.UtcNow;
        private readonly string _currentUserLogin = "admin_son";

        public StudentManagementController()
        {
            _unitOfWork = UnitOfWork.GetInstance();
        }

        // GET: /StudentManagement/Index
        // Index logic is working correctly, keep it as is
        [HttpGet]
        public IActionResult Index()
        {
            // ... (Index code already fixed join and working correctly) ...
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
                        SchoolProgramName = prog?.SchoolProgramName ?? "(Undefined Program)"
                    }).ToList();

                // ... (Logging and ViewBag.ProgramsList) ...
                ViewBag.ProgramsList = new SelectList(allPrograms, "SchoolProgramId", "SchoolProgramName");
                System.Diagnostics.Debug.WriteLine($"Generated {studentViewModels.Count} StudentViewModels.");


                return View("~/Views/Admin/StudentManagement/Index.cshtml", studentViewModels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Student Index: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading student list.";
                ViewBag.ProgramsList = new SelectList(new List<SchoolProgram>());
                return View("~/Views/Admin/StudentManagement/Index.cshtml", new List<StudentViewModel>());
            }
        }

        // *** REVISED GET Details - Ensure ViewModel values are assigned ***
        // GET: /StudentManagement/Details/{studentId}
        [HttpGet]
        public IActionResult Details(string id) // Receives StudentId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Invalid student ID.");
            System.Diagnostics.Debug.WriteLine($"Viewing details for Student ID '{id}'.");
            try
            {
                var student = _unitOfWork.Students.GetById(id);
                if (student == null) { TempData["ErrorMessage"] = $"Student {id} not found."; return RedirectToAction("Index"); }
                var user = UserManagementService.GetInstance().GetUserById(student.UserId);
                if (user == null) { TempData["ErrorMessage"] = $"Error getting user for student {id}."; return RedirectToAction("Index"); }
                var program = _unitOfWork.SchoolPrograms.GetById(student.SchoolProgramId);

                // *** Ensure all attributes are assigned to ViewModel ***
                var viewModel = new StudentViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    StudentId = student.StudentId,
                    SchoolProgramId = student.SchoolProgramId,
                    SchoolProgramName = program?.SchoolProgramName ?? "(Undefined Program)"
                };

                return View("~/Views/Admin/StudentManagement/Details.cshtml", viewModel);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error details student {id}: {ex.Message}"); TempData["ErrorMessage"] = "Error viewing details."; return RedirectToAction("Index"); }
        }


        // *** REVISED GET Edit - Ensure ViewModel values are assigned ***
        // GET: /StudentManagement/Edit/{studentId}
        [HttpGet]
        public IActionResult Edit(string id) // Receives StudentId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Invalid student ID.");
            System.Diagnostics.Debug.WriteLine($"Loading edit page for Student ID '{id}'.");
            try
            {
                var student = _unitOfWork.Students.GetById(id);
                if (student == null) { TempData["ErrorMessage"] = $"Student {id} not found."; return RedirectToAction("Index"); }
                var user = UserManagementService.GetInstance().GetUserById(student.UserId);
                if (user == null) { TempData["ErrorMessage"] = $"Error getting user for student {id}."; return RedirectToAction("Index"); }

                // *** Ensure all attributes are assigned to ViewModel ***
                var model = new StudentEditViewModel
                {
                    UserId = user.UserId,
                    StudentId = student.StudentId,
                    Username = user.Username,
                    SchoolProgramId = student.SchoolProgramId
                    // Password is left empty
                };

                LoadSchoolProgramsList(model.SchoolProgramId); // Load dropdown programs

                return View("~/Views/Admin/StudentManagement/Edit.cshtml", model);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading edit student {id}: {ex.Message}"); TempData["ErrorMessage"] = "Error loading edit page."; return RedirectToAction("Index"); }
        }

        // POST: /StudentManagement/Edit/{studentId}
        // Logic unchanged from previous revision
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, StudentEditViewModel model) // id is StudentId
        {
            if (id != model.StudentId) return BadRequest("Student ID mismatch.");
            System.Diagnostics.Debug.WriteLine($"Saving changes for Student ID '{id}'. ...");
            LoadSchoolProgramsList(model.SchoolProgramId);

            const int minPasswordLength = 6;
            if (!string.IsNullOrEmpty(model.Password) && model.Password.Length < minPasswordLength) { ModelState.AddModelError(nameof(model.Password), $"New password must be at least {minPasswordLength} characters."); }

            if (!ModelState.IsValid) { LogModelStateErrors(id); TempData["ErrorMessage"] = "Invalid data."; return View("~/Views/Admin/StudentManagement/Edit.cshtml", model); }
            var selectedProgram = _unitOfWork.SchoolPrograms.GetById(model.SchoolProgramId); // Check if program exists
            if (selectedProgram == null) { ModelState.AddModelError(nameof(model.SchoolProgramId), "Invalid school program."); TempData["ErrorMessage"] = "Invalid school program."; return View("~/Views/Admin/StudentManagement/Edit.cshtml", model); }

            try
            {
                var existingStudent = _unitOfWork.Students.GetById(id);
                if (existingStudent == null) { TempData["ErrorMessage"] = $"Student {id} not found."; return RedirectToAction("Index"); }
                var existingUser = UserManagementService.GetInstance().GetUserById(existingStudent.UserId);
                if (existingUser == null) { TempData["ErrorMessage"] = $"Error getting user for student {id}."; return RedirectToAction("Index"); }

                bool userChanged = false; bool studentChanged = false;

                if (existingUser.Username != model.Username) { existingUser.Username = model.Username; userChanged = true; }
                if (!string.IsNullOrEmpty(model.Password) && (!ModelState.ContainsKey(nameof(model.Password)) || !ModelState[nameof(model.Password)].Errors.Any())) { existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password); userChanged = true; }
                if (existingStudent.SchoolProgramId != model.SchoolProgramId) { existingStudent.SchoolProgramId = model.SchoolProgramId; studentChanged = true; }

                if (userChanged)
                {
                    bool userUpdateSuccess = UserManagementService.GetInstance().UpdateUser(existingUser);
                    if (!userUpdateSuccess) { TempData["ErrorMessage"] = "Error updating account."; return View("~/Views/Admin/StudentManagement/Edit.cshtml", model); }
                }
                if (studentChanged)
                {
                    try { _unitOfWork.Students.Update(existingStudent); _unitOfWork.SaveChanges(); }
                    catch (Exception studentEx) { TempData["ErrorMessage"] = "Error updating school program."; System.Diagnostics.Debug.WriteLine($"Error saving student info: {studentEx.Message}"); }
                }

                if (!TempData.ContainsKey("ErrorMessage")) TempData["SuccessMessage"] = $"Updated student '{model.Username}'.";
                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "System error during update."; System.Diagnostics.Debug.WriteLine($"Error saving student: {ex.Message}"); return View("~/Views/Admin/StudentManagement/Edit.cshtml", model); }
        }


        // GET: /StudentManagement/Delete/{userId}
        // Logic unchanged from previous revision
        [HttpGet]
        public IActionResult Delete(string id) // Receives UserId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Invalid user ID.");
            System.Diagnostics.Debug.WriteLine($"Viewing delete confirmation for Student User ID '{id}'.");
            try
            {
                var user = UserManagementService.GetInstance().GetUserById(id);
                if (user == null || !user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = $"Student with user ID {id} not found."; return RedirectToAction("Index"); }
                var student = _unitOfWork.Students.GetByUserId(id);
                ViewBag.StudentIdToDelete = student?.StudentId;
                return View("~/Views/Admin/StudentManagement/Delete.cshtml", user);
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Error loading delete page."; System.Diagnostics.Debug.WriteLine($"Error loading delete student user {id}: {ex.Message}"); return RedirectToAction("Index"); }
        }

        // POST: /StudentManagement/Delete/{userId}
        // Logic unchanged from previous revision
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id) // id is UserId
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("Invalid user ID.");
            System.Diagnostics.Debug.WriteLine($"Confirming deletion for User ID '{id}'.");
            try
            {
                var userToDelete = UserManagementService.GetInstance().GetUserById(id);
                string usernameToDelete = userToDelete?.Username ?? $"User ID {id}";
                if (userToDelete == null || !userToDelete.Role.Equals("Student", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = $"Student '{usernameToDelete}' not found."; return RedirectToAction("Index"); }
                bool result = UserManagementService.GetInstance().DeleteUser(id);
                if (result) { TempData["SuccessMessage"] = $"Deleted student '{usernameToDelete}'."; } else { TempData["ErrorMessage"] = $"Error deleting student '{usernameToDelete}'."; }
                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "System error during deletion."; System.Diagnostics.Debug.WriteLine($"Error deleting student user {id}: {ex.Message}"); return RedirectToAction("Index"); }
        }


        // --- Helper Methods ---
        private void LoadSchoolProgramsList(string? selectedProgramId = null)
        {
            try { var programs = _unitOfWork.SchoolPrograms.GetAll()?.OrderBy(p => p.SchoolProgramName).ToList() ?? new List<SchoolProgram>(); ViewBag.SchoolProgramsList = new SelectList(programs, "SchoolProgramId", "SchoolProgramName", selectedProgramId); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading programs: {ex.Message}"); ViewBag.SchoolProgramsList = new SelectList(new List<SchoolProgram>()); TempData["ErrorMessageLoading"] = "Error loading school programs."; }
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