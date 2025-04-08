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
    public class EnrollmentManagementController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DateTime _currentDateTime = DateTime.UtcNow;
        private readonly string _currentUserLogin = "admin_son";

        public EnrollmentManagementController()
        {
            _unitOfWork = UnitOfWork.GetInstance();
        }

        // GET: /EnrollmentManagement/Index
        [HttpGet]
        public IActionResult Index(string? studentFilter, string? courseFilter, string? semesterFilter)
        {
            string actionName = nameof(Index);
            LogActionEntry(actionName, $"Filters: Student='{studentFilter}', Course='{courseFilter}', Semester='{semesterFilter}'");
            List<EnrollmentViewModel> finalResult = new List<EnrollmentViewModel>(); // Declaration outside
            try
            {
                var allEnrollments = _unitOfWork.Enrollments.GetAll()?.ToList() ?? new List<Enrollment>();
                var allStudents = _unitOfWork.Students.GetAll()?.ToList() ?? new List<Student>();
                var allUsers = _unitOfWork.Users.GetAll()?.ToList() ?? new List<User>();
                var allCourses = _unitOfWork.Courses.GetAll()?.ToList() ?? new List<Course>();
                var allFaculties = _unitOfWork.Faculty.GetAll()?.ToList() ?? new List<Faculty>();

                var enrollmentViewModels = (
                    from enrollment in allEnrollments
                    join student in allStudents on enrollment.StudentId equals student.StudentId
                    join userStudent in allUsers on student.UserId equals userStudent.UserId
                    join course in allCourses on enrollment.CourseId equals course.CourseId
                    join facultyMember in allFaculties on course.FacultyId equals facultyMember.FacultyId into fmGroup
                    from fm in fmGroup.DefaultIfEmpty()
                    join userFaculty in allUsers on fm?.UserId equals userFaculty.UserId into ufGroup
                    from uf in ufGroup.DefaultIfEmpty()
                    select new EnrollmentViewModel
                    {
                        EnrollmentId = enrollment.EnrollmentId,
                        StudentId = student.StudentId,
                        StudentCode = student.StudentId,
                        StudentUsername = userStudent.Username,
                        CourseId = course.CourseId,
                        CourseCode = course.CourseId,
                        CourseName = course.CourseName,
                        Credits = course.Credits,
                        FacultyUsername = uf?.Username ?? "(Unassigned Faculty)",
                        Semester = enrollment.Semester
                    }).AsQueryable();

                if (!string.IsNullOrWhiteSpace(studentFilter)) { enrollmentViewModels = enrollmentViewModels.Where(e => e.StudentCode.Contains(studentFilter, StringComparison.OrdinalIgnoreCase) || e.StudentUsername.Contains(studentFilter, StringComparison.OrdinalIgnoreCase)); }
                if (!string.IsNullOrWhiteSpace(courseFilter)) { enrollmentViewModels = enrollmentViewModels.Where(e => e.CourseCode.Contains(courseFilter, StringComparison.OrdinalIgnoreCase) || e.CourseName.Contains(courseFilter, StringComparison.OrdinalIgnoreCase)); }
                if (!string.IsNullOrWhiteSpace(semesterFilter)) { enrollmentViewModels = enrollmentViewModels.Where(e => e.Semester.Equals(semesterFilter, StringComparison.OrdinalIgnoreCase)); }

                finalResult = enrollmentViewModels.OrderBy(e => e.Semester).ThenBy(e => e.StudentUsername).ThenBy(e => e.CourseName).ToList(); // Assign value

                // Prepare ViewBag for filter dropdowns
                ViewBag.StudentFilterList = new SelectList(allStudents.Join(allUsers, s => s.UserId, u => u.UserId, (s, u) => new { s.StudentId, Display = $"{u.Username} ({s.StudentId})" }).OrderBy(x => x.Display), "StudentId", "Display", studentFilter);
                ViewBag.CourseFilterList = new SelectList(allCourses.OrderBy(c => c.CourseName), "CourseId", "CourseName", courseFilter);
                ViewBag.SemesterFilterList = new SelectList(allEnrollments.Select(e => e.Semester).Distinct().OrderBy(s => s), semesterFilter);

                LogActionSuccess(actionName, $"Displayed {finalResult.Count} enrollments.");
                return View("~/Views/Admin/EnrollmentManagement/Index.cshtml", finalResult);
            }
            catch (Exception ex)
            {
                LogActionError(actionName, ex);
                TempData["ErrorMessage"] = "Error loading enrollment list.";
                // Prepare empty ViewBag on error
                ViewBag.StudentFilterList = new SelectList(Enumerable.Empty<object>());
                ViewBag.CourseFilterList = new SelectList(Enumerable.Empty<object>());
                ViewBag.SemesterFilterList = new SelectList(Enumerable.Empty<string>());
                return View("~/Views/Admin/EnrollmentManagement/Index.cshtml", finalResult); // Return empty list
            }
        }

        // GET: /EnrollmentManagement/Create
        [HttpGet]
        public IActionResult Create()
        {
            LogActionEntry(nameof(Create) + " (GET)");
            LoadStudentAndCourseLists(); // Load dropdowns
            return View("~/Views/Admin/EnrollmentManagement/Create.cshtml", new EnrollmentCreateViewModel()); // Send empty model
        }

        // POST: /EnrollmentManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(EnrollmentCreateViewModel model)
        {
            string actionName = nameof(Create) + " (POST)";
            LogActionEntry(actionName, $"Attempting enroll S:{model.StudentId} C:{model.CourseId} Sem:{model.Semester}");
            LoadStudentAndCourseLists(model.StudentId, model.CourseId); // Reload dropdowns

            // Server-side validation
            var studentExists = _unitOfWork.Students.GetById(model.StudentId) != null;
            var courseExists = _unitOfWork.Courses.GetById(model.CourseId) != null;
            if (!studentExists) ModelState.AddModelError(nameof(model.StudentId), "Student does not exist.");
            if (!courseExists) ModelState.AddModelError(nameof(model.CourseId), "Course does not exist.");
            bool alreadyEnrolled = _unitOfWork.Enrollments.GetAll().Any(e => e.StudentId == model.StudentId && e.CourseId == model.CourseId && e.Semester.Equals(model.Semester?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (alreadyEnrolled) { ModelState.AddModelError(string.Empty, $"Student is already enrolled in this course for semester {model.Semester}."); }

            if (!ModelState.IsValid)
            {
                LogModelStateErrors(actionName);
                TempData["ErrorMessage"] = "Invalid information.";
                return View("~/Views/Admin/EnrollmentManagement/Create.cshtml", model);
            }

            try
            {
                string newEnrollmentId = GenerateNewEnrollmentId();
                if (string.IsNullOrEmpty(newEnrollmentId)) { TempData["ErrorMessage"] = "Error creating enrollment ID."; return View("~/Views/Admin/EnrollmentManagement/Create.cshtml", model); }

                var newEnrollment = new Enrollment
                {
                    EnrollmentId = newEnrollmentId,
                    StudentId = model.StudentId,
                    CourseId = model.CourseId,
                    Semester = model.Semester.Trim()
                };
                _unitOfWork.Enrollments.Add(newEnrollment);
                _unitOfWork.SaveChanges();

                TempData["SuccessMessage"] = $"Successfully enrolled student '{_unitOfWork.Users.GetById(_unitOfWork.Students.GetById(model.StudentId)?.UserId)?.Username}' in course '{_unitOfWork.Courses.GetById(model.CourseId)?.CourseName}' for semester {model.Semester}.";
                LogActionSuccess(actionName, $"Created enrollment '{newEnrollment.EnrollmentId}'.");
                return RedirectToAction("Index");
            }
            catch (Exception ex) { LogActionError(actionName, ex); TempData["ErrorMessage"] = "System error while creating enrollment."; return View("~/Views/Admin/EnrollmentManagement/Create.cshtml", model); }
        }

        // GET: /EnrollmentManagement/Delete/{enrollmentId}
        [HttpGet]
        public IActionResult Delete(string id) // Receive EnrollmentId
        {
            string actionName = nameof(Delete) + " (GET)";
            LogActionEntry(actionName, $"Viewing delete confirmation for Enrollment ID '{id}'.");
            if (string.IsNullOrWhiteSpace(id)) { LogActionWarning(actionName, "ID is null."); return NotFound(); }
            EnrollmentViewModel? deleteInfo = null; // Declaration outside
            try
            {
                var enrollment = _unitOfWork.Enrollments.GetById(id);
                if (enrollment == null) { LogActionWarning(actionName, $"Enrollment {id} not found."); TempData["ErrorMessage"] = $"Enrollment {id} not found."; return RedirectToAction("Index"); }

                var student = _unitOfWork.Students.GetById(enrollment.StudentId);
                var user = (student != null) ? UserManagementService.GetInstance().GetUserById(student.UserId) : null;
                var course = _unitOfWork.Courses.GetById(enrollment.CourseId);

                deleteInfo = new EnrollmentViewModel
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    StudentUsername = user?.Username ?? "N/A",
                    StudentCode = student?.StudentId ?? "N/A",
                    CourseName = course?.CourseName ?? "N/A",
                    CourseCode = course?.CourseId ?? "N/A",
                    Semester = enrollment.Semester,
                    StudentId = enrollment.StudentId,
                    CourseId = enrollment.CourseId
                };

                return View("~/Views/Admin/EnrollmentManagement/Delete.cshtml", deleteInfo);
            }
            catch (Exception ex) { LogActionError(actionName, ex, $"Error loading enrollment {id} for delete."); TempData["ErrorMessage"] = "Error loading delete page."; return RedirectToAction("Index"); }
        }

        // POST: /EnrollmentManagement/Delete/{enrollmentId}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id) // id is EnrollmentId
        {
            string actionName = nameof(DeleteConfirmed) + " (POST)";
            LogActionEntry(actionName, $"Confirming deletion for Enrollment ID '{id}'.");
            if (string.IsNullOrWhiteSpace(id)) { LogActionWarning(actionName, "ID is null."); return BadRequest(); }
            try
            {
                var enrollmentToDelete = _unitOfWork.Enrollments.GetById(id);
                if (enrollmentToDelete == null) { LogActionWarning(actionName, $"Enrollment {id} not found."); TempData["ErrorMessage"] = $"Enrollment {id} not found."; return RedirectToAction("Index"); }

                bool hasGrades = _unitOfWork.Grades.GetByEnrollment(id).Any();
                if (hasGrades) { LogActionWarning(actionName, $"Deletion failed for enrollment {id}: Grades found."); TempData["ErrorMessage"] = "Cannot delete this enrollment because grades are already assigned."; return RedirectToAction("Index"); }

                _unitOfWork.Enrollments.Delete(id); _unitOfWork.SaveChanges();
                TempData["SuccessMessage"] = $"Enrollment deleted successfully (ID: {id}).";
                LogActionSuccess(actionName, $"Deleted enrollment {id}.");
                return RedirectToAction("Index");
            }
            catch (Exception ex) { LogActionError(actionName, ex, $"Error deleting enrollment {id}."); TempData["ErrorMessage"] = "System error while deleting enrollment."; return RedirectToAction("Index"); }
        }

        // --- Helper Methods ---
        private void LoadStudentAndCourseLists(string? selectedStudentId = null, string? selectedCourseId = null)
        {
            try
            {
                // --- Load Students ---
                // 1. Get data source, ensure not null
                var studentsSource = _unitOfWork.Students.GetAll() ?? Enumerable.Empty<Student>();
                var usersSource = _unitOfWork.Users.GetAll() ?? Enumerable.Empty<User>();

                // 2. Perform Join, OrderBy and ToList to get concrete List<> (not anonymous anymore if possible)
                //    Or keep anonymous but handle differently
                var studentData = studentsSource
                                    .Join(usersSource,
                                          s => s.UserId, u => u.UserId,
                                          (s, u) => new // Create anonymous type
                                          {
                                              Value = s.StudentId,
                                              Text = $"{u.Username} ({s.StudentId})"
                                          })
                                    .OrderBy(x => x.Text)
                                    .ToList(); // studentData is now List<AnonymousType>, not null

                // 3. Create SelectList directly from the ToList() list
                //    If studentData is empty, SelectList will automatically be empty.
                ViewBag.StudentList = new SelectList(studentData, "Value", "Text", selectedStudentId);
                LogActionInfo(nameof(LoadStudentAndCourseLists), $"Loaded {studentData.Count} students for dropdown.");


                // --- Load Courses ---
                // 1. Get data source, ensure not null
                var coursesSource = _unitOfWork.Courses.GetAll() ?? Enumerable.Empty<Course>();

                // 2. OrderBy and ToList
                var courseData = coursesSource.OrderBy(c => c.CourseName).ToList(); // courseData is List<Course>, not null

                // 3. Create SelectList directly
                ViewBag.CourseList = new SelectList(courseData, "CourseId", "CourseName", selectedCourseId);
                LogActionInfo(nameof(LoadStudentAndCourseLists), $"Loaded {courseData.Count} courses for dropdown.");
            }
            catch (Exception ex)
            {
                LogActionError(nameof(LoadStudentAndCourseLists), ex);
                // Provide empty SelectList when error occurs
                ViewBag.StudentList = new SelectList(Enumerable.Empty<SelectListItem>()); // Use more explicit type
                ViewBag.CourseList = new SelectList(Enumerable.Empty<SelectListItem>());
                TempData["ErrorMessageLoading"] = "Error loading Student/Course lists.";
            }
        }

        // *** Ensure GenerateNewEnrollmentId function exists and has correct name ***
        private string GenerateNewEnrollmentId()
        {
            try
            {
                var existingIds = _unitOfWork.Enrollments.GetAll()?.Select(e => e.EnrollmentId).Where(id => id.StartsWith("E") && id.Length > 1 && int.TryParse(id.Substring(1), out _)).Select(id => int.Parse(id.Substring(1))).ToList() ?? new List<int>();
                int nextIdNumber = existingIds.Any() ? existingIds.Max() + 1 : 1;
                return $"E{nextIdNumber:D3}";
            }
            catch (Exception ex) { LogActionError(nameof(GenerateNewEnrollmentId), ex); return $"E{Guid.NewGuid().ToString().Substring(0, 5)}"; }
        }


        private void LogModelStateErrors(string context) { var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage); LogActionWarning(context, $"ModelState Invalid. Errors: {string.Join("; ", errors)}"); }
        private void LogActionEntry(string actionName, string? message = null) { System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:T}] User '{_currentUserLogin}' entered {actionName}. {message}"); }
        private void LogActionSuccess(string actionName, string message) { System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:T}] SUCCESS: {actionName} by '{_currentUserLogin}'. {message}"); }
        private void LogActionInfo(string actionName, string message) { System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:T}] INFO: {actionName} by '{_currentUserLogin}'. {message}"); }
        private void LogActionWarning(string actionName, string message) { System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:T}] WARNING: {actionName} by '{_currentUserLogin}'. {message}"); }
        private void LogActionError(string actionName, Exception ex, string? message = null) { System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:T}] ERROR: {actionName} by '{_currentUserLogin}'. {message} Exception: {ex.Message}"); }
    }
}