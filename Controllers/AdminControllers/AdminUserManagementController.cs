// Using necessary namespaces for the Controller
using System;
using System.Collections.Generic;
using System.Linq;
using BCrypt.Net; // Password hashing library (Note: the code is using BCrypt, but the service uses SHA256 -> inconsistent)
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // For using SelectList for dropdowns
using SchoolManagementSystem.Data; // For using IUnitOfWork
using SchoolManagementSystem.Entities; // For using entity classes (User, Student, ...)
using SchoolManagementSystem.Services.Implementation; // For using specific service classes (UserManagementService)
using SchoolManagementSystem.Services.Interfaces;   // For using service interfaces (IUserManagementService)
using SchoolManagementSystem.Models; // For using ViewModel classes
using SchoolManagementSystem.Utils; // For using AuthorizeAttribute, RoleConstants

// Defines the namespace for Admin controllers
namespace SchoolManagementSystem.Controllers.AdminControllers
{
    // Authorize attribute ensures only users with the Admin role can access this controller
    [Authorize(RoleConstants.Admin)]
    // Declares the AdminUserManagementController class, which inherits from the base Controller class
    // This controller is responsible for managing *all* users (Student, Faculty, Admin) from the Admin's perspective
    public class AdminUserManagementController : Controller
    {
        // Interface for interacting with user management business logic
        private readonly IUserManagementService _userManagementService;
        // Interface for managing repositories and saving data changes
        private readonly IUnitOfWork _unitOfWork;

        // Temporary information about the current time and user (for logging)
        private readonly DateTime _currentDateTime = DateTime.UtcNow;
        private readonly string _currentUserLogin = "admin_son"; // Assumed admin username

        // Constructor: Initializes necessary services and unit of work
        public AdminUserManagementController()
        {
            // Get the Singleton instance of UserManagementService
            _userManagementService = UserManagementService.GetInstance();
            // Get the Singleton instance of UnitOfWork
            _unitOfWork = UnitOfWork.GetInstance();
        }

