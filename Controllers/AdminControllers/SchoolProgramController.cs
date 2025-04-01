using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Data; // To use IUnitOfWork
using SchoolManagementSystem.Entities; // To use SchoolProgram, Student
using SchoolManagementSystem.Models; // To use ViewModels
using SchoolManagementSystem.Utils;

namespace SchoolManagementSystem.Controllers.AdminControllers
{
    [Authorize(RoleConstants.Admin)]
    public class SchoolProgramController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DateTime _currentDateTime = DateTime.UtcNow;
        private readonly string _currentUserLogin = "admin_son"; // Assuming admin is Son

        // Constructor: Inject IUnitOfWork (through Singleton in this case)
        public SchoolProgramController()
        {
            _unitOfWork = UnitOfWork.GetInstance();
        }

        // GET: /SchoolProgram/Index
        // Display list of school programs and student count
        [HttpGet]
        public IActionResult Index()
        {
            string actionName = nameof(Index);
            LogActionEntry(actionName);
            try
            {
                // Retrieve necessary data
                var programs = _unitOfWork.SchoolPrograms.GetAll()?.ToList() ?? new List<SchoolProgram>();
                var students = _unitOfWork.Students.GetAll()?.ToList() ?? new List<Student>();

                // Create ViewModel list, including student count
                var viewModels = programs.Select(p => new SchoolProgramViewModel
                {
                    SchoolProgramId = p.SchoolProgramId,
                    SchoolProgramName = p.SchoolProgramName,
                    StudentCount = students.Count(s => s.SchoolProgramId == p.SchoolProgramId) // Count students
                }).OrderBy(vm => vm.SchoolProgramName).ToList(); // Sort by name

                LogActionSuccess(actionName, $"Displayed {viewModels.Count} programs.");
                return View("~/Views/Admin/SchoolProgram/Index.cshtml", viewModels);
            }
            catch (Exception ex)
            {
                // Log error and display error message
                LogActionError(actionName, ex);
                TempData["ErrorMessage"] = "An error occurred while loading the school program list.";
                // Return View with empty list
                return View("~/Views/Admin/SchoolProgram/Index.cshtml", new List<SchoolProgramViewModel>());
            }
        }

        // GET: /SchoolProgram/Create
        // Display form to create new school program
        [HttpGet]
        public IActionResult Create()
        {
            LogActionEntry(nameof(Create) + " (GET)");
            // Simply display empty View
            return View("~/Views/Admin/SchoolProgram/Create.cshtml");
        }

        // POST: /SchoolProgram/Create
        // Handle creation of new school program
        [HttpPost]
        [ValidateAntiForgeryToken] // Prevent CSRF attacks
        public IActionResult Create(SchoolProgramCreateViewModel model)
        {
            string actionName = nameof(Create) + " (POST)";
            LogActionEntry(actionName, $"Attempting to create program '{model.SchoolProgramName}'.");

            // --- Server-side Validation ---
            // 1. Check if program name already exists (case-insensitive, remove extra whitespace)
            string trimmedName = model.SchoolProgramName?.Trim();
            if (!string.IsNullOrEmpty(trimmedName)) // Only check if name is not empty
            {
                bool nameExists = _unitOfWork.SchoolPrograms.GetAll()
                                    .Any(p => p.SchoolProgramName.Equals(trimmedName, StringComparison.OrdinalIgnoreCase));
                if (nameExists)
                {
                    ModelState.AddModelError(nameof(model.SchoolProgramName), "This school program name already exists.");
                }
            }
            // 2. Check ModelState (including errors from validation attributes and newly added errors)
            if (!ModelState.IsValid)
            {
                LogModelStateErrors(actionName);
                TempData["ErrorMessage"] = "Invalid input data. Please check again.";
                return View("~/Views/Admin/SchoolProgram/Create.cshtml", model); // Return View with errors
            }
            // --- End of Validation ---

            try
            {
                // Generate new school program ID
                string newProgramId = GenerateNewProgramId();
                if (string.IsNullOrEmpty(newProgramId))
                {
                    // Critical error when ID cannot be generated
                    LogActionError(actionName, new InvalidOperationException("Could not generate a new Program ID."));
                    TempData["ErrorMessage"] = "System error: Could not generate a new school program ID. Please try again.";
                    return View("~/Views/Admin/SchoolProgram/Create.cshtml", model);
                }

                // Create new Entity object
                var newProgram = new SchoolProgram
                {
                    SchoolProgramId = newProgramId,
                    SchoolProgramName = trimmedName // Use trimmed name
                };

                // Add to Repository and Save changes
                _unitOfWork.SchoolPrograms.Add(newProgram);
                _unitOfWork.SaveChanges();

                // Success message and redirect
                TempData["SuccessMessage"] = $"Successfully created school program '{newProgram.SchoolProgramName}'.";
                LogActionSuccess(actionName, $"Created program '{newProgram.SchoolProgramName}' with ID '{newProgram.SchoolProgramId}'.");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log error and display error message
                LogActionError(actionName, ex);
                TempData["ErrorMessage"] = "A system error occurred while creating the school program.";
                return View("~/Views/Admin/SchoolProgram/Create.cshtml", model);
            }
        }

