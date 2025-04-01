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
    public class FacultyManagementController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        // Current user information (Temporary)
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
                // Get User and Faculty data directly from UnitOfWork
                var allUsers = _unitOfWork.Users.GetAll()?.ToList() ?? new List<User>();
                var allFaculties = _unitOfWork.Faculty.GetAll()?.ToList() ?? new List<Faculty>();
                System.Diagnostics.Debug.WriteLine($"Found {allUsers.Count} users, {allFaculties.Count} faculty records.");

                // Join data
                var facultyViewModels = (
                    from user in allUsers
                    where user.Role.Equals("Faculty", StringComparison.OrdinalIgnoreCase) // Only get users who are Faculty
                    join facultyMember in allFaculties
                        on user.UserId equals facultyMember.UserId // Join using UserId
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
                    // Log more details if needed
                }

                return View("~/Views/Admin/FacultyManagement/Index.cshtml", facultyViewModels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Faculty Index: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading faculty list.";
                return View("~/Views/Admin/FacultyManagement/Index.cshtml", new List<FacultyViewModel>());
            }
        }

        // GET: /FacultyManagement/Details/{facultyId}
        [HttpGet]
        public IActionResult Details(string id) // Receives FacultyId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Invalid faculty ID.");
            System.Diagnostics.Debug.WriteLine($"Viewing details for Faculty ID '{id}'.");
            try
            {
                var facultyMember = _unitOfWork.Faculty.GetById(id); // Find Faculty by FacultyId
                if (facultyMember == null) { TempData["ErrorMessage"] = $"Faculty {id} not found."; return RedirectToAction("Index"); }

                var user = UserManagementService.GetInstance().GetUserById(facultyMember.UserId); // Find User by UserId
                if (user == null) { TempData["ErrorMessage"] = $"Error getting user for faculty {id}."; return RedirectToAction("Index"); }

                var viewModel = new FacultyViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FacultyId = facultyMember.FacultyId
                    // Add other fields if any
                };
                return View("~/Views/Admin/FacultyManagement/Details.cshtml", viewModel);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error details faculty {id}: {ex.Message}"); TempData["ErrorMessage"] = "Error viewing details."; return RedirectToAction("Index"); }
        }

        // GET: /FacultyManagement/Edit/{facultyId}
        [HttpGet]
        public IActionResult Edit(string id) // Receives FacultyId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Invalid faculty ID.");
            System.Diagnostics.Debug.WriteLine($"Loading edit page for Faculty ID '{id}'.");
            try
            {
                var facultyMember = _unitOfWork.Faculty.GetById(id); // Find Faculty by FacultyId
                if (facultyMember == null) { TempData["ErrorMessage"] = $"Faculty {id} not found."; return RedirectToAction("Index"); }

                var user = UserManagementService.GetInstance().GetUserById(facultyMember.UserId); // Find User by UserId
                if (user == null) { TempData["ErrorMessage"] = $"Error getting user for faculty {id}."; return RedirectToAction("Index"); }

                var model = new FacultyEditViewModel
                {
                    UserId = user.UserId,
                    FacultyId = facultyMember.FacultyId,
                    Username = user.Username
                    // Password is empty
                };
                return View("~/Views/Admin/FacultyManagement/Edit.cshtml", model);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading edit faculty {id}: {ex.Message}"); TempData["ErrorMessage"] = "Error loading edit page."; return RedirectToAction("Index"); }
        }

        // POST: /FacultyManagement/Edit/{facultyId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, FacultyEditViewModel model) // id is FacultyId
        {
            if (id != model.FacultyId) return BadRequest("Faculty ID mismatch.");
            System.Diagnostics.Debug.WriteLine($"Saving changes for Faculty ID '{id}'. ...");

            // Check password length if entered
            const int minPasswordLength = 6;
            if (!string.IsNullOrEmpty(model.Password) && model.Password.Length < minPasswordLength) { ModelState.AddModelError(nameof(model.Password), $"New password must be at least {minPasswordLength} characters."); }

            if (!ModelState.IsValid) { LogModelStateErrors(id); TempData["ErrorMessage"] = "Invalid data."; return View("~/Views/Admin/FacultyManagement/Edit.cshtml", model); }

            try
            {
                // Get current User information (no need to get Faculty because no changes are made to the Faculty table)
                var existingUser = UserManagementService.GetInstance().GetUserById(model.UserId); // Use UserId from model (hidden field)
                if (existingUser == null) { TempData["ErrorMessage"] = $"Error: User account with ID {model.UserId} not found."; return RedirectToAction("Index"); }
                if (!existingUser.Role.Equals("Faculty", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = "This user is not a faculty member."; return RedirectToAction("Index"); } // Re-check role


                bool userChanged = false;

                // Update User Entity
                if (existingUser.Username != model.Username) { existingUser.Username = model.Username; userChanged = true; }
                if (!string.IsNullOrEmpty(model.Password) && (!ModelState.ContainsKey(nameof(model.Password)) || !ModelState[nameof(model.Password)].Errors.Any())) { existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password); userChanged = true; }

                // Save User changes if any
                if (userChanged)
                {
                    bool userUpdateSuccess = UserManagementService.GetInstance().UpdateUser(existingUser); // Service will handle saving User Repo
                    if (!userUpdateSuccess) { TempData["ErrorMessage"] = "Error updating faculty account."; return View("~/Views/Admin/FacultyManagement/Edit.cshtml", model); }
                    System.Diagnostics.Debug.WriteLine($"User info updated for faculty User ID {model.UserId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No changes detected for faculty User ID {model.UserId}");
                }

                TempData["SuccessMessage"] = $"Successfully updated faculty '{model.Username}'.";
                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "System error during update."; System.Diagnostics.Debug.WriteLine($"Error saving faculty {id}: {ex.Message}"); return View("~/Views/Admin/FacultyManagement/Edit.cshtml", model); }
        }

        // GET: /FacultyManagement/Delete/{userId}
        [HttpGet]
        public IActionResult Delete(string id) // Receives UserId from link in Index
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Invalid user ID.");
            System.Diagnostics.Debug.WriteLine($"Viewing delete confirmation for Faculty with User ID '{id}'.");
            try
            {
                var user = UserManagementService.GetInstance().GetUserById(id);
                if (user == null || !user.Role.Equals("Faculty", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = $"Faculty with user ID {id} not found."; return RedirectToAction("Index"); }

                var facultyMember = _unitOfWork.Faculty.GetByUserId(id); // Get FacultyId to display if needed
                ViewBag.FacultyIdToDelete = facultyMember?.FacultyId;

                return View("~/Views/Admin/FacultyManagement/Delete.cshtml", user); // Use Delete View to display User
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Error loading delete page."; System.Diagnostics.Debug.WriteLine($"Error loading delete faculty user {id}: {ex.Message}"); return RedirectToAction("Index"); }
        }

        // POST: /FacultyManagement/Delete/{userId}
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
                if (userToDelete == null || !userToDelete.Role.Equals("Faculty", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = $"Faculty '{usernameToDelete}' not found."; return RedirectToAction("Index"); }

                bool result = UserManagementService.GetInstance().DeleteUser(id); // Service will delete both User and Faculty record
                if (result) { TempData["SuccessMessage"] = $"Successfully deleted faculty '{usernameToDelete}'."; }
                else { TempData["ErrorMessage"] = $"Error deleting faculty '{usernameToDelete}'."; }
                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "System error during deletion."; System.Diagnostics.Debug.WriteLine($"Error deleting faculty user {id}: {ex.Message}"); return RedirectToAction("Index"); }
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