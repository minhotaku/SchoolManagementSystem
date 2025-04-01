using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces;
using SchoolManagementSystem.Utils;

namespace SchoolManagementSystem.Controllers
{
    [Authorize(RoleConstants.Admin)]
    public class UserManagementController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IUnitOfWork _unitOfWork;

        // Current UTC date and time and current logged-in user information
        private readonly DateTime _currentDateTime = new DateTime(2025, 03, 31, 10, 28, 53);
        private readonly string _currentUserLogin = "minhotaku";

        /// <summary>
        /// Initializes the user management controller
        /// </summary>
        public UserManagementController()
        {
            _userManagementService = UserManagementService.GetInstance();
            _unitOfWork = UnitOfWork.GetInstance();

            // Set the layout for all actions in this controller
            ViewData["Layout"] = "_AdminLayout";
        }

        /// <summary>
        /// Displays the list of users
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            // Log activity
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} accessed user management index");

            // Get all users to display
            var users = _userManagementService.GetAllUsers();
            return View(users);
        }

        /// <summary>
        /// Displays the form to register a new user
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            // Prepare the list of school programs for the registration form
            ViewBag.SchoolPrograms = _unitOfWork.SchoolPrograms.GetAll();

            // Log activity
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} accessed user registration form");

            return View();
        }

        /// <summary>
        /// Processes the registration of a new user
        /// </summary>
        [HttpPost]
        public IActionResult Register(string username, string password, string role, string schoolProgramId)
        {
            // Log activity
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} attempted to register a new user with role {role}");

            // Check input data validity
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                TempData["ErrorMessage"] = "Please fill in all required information.";
                ViewBag.SchoolPrograms = _unitOfWork.SchoolPrograms.GetAll();
                return View();
            }

            // Prepare additional information for students
            Dictionary<string, string> additionalInfo = null;
            if (role == "Student" && !string.IsNullOrWhiteSpace(schoolProgramId))
            {
                additionalInfo = new Dictionary<string, string>
                {
                    { "SchoolProgramId", schoolProgramId }
                };
            }

            // Call service to register the new user
            var userId = _userManagementService.RegisterUser(username, password, role, additionalInfo);
            if (userId == null)
            {
                TempData["ErrorMessage"] = "User registration error. Please check if the username already exists or if the information is invalid.";
                ViewBag.SchoolPrograms = _unitOfWork.SchoolPrograms.GetAll();
                return View();
            }

            // Display success message
            TempData["SuccessMessage"] = $"Successfully registered user with role {role}.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Displays the form to edit user information
        /// </summary>
        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            // Get the user information to edit
            var user = _userManagementService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }

            // Log activity
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} accessed edit form for user ID {id}");

            return View(user);
        }

        /// <summary>
        /// Processes the update of user information
        /// </summary>
        [HttpPost]
        public IActionResult Edit(User user, string newPassword)
        {
            // Log activity
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} attempted to update user ID {user?.UserId}");

            if (user == null || string.IsNullOrWhiteSpace(user.UserId))
            {
                return NotFound();
            }

            // Get current user information
            var existingUser = _userManagementService.GetUserById(user.UserId);
            if (existingUser == null)
            {
                return NotFound();
            }

            // Update user information
            existingUser.Username = user.Username;

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                existingUser.PasswordHash = HashPassword.ComputeSha256Hash(newPassword);
            }

            // Save updated information
            bool result = _userManagementService.UpdateUser(existingUser);
            if (!result)
            {
                TempData["ErrorMessage"] = "Error updating user.";
                return View(user);
            }

            // Display success message
            TempData["SuccessMessage"] = "Successfully updated user information.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Displays the page to confirm user deletion
        /// </summary>
        [HttpGet]
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            // Get the user information to delete
            var user = _userManagementService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }

            // Log activity
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} accessed delete confirmation for user ID {id}");

            return View(user);
        }

        /// <summary>
        /// Processes user deletion
        /// </summary>
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(string id)
        {
            // Log activity
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} attempted to delete user ID {id}");

            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            // Delete user and related information
            bool result = _userManagementService.DeleteUser(id);
            if (!result)
            {
                TempData["ErrorMessage"] = "Error deleting user.";
                return RedirectToAction("Index");
            }

            // Display success message
            TempData["SuccessMessage"] = "Successfully deleted user.";
            return RedirectToAction("Index");
        }
    }
}