        // 42-95
        // GET: /AdminUserManagement/Index
        // Action to display the list of all users
        [HttpGet]
        public IActionResult Index()
        {
            // Log the activity
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' accessed the user management page.");
            try
            {
                // Retrieve the list of all users from the service
                var users = _userManagementService.GetAllUsers()?.ToList() ?? new List<User>();
                // Retrieve the lists of all students, faculty, admins, and school programs from UnitOfWork
                var students = _unitOfWork.Students.GetAll()?.ToList() ?? new List<Student>();
                var faculties = _unitOfWork.Faculty.GetAll()?.ToList() ?? new List<Faculty>();
                var admins = _unitOfWork.Admins.GetAll()?.ToList() ?? new List<Admin>();
                var programs = _unitOfWork.SchoolPrograms.GetAll()?.ToList() ?? new List<SchoolProgram>();

                // Use LINQ to create a list of ViewModels to display aggregated user information
                var userViewModels = users.Select(u => {
                    // Find Student, Faculty, Admin information corresponding to the current User
                    Student studentInfo = students.FirstOrDefault(s => s.UserId == u.UserId);
                    Faculty facultyInfo = faculties.FirstOrDefault(f => f.UserId == u.UserId);
                    Admin adminInfo = admins.FirstOrDefault(a => a.UserId == u.UserId);
                    // If it's a Student, find school program information
                    SchoolProgram programInfo = (studentInfo != null) ? programs.FirstOrDefault(p => p.SchoolProgramId == studentInfo.SchoolProgramId) : null;

                    // Create AdminUserViewModel with the retrieved information
                    return new AdminUserViewModel
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        Role = u.Role,
                        StudentId = studentInfo?.StudentId, // Use null-conditional operator to avoid errors if not found
                        FacultyId = facultyInfo?.FacultyId,
                        AdminId = adminInfo?.AdminId,
                        SchoolProgramId = studentInfo?.SchoolProgramId,
                        SchoolProgramName = programInfo?.SchoolProgramName ?? (studentInfo != null ? "(Unknown Program)" : null) // Display program name or message if not available
                    };
                }).ToList(); // Convert the result to a List

                // Return the Index View with the created ViewModel list
                return View("~/Views/Admin/AdminUserManagement/Index.cshtml", userViewModels);
            }
            catch (Exception ex) // Handle exceptions
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error accessing the user management page: {ex.Message}");
                // Set an error message to display on the View
                TempData["ErrorMessage"] = "An error occurred while loading the user list.";
                // Return the Index View with an empty ViewModel list
                return View("~/Views/Admin/AdminUserManagement/Index.cshtml", new List<AdminUserViewModel>());
            }
        }

        // 97-122
        // GET: /AdminUserManagement/Create
        // Action to display the create user form
        [HttpGet]
        public IActionResult Create()
        {
            try
            {
                // Retrieve the list of school programs from UnitOfWork
                var programs = _unitOfWork.SchoolPrograms.GetAll();
                // Create a SelectList from the list of school programs and assign it to ViewBag to display a dropdown on the View
                // If programs is null, create an empty SelectList
                ViewBag.SchoolPrograms = new SelectList(programs ?? new List<SchoolProgram>(), "SchoolProgramId", "SchoolProgramName");
            }
            catch (Exception ex) // Handle errors loading school programs
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error loading school program list for the Create User form: {ex.Message}");
                // Create an empty SelectList to avoid errors on the View
                ViewBag.SchoolPrograms = new SelectList(new List<SchoolProgram>());
                // Set an error message
                TempData["ErrorMessage"] = "Error loading the school program list.";
            }
            // Return the Create View (user creation form)
            return View("~/Views/Admin/AdminUserManagement/Create.cshtml");
        }

        // 124-211
        // POST: /AdminUserManagement/Create
        // Action to handle the creation of a new user when the form is submitted
        [HttpPost]
        // This attribute helps prevent Cross-Site Request Forgery (CSRF) attacks
        [ValidateAntiForgeryToken]
        // The model parameter receives data from the form (automatically bound by ASP.NET Core)
        public IActionResult Create(UserCreateViewModel model)
        {
            // Log the activity
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' is attempting to create user '{model.Username}'.");

            // Reload the list of school programs into ViewBag in case the form needs to be redisplayed if there are errors
            // Necessary because ViewBag does not persist across different requests
            try
            {
                var programs = _unitOfWork.SchoolPrograms.GetAll();
                // Reassign the SelectList, keeping the user's selected SchoolProgramId value (if any)
                ViewBag.SchoolPrograms = new SelectList(programs ?? new List<SchoolProgram>(), "SchoolProgramId", "SchoolProgramName", model.SchoolProgramId);
            }
            catch (Exception ex)
            {
                // Log the error reloading school programs
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error reloading the school program list for POST Create User: {ex.Message}");
                ViewBag.SchoolPrograms = new SelectList(new List<SchoolProgram>());
            }

            // Check if the data from the form is valid (based on the DataAnnotations in UserCreateViewModel)
            if (!ModelState.IsValid)
            {
                // If not valid, set an error message and return the Create View with the current model data for the user to correct errors
                TempData["ErrorMessage"] = "Invalid input data. Please check again.";
                return View("~/Views/Admin/AdminUserManagement/Create.cshtml", model);
            }

            // Additional check: If the role is Student, then SchoolProgramId must not be empty
            if (model.Role.Equals("Student", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(model.SchoolProgramId))
            {
                // Add an error to ModelState for the SchoolProgramId field
                ModelState.AddModelError(nameof(model.SchoolProgramId), "Please select a school program for students.");
                // Set an error message and return the Create View
                TempData["ErrorMessage"] = "Invalid input data. Please check again.";
                return View("~/Views/Admin/AdminUserManagement/Create.cshtml", model);
            }

            // Try-catch block to handle errors during the user creation service call
            try
            {
                // Prepare additional information (if it's a student, SchoolProgramId is required)
                Dictionary<string, string> additionalInfo = null;
                if (model.Role.Equals("Student", StringComparison.OrdinalIgnoreCase))
                {
                    additionalInfo = new Dictionary<string, string> { { "SchoolProgramId", model.SchoolProgramId } };
                }

                // Call the service to register the new user
                var userId = _userManagementService.RegisterUser(model.Username, model.Password, model.Role, additionalInfo);

                // Check the result returned from the service
                if (userId != null) // If creation is successful (service returns UserId)
                {
                    // Set a success message
                    TempData["SuccessMessage"] = $"Successfully created user '{model.Username}' with role {model.Role}.";
                    // Log the success
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' successfully created user '{model.Username}' (ID: {userId}).");
                    // Redirect to the user list page (Index)
                    return RedirectToAction("Index");
                }
                else // If creation fails (service returns null)
                {
                    // Set an error message (possibly due to username already existing or invalid school program)
                    TempData["ErrorMessage"] = "Error creating user. The username may already exist or the school program is invalid.";
                    // Log the failure
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' failed to create user '{model.Username}'. Service returned null.");
                    // Return the Create View for the user to re-enter data
                    return View("~/Views/Admin/AdminUserManagement/Create.cshtml", model);
                }
            }
            catch (Exception ex) // Handle serious system errors
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error creating user '{model.Username}': {ex.Message}");
                // Set a system error message
                TempData["ErrorMessage"] = "A system error occurred while creating the user.";
                // Return the Create View
                return View("~/Views/Admin/AdminUserManagement/Create.cshtml", model);
            }
        }

        // GET: /AdminUserManagement/Edit/{id}
        // Action to display the user information edit form
        [HttpGet]
        // The id parameter is the UserId passed through the route
        public IActionResult Edit(string id)
        {
            // Check if the id is valid
            if (string.IsNullOrWhiteSpace(id)) return NotFound("User ID not found.");

            // Log the activity
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' is attempting to load the edit page for user ID '{id}'.");

            try
            {
                // Retrieve the user information to edit from the service
                var user = _userManagementService.GetUserById(id);
                // If the user is not found, set an error message and redirect to Index
                if (user == null)
                {
                    TempData["ErrorMessage"] = $"User with ID {id} not found.";
                    return RedirectToAction("Index");
                }

                // Create a ViewModel to hold the data for the Edit form
                var model = new UserEditViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    CurrentRole = user.Role // Assign the current role (important to know if it's a Student)
                };

                // Check if the user is a Student
                bool isStudent = user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase);
                if (isStudent) // If it's a Student
                {
                    // Retrieve the corresponding Student information from UnitOfWork
                    var student = _unitOfWork.Students.GetByUserId(id);
                    // Assign the current SchoolProgramId to the model
                    model.SchoolProgramId = student?.SchoolProgramId;
                    // Log a warning if the corresponding Student record is not found
                    if (student == null) System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Warning: Student record not found for user ID '{id}'.");

                    // Load the list of school programs into ViewBag (only needed for Students)
                    try
                    {
                        var programs = _unitOfWork.SchoolPrograms.GetAll();
                        // Create a SelectList and pre-select the student's current school program
                        ViewBag.SchoolPrograms = new SelectList(programs ?? new List<SchoolProgram>(), "SchoolProgramId", "SchoolProgramName", model.SchoolProgramId);
                    }
                    catch (Exception progEx) // Handle errors loading school programs
                    {
                        System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error loading school programs for GET Edit User: {progEx.Message}");
                        ViewBag.SchoolPrograms = new SelectList(new List<SchoolProgram>()); // Create an empty SelectList
                        TempData["ErrorMessage"] = "Error loading the school program list.";
                    }
                }
                else // If it's not a Student
                {
                    // Still create an empty ViewBag to avoid errors on the View if the View references it
                    ViewBag.SchoolPrograms = new SelectList(new List<SchoolProgram>());
                }

                // Return the Edit View with the prepared model data
                return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
            }
            catch (Exception ex) // Handle general errors loading the Edit page
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error loading user '{id}' for editing: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading user information for editing.";
                return RedirectToAction("Index");
            }
        }

        // POST: /AdminUserManagement/Edit/{id}
        // Action to handle updating user information when the Edit form is submitted
        [HttpPost]
        // Prevent CSRF attacks
        [ValidateAntiForgeryToken]
        // The id parameter is the UserId from the route, model is the data from the form
        public IActionResult Edit(string id, UserEditViewModel model)
        {
            // Check if the UserId from the route and from the model match
            if (id != model.UserId) return BadRequest("User ID mismatch.");

            // Log the activity, recording whether a password was submitted
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' is attempting to save changes for user ID '{id}'. Password submitted: '{(string.IsNullOrEmpty(model.Password) ? "[Empty]" : "[Provided]")}'");

            // Determine if the user being edited is a Student (based on CurrentRole from the ViewModel)
            bool isStudent = model.CurrentRole.Equals("Student", StringComparison.OrdinalIgnoreCase);

            // Reload the ViewBag containing the list of school programs if it's a Student (needed when returning the View if there are errors)
            if (isStudent)
            {
                try
                {
                    var programs = _unitOfWork.SchoolPrograms.GetAll();
                    ViewBag.SchoolPrograms = new SelectList(programs ?? new List<SchoolProgram>(), "SchoolProgramId", "SchoolProgramName", model.SchoolProgramId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error reloading school programs for POST Edit User: {ex.Message}");
                    ViewBag.SchoolPrograms = new SelectList(new List<SchoolProgram>());
                }
            }
            else
            {
                ViewBag.SchoolPrograms = new SelectList(new List<SchoolProgram>());
            }

            // --- Manually check password length IF a password is provided ---
            const int minPasswordLength = 6; // Minimum password length
            if (!string.IsNullOrEmpty(model.Password) && model.Password.Length < minPasswordLength)
            {
                // If a password is provided and it's too short, add an error to ModelState
                ModelState.AddModelError(nameof(model.Password), $"The new password must be at least {minPasswordLength} characters long.");
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Password validation failed for user ID '{id}': Too short.");
            }
            else if (!string.IsNullOrEmpty(model.Password))
            {
                // Log if a valid password is provided
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Password provided for user ID '{id}' and passed length check.");
            }
            else
            {
                // Log if a password is not provided (no change)
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Password field empty for user ID '{id}', skipping length check.");
            }

            // Check the overall ModelState (including password errors if any)
            if (!ModelState.IsValid)
            {
                // Log detailed ModelState errors
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] ModelState invalid for user ID '{id}'. Errors:");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Any())
                    {
                        foreach (var error in state.Errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - Field: {key}, Error: {error.ErrorMessage}");
                        }
                    }
                }
                // Set an error message and return the Edit View with the current model
                TempData["ErrorMessage"] = "Invalid input data. Please check the fields with errors.";
                return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
            }

            // Check SchoolProgramId if it's a Student (after ModelState is valid)
            if (isStudent && string.IsNullOrWhiteSpace(model.SchoolProgramId))
            {
                ModelState.AddModelError(nameof(model.SchoolProgramId), "Please select a school program for students.");
                TempData["ErrorMessage"] = "Invalid input data. Please check again.";
                return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
            }

            // Try-catch block to handle errors when saving changes
            try
            {
                // Retrieve the current user information from the service
                var existingUser = _userManagementService.GetUserById(id);
                if (existingUser == null) // If user not found
                {
                    TempData["ErrorMessage"] = $"User with ID {id} not found for updating.";
                    return RedirectToAction("Index"); // Redirect to Index
                }

                // Update Username
                existingUser.Username = model.Username;

                // Only hash and update the password if it is provided and has passed the length check
                if (!string.IsNullOrEmpty(model.Password) &&
                    (!ModelState.ContainsKey(nameof(model.Password)) || !ModelState[nameof(model.Password)].Errors.Any()))
                {
                    // Hash the new password using BCrypt (Note: Ensure Service and Login also use BCrypt)
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' updated password for user ID '{id}'.");
                }

                // Call the service to update the basic User information (save to users.csv)
                bool userUpdateResult = _userManagementService.UpdateUser(existingUser);

                // If updating User information fails
                if (!userUpdateResult)
                {
                    TempData["ErrorMessage"] = "Error updating basic user information.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' failed to update basic information for user ID '{id}'.");
                    return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model); // Return the Edit View
                }

                // Variable to check the result of updating specific information (e.g., SchoolProgramId for Student)
                bool specificInfoUpdateResult = true;
                if (isStudent) // If the user is a Student
                {
                    // Retrieve the Student information from UnitOfWork
                    var student = _unitOfWork.Students.GetByUserId(id);
                    if (student != null) // If the Student record is found
                    {
                        // If the SchoolProgramId has changed
                        if (student.SchoolProgramId != model.SchoolProgramId)
                        {
                            // Check if the new SchoolProgramId exists in the system
                            var programExists = _unitOfWork.SchoolPrograms.GetById(model.SchoolProgramId) != null;
                            if (programExists) // If the school program is valid
                            {
                                // Update the SchoolProgramId for the Student object
                                student.SchoolProgramId = model.SchoolProgramId;
                                try
                                {
                                    // Update the Student record in the repository and save the changes to the students.csv file
                                    _unitOfWork.Students.Update(student);
                                    _unitOfWork.SaveChanges();
                                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' updated SchoolProgramId for student with user ID '{id}'.");
                                }
                                catch (Exception studentEx) // Handle errors when saving Student information
                                {
                                    specificInfoUpdateResult = false; // Mark update as failed
                                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error saving student information for user ID '{id}': {studentEx.Message}");
                                    TempData["ErrorMessage"] = "Error updating the school program.";
                                }
                            }
                            else // If the selected school program is invalid
                            {
                                specificInfoUpdateResult = false; // Mark update as failed
                                TempData["ErrorMessage"] = "The selected school program is invalid.";
                                ModelState.AddModelError(nameof(model.SchoolProgramId), "Invalid school program.");
                                return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model); // Return the Edit View
                            }
                        }
                    }
                    else // If the corresponding Student record is not found
                    {
                        specificInfoUpdateResult = false; // Mark update as failed
                        System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error: Student record not found for user ID '{id}'.");
                        TempData["ErrorMessage"] = "Error: The corresponding student record was not found.";
                    }
                }

                // Check the result of updating specific information
                if (specificInfoUpdateResult) // If successful (or no update needed)
                {
                    // Set a success message
                    TempData["SuccessMessage"] = $"Successfully updated user information for '{model.Username}'.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' successfully updated user ID '{id}'.");
                    // Redirect to the Index page
                    return RedirectToAction("Index");
                }
                else // If updating specific information fails
                {
                    // The error has already been set in TempData/ModelState, just return the Edit View
                    return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
                }
            }
            catch (Exception ex) // Handle serious system errors when saving
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error saving changes for user ID '{id}': {ex.Message}");
                TempData["ErrorMessage"] = "A system error occurred while updating the user.";
                return View("~/Views/Admin/AdminUserManagement/Edit.cshtml", model);
            }
        }

        // GET: /AdminUserManagement/Delete/{id}
        // Action to display the confirm user deletion page
        [HttpGet]
        // The id parameter is the UserId
        public IActionResult Delete(string id)
        {
            // Check for valid id
            if (string.IsNullOrWhiteSpace(id)) return NotFound("User ID not found.");

            // Log the activity
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' is viewing the delete confirmation for user ID '{id}'.");

            try
            {
                // Get the user information for deletion
                var user = _userManagementService.GetUserById(id);
                // If not found, set an error message and redirect to Index
                if (user == null)
                {
                    TempData["ErrorMessage"] = $"User with ID {id} not found for deletion.";
                    return RedirectToAction("Index");
                }
                // Return the Delete View, passing the user object to display confirmation information
                return View("~/Views/Admin/AdminUserManagement/Delete.cshtml", user);
            }
            catch (Exception ex) // Handle errors loading the confirmation page
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error loading user '{id}' for delete confirmation: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading user information for deletion.";
                return RedirectToAction("Index");
            }
        }

        // POST: /AdminUserManagement/Delete/{id}
        // Action to perform the user deletion after confirmation
        [HttpPost, ActionName("Delete")] // Set the action name to "Delete" to match the form post
        // Prevent CSRF attacks
        [ValidateAntiForgeryToken]
        // The id parameter is the UserId
        public IActionResult DeleteConfirmed(string id)
        {
            // Check for valid id
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("Invalid User ID.");

            // Log the activity
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' confirmed deletion of user ID '{id}'.");

            try
            {
                // Get the user information to be deleted to display the name in the message
                var userToDelete = _userManagementService.GetUserById(id);
                string usernameToDelete = userToDelete?.Username ?? $"ID {id}"; // Get the username or use the ID if the user no longer exists

                // Double check if the user exists (may have been deleted by another request)
                if (userToDelete == null)
                {
                    TempData["ErrorMessage"] = $"User '{usernameToDelete}' not found. May have already been deleted.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Deletion failed: User ID '{id}' not found.");
                    return RedirectToAction("Index");
                }

                // Call the service to delete the user. This service will handle deleting the User record and related records (Student, Faculty, Admin)
                bool result = _userManagementService.DeleteUser(id);

                // Check the deletion result
                if (result) // If deletion is successful
                {
                    TempData["SuccessMessage"] = $"Successfully deleted user '{usernameToDelete}'.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Successfully deleted user ID '{id}' ('{usernameToDelete}').");
                }
                else // If deletion fails
                {
                    TempData["ErrorMessage"] = $"Error deleting user '{usernameToDelete}'.";
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Deletion failed for user ID '{id}'. Service returned false.");
                }
                // Always redirect to the Index page after processing
                return RedirectToAction("Index");
            }
            catch (Exception ex) // Handle serious system errors when deleting
            {
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error deleting user ID '{id}': {ex.Message}");
                TempData["ErrorMessage"] = "A system error occurred while deleting the user.";
                return RedirectToAction("Index");
            }
        }
    }
}