        // GET: /SchoolProgram/Edit/{programId}
        // Display form to edit school program
        [HttpGet]
        public IActionResult Edit(string id) // Receive ProgramId from route
        {
            string actionName = nameof(Edit) + " (GET)";
            LogActionEntry(actionName, $"Loading edit page for Program ID '{id}'.");

            // Check if ID is valid
            if (string.IsNullOrWhiteSpace(id))
            {
                LogActionWarning(actionName, "Program ID is null or whitespace.");
                return NotFound("Invalid school program ID.");
            }

            try
            {
                // Get school program from Repository
                var program = _unitOfWork.SchoolPrograms.GetById(id);
                if (program == null)
                {
                    // Not found
                    LogActionWarning(actionName, $"Program with ID '{id}' not found.");
                    TempData["ErrorMessage"] = $"School program with ID {id} not found.";
                    return RedirectToAction("Index");
                }

                // Create ViewModel from Entity
                var model = new SchoolProgramEditViewModel
                {
                    SchoolProgramId = program.SchoolProgramId,
                    SchoolProgramName = program.SchoolProgramName
                };

                // Return Edit View with ViewModel
                LogActionSuccess(actionName, $"Loaded program '{program.SchoolProgramName}' for edit.");
                return View("~/Views/Admin/SchoolProgram/Edit.cshtml", model);
            }
            catch (Exception ex)
            {
                LogActionError(actionName, ex, $"Error loading program {id} for edit.");
                TempData["ErrorMessage"] = "An error occurred while loading the edit page.";
                return RedirectToAction("Index");
            }
        }

