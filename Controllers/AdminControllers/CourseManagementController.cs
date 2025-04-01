using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Utils;

namespace SchoolManagementSystem.Controllers.AdminControllers
{
    [Authorize(RoleConstants.Admin)]
    public class CourseManagementController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DateTime _currentDateTime = DateTime.UtcNow;
        private readonly string _currentUserLogin = "admin_son";

        public CourseManagementController()
        {
            _unitOfWork = UnitOfWork.GetInstance();
        }

        // GET: /CourseManagement/Index
        [HttpGet]
        public IActionResult Index()
        {
            System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User '{_currentUserLogin}' accessed Course Management Index.");
            try
            {
                var allCourses = _unitOfWork.Courses.GetAll()?.ToList() ?? new List<Course>();
                var allFaculties = _unitOfWork.Faculty.GetAll()?.ToList() ?? new List<Faculty>();
                var allUsers = _unitOfWork.Users.GetAll()?.ToList() ?? new List<User>(); // Needed to get faculty username

                System.Diagnostics.Debug.WriteLine($"Found {allCourses.Count} courses, {allFaculties.Count} faculty records, {allUsers.Count} users.");

                // Join to get ViewModel information
                var courseViewModels = (
                    from course in allCourses
                    join facultyMember in allFaculties
                        on course.FacultyId equals facultyMember.FacultyId // Join course with faculty
                        into facultyGroup
                    from fm in facultyGroup.DefaultIfEmpty() // Left Join in case FacultyId is incorrect
                    join user in allUsers
                        on fm?.UserId equals user.UserId // Join faculty with user to get username
                        into userGroup
                    from u in userGroup.DefaultIfEmpty() // Left Join in case user is missing
                    orderby course.CourseName // Sort by course name
                    select new CourseViewModel
                    {
                        CourseId = course.CourseId,
                        CourseName = course.CourseName,
                        Credits = course.Credits,
                        FacultyId = course.FacultyId, // Can be hidden if not needed
                        FacultyUsername = u?.Username ?? (fm != null ? "(Faculty account missing)" : "(Unassigned Faculty)") // Display username or message
                    }
                ).ToList();

                System.Diagnostics.Debug.WriteLine($"Generated {courseViewModels.Count} CourseViewModels.");
                // Add Debugging join if needed

                return View("~/Views/Admin/CourseManagement/Index.cshtml", courseViewModels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Course Index: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading course list.";
                return View("~/Views/Admin/CourseManagement/Index.cshtml", new List<CourseViewModel>());
            }
        }

        // GET: /CourseManagement/Create
        [HttpGet]
        public IActionResult Create()
        {
            LoadFacultyList(); // Prepare faculty list for dropdown
            return View("~/Views/Admin/CourseManagement/Create.cshtml");
        }

        // POST: /CourseManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CourseCreateViewModel model)
        {
            System.Diagnostics.Debug.WriteLine($"Attempting to create course '{model.CourseName}'.");
            LoadFacultyList(model.FacultyId); // Reload faculty list

            // Check if FacultyId is valid
            if (_unitOfWork.Faculty.GetById(model.FacultyId) == null)
            {
                ModelState.AddModelError(nameof(model.FacultyId), "Selected faculty is invalid.");
            }

            if (!ModelState.IsValid)
            {
                LogModelStateErrors("Create");
                TempData["ErrorMessage"] = "Invalid data.";
                return View("~/Views/Admin/CourseManagement/Create.cshtml", model);
            }

            try
            {
                // Create new CourseId (e.g., CXXX)
                string newCourseId = GenerateNewCourseId();
                if (string.IsNullOrEmpty(newCourseId)) // Check if ID creation failed
                {
                    TempData["ErrorMessage"] = "System error: Could not generate new course code.";
                    return View("~/Views/Admin/CourseManagement/Create.cshtml", model);
                }


                var newCourse = new Course
                {
                    CourseId = newCourseId, // Assign new ID
                    CourseName = model.CourseName,
                    Credits = model.Credits,
                    FacultyId = model.FacultyId
                };

                _unitOfWork.Courses.Add(newCourse);
                _unitOfWork.SaveChanges(); // Save new course

                TempData["SuccessMessage"] = $"Successfully created course '{model.CourseName}'.";
                System.Diagnostics.Debug.WriteLine($"Successfully created course '{model.CourseName}' with ID '{newCourse.CourseId}'.");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating course: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "System error when creating course.";
                return View("~/Views/Admin/CourseManagement/Create.cshtml", model);
            }
        }

        // GET: /CourseManagement/Edit/{courseId}
        [HttpGet]
        public IActionResult Edit(string id) // Receive CourseId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Invalid course code.");
            System.Diagnostics.Debug.WriteLine($"Loading edit page for Course ID '{id}'.");
            try
            {
                var course = _unitOfWork.Courses.GetById(id);
                if (course == null) { TempData["ErrorMessage"] = $"Course {id} not found."; return RedirectToAction("Index"); }

                var model = new CourseEditViewModel
                {
                    CourseId = course.CourseId,
                    CourseName = course.CourseName,
                    Credits = course.Credits,
                    FacultyId = course.FacultyId
                };

                LoadFacultyList(model.FacultyId); // Load faculty dropdown, pre-select current faculty

                return View("~/Views/Admin/CourseManagement/Edit.cshtml", model);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading edit course {id}: {ex.Message}"); TempData["ErrorMessage"] = "Error loading edit page."; return RedirectToAction("Index"); }
        }

        // POST: /CourseManagement/Edit/{courseId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, CourseEditViewModel model) // id is CourseId
        {
            if (id != model.CourseId) return BadRequest("Course code mismatch.");
            System.Diagnostics.Debug.WriteLine($"Saving changes for Course ID '{id}'.");
            LoadFacultyList(model.FacultyId); // Reload faculty list

            // Check valid FacultyId
            if (_unitOfWork.Faculty.GetById(model.FacultyId) == null) { ModelState.AddModelError(nameof(model.FacultyId), "Selected faculty is invalid."); }


            if (!ModelState.IsValid) { LogModelStateErrors(id); TempData["ErrorMessage"] = "Invalid data."; return View("~/Views/Admin/CourseManagement/Edit.cshtml", model); }

            try
            {
                var existingCourse = _unitOfWork.Courses.GetById(id);
                if (existingCourse == null) { TempData["ErrorMessage"] = $"Course {id} not found."; return RedirectToAction("Index"); }

                // Update information
                bool changed = false;
                if (existingCourse.CourseName != model.CourseName) { existingCourse.CourseName = model.CourseName; changed = true; }
                if (existingCourse.Credits != model.Credits) { existingCourse.Credits = model.Credits; changed = true; }
                if (existingCourse.FacultyId != model.FacultyId) { existingCourse.FacultyId = model.FacultyId; changed = true; }

                if (changed)
                {
                    _unitOfWork.Courses.Update(existingCourse);
                    _unitOfWork.SaveChanges(); // Save changes
                    System.Diagnostics.Debug.WriteLine($"Course {id} updated.");
                    TempData["SuccessMessage"] = $"Course '{model.CourseName}' updated successfully.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"No changes made to course '{model.CourseName}'.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "System error when updating."; System.Diagnostics.Debug.WriteLine($"Error saving course {id}: {ex.Message}"); return View("~/Views/Admin/CourseManagement/Edit.cshtml", model); }
        }

        // GET: /CourseManagement/Delete/{courseId}
        [HttpGet]
        public IActionResult Delete(string id) // Receive CourseId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Invalid course code.");
            System.Diagnostics.Debug.WriteLine($"Viewing delete confirmation for Course ID '{id}'.");
            try
            {
                var course = _unitOfWork.Courses.GetById(id);
                if (course == null) { TempData["ErrorMessage"] = $"Course {id} not found."; return RedirectToAction("Index"); }

                // Can retrieve faculty info to display if needed
                var faculty = _unitOfWork.Faculty.GetById(course.FacultyId);
                var user = (faculty != null) ? UserManagementService.GetInstance().GetUserById(faculty.UserId) : null;
                ViewBag.FacultyNameToDelete = user?.Username ?? "(Unassigned)";

                return View("~/Views/Admin/CourseManagement/Delete.cshtml", course); // Send Course entity to View
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Error loading delete page."; System.Diagnostics.Debug.WriteLine($"Error loading delete course {id}: {ex.Message}"); return RedirectToAction("Index"); }
        }

        // POST: /CourseManagement/Delete/{courseId}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id) // id is CourseId
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("Invalid course code.");
            System.Diagnostics.Debug.WriteLine($"Confirming deletion for Course ID '{id}'.");
            try
            {
                var courseToDelete = _unitOfWork.Courses.GetById(id);
                if (courseToDelete == null) { TempData["ErrorMessage"] = $"Course {id} not found."; return RedirectToAction("Index"); }

                // *** Check constraints before deleting (IMPORTANT) ***
                // Example: Check if any Enrollments are referencing this course
                var enrollmentsExist = _unitOfWork.Enrollments.GetByCourse(id).Any();
                if (enrollmentsExist)
                {
                    TempData["ErrorMessage"] = $"Cannot delete course '{courseToDelete.CourseName}' because students are enrolled.";
                    System.Diagnostics.Debug.WriteLine($"Deletion failed for course {id}: Existing enrollments found.");
                    return RedirectToAction("Index"); // Or course details page
                }

                _unitOfWork.Courses.Delete(id);
                _unitOfWork.SaveChanges();

                TempData["SuccessMessage"] = $"Course '{courseToDelete.CourseName}' deleted successfully.";
                System.Diagnostics.Debug.WriteLine($"Successfully deleted course {id}.");
                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "System error when deleting."; System.Diagnostics.Debug.WriteLine($"Error deleting course {id}: {ex.Message}"); return RedirectToAction("Index"); }
        }


        // --- Helper Methods ---
        private void LoadFacultyList(string? selectedFacultyId = null)
        {
            try
            {
                // Get list of Users who are Faculty and corresponding Faculty information
                var facultyUsers = (from user in _unitOfWork.Users.GetAll()
                                    where user.Role.Equals("Faculty", StringComparison.OrdinalIgnoreCase)
                                    join faculty in _unitOfWork.Faculty.GetAll() on user.UserId equals faculty.UserId
                                    orderby user.Username // Sort by name for easy selection
                                    select new
                                    {
                                        FacultyId = faculty.FacultyId, // Value of option
                                        DisplayText = $"{user.Username} ({faculty.FacultyId})" // Text displayed in dropdown
                                    }).ToList();

                ViewBag.FacultyList = new SelectList(facultyUsers, "FacultyId", "DisplayText", selectedFacultyId);
                System.Diagnostics.Debug.WriteLine($"Loaded {facultyUsers.Count} faculty members for dropdown.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading faculty list: {ex.Message}");
                ViewBag.FacultyList = new SelectList(new List<object>()); // Avoid null error
                TempData["ErrorMessageLoading"] = "Error loading faculty list.";
            }
        }

        // Helper to create new CourseId (example)
        private string GenerateNewCourseId()
        {
            try
            {
                var existingIds = _unitOfWork.Courses.GetAll()
                                    .Select(c => c.CourseId)
                                    .Where(id => id.StartsWith("C") && id.Length > 1 && int.TryParse(id.Substring(1), out _))
                                    .Select(id => int.Parse(id.Substring(1)))
                                    .ToList();

                int nextIdNumber = existingIds.Any() ? existingIds.Max() + 1 : 1;
                // Ensure ID has 3 digits after 'C' (e.g., C001, C010, C100)
                return $"C{nextIdNumber:D3}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating new Course ID: {ex.Message}");
                return null; // Return null if error occurs
            }
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