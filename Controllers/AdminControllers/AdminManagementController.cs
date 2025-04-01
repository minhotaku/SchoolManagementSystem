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

        // Current user information (Temporary)
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
                    where user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) // Only get users who are Admins
                    join adminRecord in allAdmins
                        on user.UserId equals adminRecord.UserId // Join by UserId
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
                TempData["ErrorMessage"] = "Error loading admin list.";
                return View("~/Views/Admin/AdminManagement/Index.cshtml", new List<AdminViewModel>());
            }
        }

        // GET: /AdminManagement/Details/{adminId}
        [HttpGet]
        public IActionResult Details(string id) // Receive AdminId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Invalid admin code.");
            System.Diagnostics.Debug.WriteLine($"Viewing details for Admin ID '{id}'.");
            try
            {
                var adminRecord = _unitOfWork.Admins.GetById(id); // Find Admin by AdminId
                if (adminRecord == null) { TempData["ErrorMessage"] = $"Admin {id} not found."; return RedirectToAction("Index"); }

                var user = UserManagementService.GetInstance().GetUserById(adminRecord.UserId); // Find User by UserId
                if (user == null) { TempData["ErrorMessage"] = $"User error for admin {id}."; return RedirectToAction("Index"); }

                var viewModel = new AdminViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    AdminId = adminRecord.AdminId
                };
                return View("~/Views/Admin/AdminManagement/Details.cshtml", viewModel);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error details admin {id}: {ex.Message}"); TempData["ErrorMessage"] = "Error viewing details."; return RedirectToAction("Index"); }
        }

        // GET: /AdminManagement/Edit/{adminId}
        [HttpGet]
        public IActionResult Edit(string id) // Receive AdminId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Invalid admin code.");
            System.Diagnostics.Debug.WriteLine($"Loading edit page for Admin ID '{id}'.");
            try
            {
                var adminRecord = _unitOfWork.Admins.GetById(id); // Find Admin by AdminId
                if (adminRecord == null) { TempData["ErrorMessage"] = $"Admin {id} not found."; return RedirectToAction("Index"); }

                var user = UserManagementService.GetInstance().GetUserById(adminRecord.UserId); // Find User by UserId
                if (user == null) { TempData["ErrorMessage"] = $"User error for admin {id}."; return RedirectToAction("Index"); }

                var model = new AdminEditViewModel
                {
                    UserId = user.UserId,
                    AdminId = adminRecord.AdminId,
                    Username = user.Username
                    // Password is empty
                };
                return View("~/Views/Admin/AdminManagement/Edit.cshtml", model);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading edit admin {id}: {ex.Message}"); TempData["ErrorMessage"] = "Error loading edit page."; return RedirectToAction("Index"); }
        }

        // POST: /AdminManagement/Edit/{adminId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, AdminEditViewModel model) // id is AdminId
        {
            if (id != model.AdminId) return BadRequest("Admin code does not match.");
            System.Diagnostics.Debug.WriteLine($"Saving changes for Admin ID '{id}'. ...");

            const int minPasswordLength = 6;
            if (!string.IsNullOrEmpty(model.Password) && model.Password.Length < minPasswordLength) { ModelState.AddModelError(nameof(model.Password), $"New password must be at least {minPasswordLength} characters."); }

            if (!ModelState.IsValid) { LogModelStateErrors(id); TempData["ErrorMessage"] = "Invalid data."; return View("~/Views/Admin/AdminManagement/Edit.cshtml", model); }

            try
            {
                var existingUser = UserManagementService.GetInstance().GetUserById(model.UserId);
                if (existingUser == null) { TempData["ErrorMessage"] = $"Error: User account with ID {model.UserId} not found."; return RedirectToAction("Index"); }
                if (!existingUser.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = "This user is not an admin."; return RedirectToAction("Index"); }

                bool userChanged = false;

                if (existingUser.Username != model.Username) { existingUser.Username = model.Username; userChanged = true; }
                if (!string.IsNullOrEmpty(model.Password) && (!ModelState.ContainsKey(nameof(model.Password)) || !ModelState[nameof(model.Password)].Errors.Any())) { existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password); userChanged = true; }

                if (userChanged)
                {
                    bool userUpdateSuccess = UserManagementService.GetInstance().UpdateUser(existingUser);
                    if (!userUpdateSuccess) { TempData["ErrorMessage"] = "Error updating admin account."; return View("~/Views/Admin/AdminManagement/Edit.cshtml", model); }
                    System.Diagnostics.Debug.WriteLine($"User info updated for admin User ID {model.UserId}");
                }
                else { System.Diagnostics.Debug.WriteLine($"No changes detected for admin User ID {model.UserId}"); }

                TempData["SuccessMessage"] = $"Successfully updated admin '{model.Username}'.";
                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "System error during update."; System.Diagnostics.Debug.WriteLine($"Error saving admin {id}: {ex.Message}"); return View("~/Views/Admin/AdminManagement/Edit.cshtml", model); }
        }

        // GET: /AdminManagement/Delete/{userId}
        [HttpGet]
        public IActionResult Delete(string id) // Receive UserId from link in Index
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Invalid user ID.");
            System.Diagnostics.Debug.WriteLine($"Viewing delete confirmation for Admin with User ID '{id}'.");
            try
            {
                var user = UserManagementService.GetInstance().GetUserById(id);
                if (user == null || !user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = $"Admin with user ID {id} not found."; return RedirectToAction("Index"); }

                var adminRecord = _unitOfWork.Admins.GetByUserId(id);
                ViewBag.AdminIdToDelete = adminRecord?.AdminId;

                return View("~/Views/Admin/AdminManagement/Delete.cshtml", user);
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Error loading delete page."; System.Diagnostics.Debug.WriteLine($"Error loading delete admin user {id}: {ex.Message}"); return RedirectToAction("Index"); }
        }

        // POST: /AdminManagement/Delete/{userId}
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
                if (userToDelete == null || !userToDelete.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = $"Admin '{usernameToDelete}' not found."; return RedirectToAction("Index"); }

                var allAdmins = _unitOfWork.Admins.GetAll();
                if (allAdmins.Count() <= 1)
                {
                    TempData["ErrorMessage"] = "Cannot delete the last administrator account.";
                    return RedirectToAction("Index");
                }
                if (userToDelete.UserId == HttpContext.Session.GetString("_UserId"))
                { // Assuming SessionKeyUserId is "_UserId"
                    TempData["ErrorMessage"] = "You cannot delete your own account.";
                    return RedirectToAction("Index");
                }

                bool result = UserManagementService.GetInstance().DeleteUser(id); // Service will delete both User and Admin record
                if (result) { TempData["SuccessMessage"] = $"Admin '{usernameToDelete}' deleted successfully."; }
                else { TempData["ErrorMessage"] = $"Error deleting admin '{usernameToDelete}'."; }
                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "System error during deletion."; System.Diagnostics.Debug.WriteLine($"Error deleting admin user {id}: {ex.Message}"); return RedirectToAction("Index"); }
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