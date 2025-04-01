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
    public class ReportsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DateTime _currentDateTime = DateTime.UtcNow;
        private readonly string _currentUserLogin = "admin_son";

        public ReportsController()
        {
            _unitOfWork = UnitOfWork.GetInstance();
        }

        // GET: /Reports/CourseEnrollmentReport
        [HttpGet]
        public IActionResult CourseEnrollmentReport(string? semesterFilter)
        {
            string actionName = nameof(CourseEnrollmentReport);
            LogActionEntry(actionName, $"Semester filter: '{semesterFilter}'");
            List<CourseEnrollmentReportViewModel> reportViewModels = new List<CourseEnrollmentReportViewModel>();
            try
            {
                var allCourses = _unitOfWork.Courses.GetAll()?.ToList() ?? new List<Course>();
                var allEnrollments = _unitOfWork.Enrollments.GetAll()?.ToList() ?? new List<Enrollment>();
                var allFaculties = _unitOfWork.Faculty.GetAll()?.ToList() ?? new List<Faculty>();
                var allUsers = _unitOfWork.Users.GetAll()?.ToList() ?? new List<User>();

                List<Enrollment> filteredEnrollments = allEnrollments;
                if (!string.IsNullOrWhiteSpace(semesterFilter))
                {
                    filteredEnrollments = allEnrollments
                        .Where(e => e.Semester.Equals(semesterFilter, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                reportViewModels = (
                    from course in allCourses
                    join facultyMember in allFaculties on course.FacultyId equals facultyMember.FacultyId into fmGroup
                    from fm in fmGroup.DefaultIfEmpty()
                    join userFaculty in allUsers on fm?.UserId equals userFaculty.UserId into ufGroup
                    from uf in ufGroup.DefaultIfEmpty()
                    let enrollmentCount = filteredEnrollments.Count(e => e.CourseId == course.CourseId)
                    orderby course.CourseName
                    select new CourseEnrollmentReportViewModel
                    {
                        CourseId = course.CourseId,
                        CourseName = course.CourseName,
                        FacultyUsername = uf?.Username ?? "(Not assigned)",
                        Credits = course.Credits,
                        EnrollmentCount = enrollmentCount
                    }).ToList();

                LogActionSuccess(actionName, $"Generated {reportViewModels.Count} report items.");
                // Use allEnrollments to create semester list
                ViewBag.SemesterList = new SelectList(allEnrollments.Select(e => e.Semester).Distinct().OrderBy(s => s), semesterFilter);
                ViewBag.SelectedSemester = semesterFilter;
                // *** Return the correct View ***
                return View("~/Views/Admin/Reports/CourseEnrollmentReport.cshtml", reportViewModels);
            }
            catch (Exception ex)
            {
                LogActionError(actionName, ex);
                TempData["ErrorMessage"] = "Error creating course enrollment report.";
                ViewBag.SemesterList = new SelectList(Enumerable.Empty<string>());
                ViewBag.SelectedSemester = semesterFilter;
                // *** Return the correct View ***
                return View("~/Views/Admin/Reports/CourseEnrollmentReport.cshtml", reportViewModels);
            }
        }

        // GET: /Reports/GradeReport
        [HttpGet]
        public IActionResult GradeReport(string? studentFilter, string? courseFilter, string? semesterFilter)
        {
            string actionName = nameof(GradeReport);
            LogActionEntry(actionName, $"Filters: Student='{studentFilter}', Course='{courseFilter}', Semester='{semesterFilter}'");
            List<GradeReportViewModel> finalResult = new List<GradeReportViewModel>();
            try
            {
                var allGrades = _unitOfWork.Grades.GetAll()?.ToList() ?? new List<Grade>();
                var allEnrollments = _unitOfWork.Enrollments.GetAll()?.ToList() ?? new List<Enrollment>();
                var allStudents = _unitOfWork.Students.GetAll()?.ToList() ?? new List<Student>();
                var allUsers = _unitOfWork.Users.GetAll()?.ToList() ?? new List<User>();
                var allCourses = _unitOfWork.Courses.GetAll()?.ToList() ?? new List<Course>();

                var gradeViewModels = (
                     from grade in allGrades
                     join enrollment in allEnrollments on grade.EnrollmentId equals enrollment.EnrollmentId
                     join student in allStudents on enrollment.StudentId equals student.StudentId
                     join user in allUsers on student.UserId equals user.UserId
                     join course in allCourses on enrollment.CourseId equals course.CourseId
                     select new GradeReportViewModel
                     {
                         GradeId = grade.GradeId,
                         Component = grade.Component,
                         Score = grade.Score,
                         EnrollmentId = enrollment.EnrollmentId,
                         Semester = enrollment.Semester,
                         StudentId = student.StudentId,
                         StudentCode = student.StudentId,
                         StudentUsername = user.Username,
                         CourseId = course.CourseId,
                         CourseCode = course.CourseId,
                         CourseName = course.CourseName
                     }).AsQueryable();

                if (!string.IsNullOrWhiteSpace(studentFilter)) { gradeViewModels = gradeViewModels.Where(g => g.StudentCode.Contains(studentFilter, StringComparison.OrdinalIgnoreCase) || g.StudentUsername.Contains(studentFilter, StringComparison.OrdinalIgnoreCase)); }
                if (!string.IsNullOrWhiteSpace(courseFilter)) { gradeViewModels = gradeViewModels.Where(g => g.CourseCode.Contains(courseFilter, StringComparison.OrdinalIgnoreCase) || g.CourseName.Contains(courseFilter, StringComparison.OrdinalIgnoreCase)); }
                if (!string.IsNullOrWhiteSpace(semesterFilter)) { gradeViewModels = gradeViewModels.Where(g => g.Semester.Equals(semesterFilter, StringComparison.OrdinalIgnoreCase)); }

                finalResult = gradeViewModels.OrderBy(g => g.Semester).ThenBy(g => g.StudentUsername).ThenBy(g => g.CourseName).ThenBy(g => g.Component).ToList();

                LogActionSuccess(actionName, $"Generated {finalResult.Count} grade report items.");
                // Prepare ViewBag
                ViewBag.StudentList = new SelectList(allStudents.Join(allUsers, s => s.UserId, u => u.UserId, (s, u) => new { Value = s.StudentId, Text = $"{u.Username} ({s.StudentId})" }).OrderBy(x => x.Text), "Value", "Text", studentFilter);
                ViewBag.CourseList = new SelectList(allCourses.OrderBy(c => c.CourseName), "CourseId", "CourseName", courseFilter);
                ViewBag.SemesterList = new SelectList(allEnrollments.Select(e => e.Semester).Distinct().OrderBy(s => s), semesterFilter);
                ViewBag.CurrentStudentFilter = studentFilter; ViewBag.CurrentCourseFilter = courseFilter; ViewBag.CurrentSemesterFilter = semesterFilter;

                // *** Return the correct View ***
                return View("~/Views/Admin/Reports/GradeReport.cshtml", finalResult);
            }
            catch (Exception ex)
            {
                LogActionError(actionName, ex);
                TempData["ErrorMessage"] = "Error creating grade report.";
                ViewBag.StudentList = new SelectList(Enumerable.Empty<SelectListItem>()); ViewBag.CourseList = new SelectList(Enumerable.Empty<SelectListItem>()); ViewBag.SemesterList = new SelectList(Enumerable.Empty<string>());
                // *** Return the correct View ***
                return View("~/Views/Admin/Reports/GradeReport.cshtml", finalResult);
            }
        }

        // GET: /Reports/StudentProgressReport
        [HttpGet]
        public IActionResult StudentProgressReport(string? studentId)
        {
            string actionName = nameof(StudentProgressReport);
            LogActionEntry(actionName, $"Selected Student ID: '{studentId}'");
            LoadStudentList(studentId);
            var viewModel = new StudentProgressViewModel();

            if (string.IsNullOrWhiteSpace(studentId)) { LogActionInfo(actionName, "No student selected."); return View("~/Views/Admin/Reports/StudentProgressReport.cshtml", viewModel); }

            try
            {
                var student = _unitOfWork.Students.GetById(studentId);
                if (student == null) { LogActionWarning(actionName, $"Student {studentId} not found."); TempData["ErrorMessage"] = $"Student {studentId} not found."; return View("~/Views/Admin/Reports/StudentProgressReport.cshtml", viewModel); }
                var user = UserManagementService.GetInstance().GetUserById(student.UserId);
                if (user == null) { LogActionWarning(actionName, $"User {student.UserId} not found."); TempData["ErrorMessage"] = $"User error for student {studentId}."; return View("~/Views/Admin/Reports/StudentProgressReport.cshtml", viewModel); }

                viewModel.StudentId = student.StudentId; viewModel.StudentUsername = user.Username;

                var studentEnrollments = _unitOfWork.Enrollments.GetByStudent(studentId)?.ToList() ?? new List<Enrollment>();
                if (!studentEnrollments.Any()) { LogActionInfo(actionName, $"No enrollments for student {studentId}."); TempData["InfoMessage"] = $"Student '{user.Username}' has not enrolled in any courses yet."; return View("~/Views/Admin/Reports/StudentProgressReport.cshtml", viewModel); }

                var enrollmentIds = studentEnrollments.Select(e => e.EnrollmentId).ToList();
                var studentGrades = _unitOfWork.Grades.GetAll()?.Where(g => enrollmentIds.Contains(g.EnrollmentId)).ToList() ?? new List<Grade>();
                var courseIds = studentEnrollments.Select(e => e.CourseId).Distinct().ToList();
                var relevantCourses = _unitOfWork.Courses.GetAll()?.Where(c => courseIds.Contains(c.CourseId)).ToDictionary(c => c.CourseId) ?? new Dictionary<string, Course>();

                var enrollmentsBySemester = studentEnrollments.GroupBy(e => e.Semester).OrderBy(g => g.Key);

                foreach (var semesterGroup in enrollmentsBySemester)
                {
                    string semester = semesterGroup.Key;
                    var coursesInSemester = new List<CourseGradeDetailsViewModel>();
                    foreach (var enrollment in semesterGroup)
                    {
                        relevantCourses.TryGetValue(enrollment.CourseId, out Course courseInfo);
                        var gradesForEnrollment = studentGrades
                            .Where(g => g.EnrollmentId == enrollment.EnrollmentId)
                            .Select(g => new GradeComponentViewModel { GradeId = g.GradeId, Component = g.Component, Score = g.Score })
                            .OrderBy(gc => gc.Component).ToList();
                        coursesInSemester.Add(new CourseGradeDetailsViewModel
                        {
                            CourseId = enrollment.CourseId,
                            CourseCode = enrollment.CourseId,
                            CourseName = courseInfo?.CourseName ?? "(N/A)",
                            Credits = courseInfo?.Credits ?? 0,
                            EnrollmentId = enrollment.EnrollmentId,
                            Grades = gradesForEnrollment
                        });
                    }
                    viewModel.SemesterGrades.Add(semester, coursesInSemester.OrderBy(c => c.CourseName).ToList());
                }

                LogActionSuccess(actionName, $"Generated progress report for student {studentId}.");
                // *** Return the correct View ***
                return View("~/Views/Admin/Reports/StudentProgressReport.cshtml", viewModel);
            }
            catch (Exception ex) { LogActionError(actionName, ex, $"Error generating progress report for {studentId}."); TempData["ErrorMessage"] = "Error creating progress report."; return View("~/Views/Admin/Reports/StudentProgressReport.cshtml", viewModel); }
        }

        // --- Helper Methods ---
        private void LoadStudentList(string? selectedStudentId = null)
        {
            try
            {
                // --- Load Students ---
                // 1. Get data source, ensure not null
                var studentsSource = _unitOfWork.Students.GetAll() ?? Enumerable.Empty<Student>();
                var usersSource = _unitOfWork.Users.GetAll() ?? Enumerable.Empty<User>();

                // 2. Perform Join, OrderBy and ToList
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
                ViewBag.StudentList = new SelectList(studentData, "Value", "Text", selectedStudentId);
                LogActionInfo(nameof(LoadStudentList), $"Loaded {studentData.Count} students for dropdown.");
            }
            catch (Exception ex)
            {
                LogActionError(nameof(LoadStudentList), ex);
                // Provide an empty SelectList when there is an error
                ViewBag.StudentList = new SelectList(Enumerable.Empty<SelectListItem>()); // Use more explicit type
                TempData["ErrorMessageLoading"] = "Error loading student list."; // Could report more specific error
            }
        }

        private void LogActionEntry(string actionName, string? message = null) { System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] User '{_currentUserLogin}' entered {actionName}. {message}"); }
        private void LogActionSuccess(string actionName, string message) { System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] SUCCESS: {actionName} by '{_currentUserLogin}'. {message}"); }
        private void LogActionInfo(string actionName, string message) { System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] INFO: {actionName} by '{_currentUserLogin}'. {message}"); }
        private void LogActionWarning(string actionName, string message) { System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] WARNING: {actionName} by '{_currentUserLogin}'. {message}"); }
        private void LogActionError(string actionName, Exception ex, string? message = null) { System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] ERROR: {actionName} by '{_currentUserLogin}'. {message} Exception: {ex.GetType().Name} - {ex.Message}"); /* Log StackTrace to file in production */ }
        private void LogModelStateErrors(string actionName) { var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage); LogActionWarning(actionName, $"ModelState Invalid. Errors: {string.Join("; ", errors)}"); }
    }
}