        // POST: /SchoolProgram/Edit/{programId}
        // Handle update of school program
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, SchoolProgramEditViewModel model) // id is ProgramId from route
        {
            string actionName = nameof(Edit) + " (POST)";
            LogActionEntry(actionName, $"Saving changes for Program ID '{id}'.");

            // Check if ID matches
            if (id != model.SchoolProgramId)
            {
                LogActionWarning(actionName, $"Route ID '{id}' does not match model ID '{model.SchoolProgramId}'.");
                return BadRequest("School program ID mismatch.");
            }

            // --- Server-side Validation ---
            // 1. Check if new name is already used by another program (except itself)
            string trimmedName = model.SchoolProgramName?.Trim();
            if (!string.IsNullOrEmpty(trimmedName))
            {
                bool nameExists = _unitOfWork.SchoolPrograms.GetAll()
                                    .Any(p => p.SchoolProgramId != id && // Important: Exclude itself
                                               p.SchoolProgramName.Equals(trimmedName, StringComparison.OrdinalIgnoreCase));
                if (nameExists)
                {
                    ModelState.AddModelError(nameof(model.SchoolProgramName), "This school program name is already used by another program.");
                }
            }
            // 2. Check ModelState
            if (!ModelState.IsValid)
            {
                LogModelStateErrors(actionName);
                TempData["ErrorMessage"] = "Invalid input data.";
                return View("~/Views/Admin/SchoolProgram/Edit.cshtml", model); // Return View with errors
            }
            // --- End of Validation ---

            try
            {
                // Get current school program from Repository
                var existingProgram = _unitOfWork.SchoolPrograms.GetById(id);
                if (existingProgram == null)
                {
                    LogActionWarning(actionName, $"Program with ID '{id}' not found for update.");
                    TempData["ErrorMessage"] = $"School program with ID {id} not found for update.";
                    return RedirectToAction("Index");
                }

                // Check if there are actual changes
                if (existingProgram.SchoolProgramName != trimmedName)
                {
                    // Update name
                    existingProgram.SchoolProgramName = trimmedName;
                    _unitOfWork.SchoolPrograms.Update(existingProgram);
                    _unitOfWork.SaveChanges(); // Save changes

                    TempData["SuccessMessage"] = $"Successfully updated school program '{existingProgram.SchoolProgramName}'.";
                    LogActionSuccess(actionName, $"Updated program {id}. New name: '{existingProgram.SchoolProgramName}'.");
                }
                else
                {
                    // No changes made
                    TempData["SuccessMessage"] = "No changes were made."; // Or use TempData["InfoMessage"]
                    LogActionInfo(actionName, $"No changes detected for program {id}.");
                }
                // Redirect to Index after success or no changes
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log error and display message
                LogActionError(actionName, ex, $"Error saving changes for program {id}.");
                TempData["ErrorMessage"] = "A system error occurred while updating the school program.";
                return View("~/Views/Admin/SchoolProgram/Edit.cshtml", model); // Return Edit View with old model
            }
        }

        // GET: /SchoolProgram/Delete/{programId}
        // Display delete confirmation page
        [HttpGet]
        public IActionResult Delete(string id) // Receive ProgramId
        {
            string actionName = nameof(Delete) + " (GET)";
            LogActionEntry(actionName, $"Viewing delete confirmation for Program ID '{id}'.");

            if (string.IsNullOrWhiteSpace(id)) { LogActionWarning(actionName, "ID is null."); return NotFound(); }

            try
            {
                var program = _unitOfWork.SchoolPrograms.GetById(id);
                if (program == null) { LogActionWarning(actionName, $"Program {id} not found."); TempData["ErrorMessage"] = $"Program {id} not found."; return RedirectToAction("Index"); }

                // Count students to warn
                int studentCount = _unitOfWork.Students.GetBySchoolProgram(id).Count();
                ViewBag.StudentCount = studentCount;
                LogActionInfo(actionName, $"Program {id} ('{program.SchoolProgramName}') has {studentCount} students.");

                return View("~/Views/Admin/SchoolProgram/Delete.cshtml", program);
            }
            catch (Exception ex) { LogActionError(actionName, ex, $"Error loading program {id} for delete."); TempData["ErrorMessage"] = "Error loading delete page."; return RedirectToAction("Index"); }
        }

        // POST: /SchoolProgram/Delete/{programId}
        // Handle deletion of school program
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id) // id is ProgramId
        {
            string actionName = nameof(DeleteConfirmed) + " (POST)";
            LogActionEntry(actionName, $"Confirming deletion for Program ID '{id}'.");

            if (string.IsNullOrWhiteSpace(id)) { LogActionWarning(actionName, "ID is null."); return BadRequest(); }

            try
            {
                var programToDelete = _unitOfWork.SchoolPrograms.GetById(id);
                if (programToDelete == null) { LogActionWarning(actionName, $"Program {id} not found."); TempData["ErrorMessage"] = $"Program {id} not found."; return RedirectToAction("Index"); }

                // *** Check student constraints ***
                bool hasStudents = _unitOfWork.Students.GetBySchoolProgram(id).Any();
                if (hasStudents)
                {
                    LogActionWarning(actionName, $"Deletion failed for program {id}: Students assigned.");
                    TempData["ErrorMessage"] = $"Cannot delete program '{programToDelete.SchoolProgramName}' because students are currently enrolled.";
                    // Do not delete, just redirect to Index with error
                    return RedirectToAction("Index");
                }

                // Perform deletion
                _unitOfWork.SchoolPrograms.Delete(id);
                _unitOfWork.SaveChanges();

                TempData["SuccessMessage"] = $"Deleted program '{programToDelete.SchoolProgramName}'.";
                LogActionSuccess(actionName, $"Deleted program {id}.");
                return RedirectToAction("Index");
            }
            catch (Exception ex) { LogActionError(actionName, ex, $"Error deleting program {id}."); TempData["ErrorMessage"] = "System error during deletion."; return RedirectToAction("Index"); }
        }


        // --- Helper Methods ---
        private string GenerateNewProgramId()
        {
            try
            {
                var existingIds = _unitOfWork.SchoolPrograms.GetAll()
                                    .Select(p => p.SchoolProgramId)
                                    .Where(id => id.StartsWith("SP") && id.Length > 2 && int.TryParse(id.Substring(2), out _))
                                    .Select(id => int.Parse(id.Substring(2)))
                                    .ToList();
                int nextIdNumber = existingIds.Any() ? existingIds.Max() + 1 : 1;
                return $"SP{nextIdNumber:D3}";
            }
            catch (Exception ex)
            {
                LogActionError(nameof(GenerateNewProgramId), ex);
                return null; // Return null if error occurs
            }
        }

        // Helper logging methods (example)
        private void LogActionEntry(string actionName, string? message = null) { System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:T}] User '{_currentUserLogin}' entered {actionName}. {message}"); }
        private void LogActionSuccess(string actionName, string message) { System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:T}] SUCCESS: {actionName} by '{_currentUserLogin}'. {message}"); }
        private void LogActionInfo(string actionName, string message) { System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:T}] INFO: {actionName} by '{_currentUserLogin}'. {message}"); }
        private void LogActionWarning(string actionName, string message) { System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:T}] WARNING: {actionName} by '{_currentUserLogin}'. {message}"); }
        private void LogActionError(string actionName, Exception ex, string? message = null) { System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:T}] ERROR: {actionName} by '{_currentUserLogin}'. {message} Exception: {ex.Message}"); /* Write full error to actual log file */ }
        private void LogModelStateErrors(string actionName)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            LogActionWarning(actionName, $"ModelState Invalid. Errors: {string.Join("; ", errors)}");
        }
    }
}