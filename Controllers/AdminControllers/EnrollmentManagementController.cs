using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Services.Implementation; 

namespace SchoolManagementSystem.Controllers.AdminControllers
{
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
            List<EnrollmentViewModel> finalResult = new List<EnrollmentViewModel>(); // Khai báo ở ngoài
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
                        FacultyUsername = uf?.Username ?? "(Chưa gán GV)",
                        Semester = enrollment.Semester
                    }).AsQueryable();

                if (!string.IsNullOrWhiteSpace(studentFilter)) { enrollmentViewModels = enrollmentViewModels.Where(e => e.StudentCode.Contains(studentFilter, StringComparison.OrdinalIgnoreCase) || e.StudentUsername.Contains(studentFilter, StringComparison.OrdinalIgnoreCase)); }
                if (!string.IsNullOrWhiteSpace(courseFilter)) { enrollmentViewModels = enrollmentViewModels.Where(e => e.CourseCode.Contains(courseFilter, StringComparison.OrdinalIgnoreCase) || e.CourseName.Contains(courseFilter, StringComparison.OrdinalIgnoreCase)); }
                if (!string.IsNullOrWhiteSpace(semesterFilter)) { enrollmentViewModels = enrollmentViewModels.Where(e => e.Semester.Equals(semesterFilter, StringComparison.OrdinalIgnoreCase)); }

                finalResult = enrollmentViewModels.OrderBy(e => e.Semester).ThenBy(e => e.StudentUsername).ThenBy(e => e.CourseName).ToList(); // Gán giá trị

                // Chuẩn bị ViewBag cho filter dropdowns
                ViewBag.StudentFilterList = new SelectList(allStudents.Join(allUsers, s => s.UserId, u => u.UserId, (s, u) => new { s.StudentId, Display = $"{u.Username} ({s.StudentId})" }).OrderBy(x => x.Display), "StudentId", "Display", studentFilter);
                ViewBag.CourseFilterList = new SelectList(allCourses.OrderBy(c => c.CourseName), "CourseId", "CourseName", courseFilter);
                ViewBag.SemesterFilterList = new SelectList(allEnrollments.Select(e => e.Semester).Distinct().OrderBy(s => s), semesterFilter);

                LogActionSuccess(actionName, $"Displayed {finalResult.Count} enrollments.");
                return View("~/Views/Admin/EnrollmentManagement/Index.cshtml", finalResult);
            }
            catch (Exception ex)
            {
                LogActionError(actionName, ex);
                TempData["ErrorMessage"] = "Lỗi tải danh sách đăng ký.";
                // Chuẩn bị ViewBag rỗng khi lỗi
                ViewBag.StudentFilterList = new SelectList(Enumerable.Empty<object>());
                ViewBag.CourseFilterList = new SelectList(Enumerable.Empty<object>());
                ViewBag.SemesterFilterList = new SelectList(Enumerable.Empty<string>());
                return View("~/Views/Admin/EnrollmentManagement/Index.cshtml", finalResult); // Trả về list rỗng
            }
        }

        // GET: /EnrollmentManagement/Create
        [HttpGet]
        public IActionResult Create()
        {
            LogActionEntry(nameof(Create) + " (GET)");
            LoadStudentAndCourseLists(); // Load dropdowns
            return View("~/Views/Admin/EnrollmentManagement/Create.cshtml", new EnrollmentCreateViewModel()); // Gửi model rỗng
        }

        // POST: /EnrollmentManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(EnrollmentCreateViewModel model)
        {
            string actionName = nameof(Create) + " (POST)";
            LogActionEntry(actionName, $"Attempting enroll S:{model.StudentId} C:{model.CourseId} Sem:{model.Semester}");
            LoadStudentAndCourseLists(model.StudentId, model.CourseId); // Load lại dropdowns

            // Server-side validation
            var studentExists = _unitOfWork.Students.GetById(model.StudentId) != null;
            var courseExists = _unitOfWork.Courses.GetById(model.CourseId) != null;
            if (!studentExists) ModelState.AddModelError(nameof(model.StudentId), "Sinh viên không tồn tại.");
            if (!courseExists) ModelState.AddModelError(nameof(model.CourseId), "Khóa học không tồn tại.");
            bool alreadyEnrolled = _unitOfWork.Enrollments.GetAll().Any(e => e.StudentId == model.StudentId && e.CourseId == model.CourseId && e.Semester.Equals(model.Semester?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (alreadyEnrolled) { ModelState.AddModelError(string.Empty, $"Sinh viên đã đăng ký khóa học này trong học kỳ {model.Semester}."); }

            if (!ModelState.IsValid)
            {
                LogModelStateErrors(actionName);
                TempData["ErrorMessage"] = "Thông tin không hợp lệ.";
                return View("~/Views/Admin/EnrollmentManagement/Create.cshtml", model);
            }

            try
            {
                string newEnrollmentId = GenerateNewEnrollmentId();
                if (string.IsNullOrEmpty(newEnrollmentId)) { TempData["ErrorMessage"] = "Lỗi tạo mã đăng ký."; return View("~/Views/Admin/EnrollmentManagement/Create.cshtml", model); }

                var newEnrollment = new Enrollment
                {
                    EnrollmentId = newEnrollmentId,
                    StudentId = model.StudentId,
                    CourseId = model.CourseId,
                    Semester = model.Semester.Trim()
                };
                _unitOfWork.Enrollments.Add(newEnrollment);
                _unitOfWork.SaveChanges();

                TempData["SuccessMessage"] = $"Đã đăng ký thành công SV '{_unitOfWork.Users.GetById(_unitOfWork.Students.GetById(model.StudentId)?.UserId)?.Username}' vào KH '{_unitOfWork.Courses.GetById(model.CourseId)?.CourseName}' cho học kỳ {model.Semester}.";
                LogActionSuccess(actionName, $"Created enrollment '{newEnrollment.EnrollmentId}'.");
                return RedirectToAction("Index");
            }
            catch (Exception ex) { LogActionError(actionName, ex); TempData["ErrorMessage"] = "Lỗi hệ thống khi tạo đăng ký."; return View("~/Views/Admin/EnrollmentManagement/Create.cshtml", model); }
        }

        // GET: /EnrollmentManagement/Delete/{enrollmentId}
        [HttpGet]
        public IActionResult Delete(string id) // Nhận EnrollmentId
        {
            string actionName = nameof(Delete) + " (GET)";
            LogActionEntry(actionName, $"Viewing delete confirmation for Enrollment ID '{id}'.");
            if (string.IsNullOrWhiteSpace(id)) { LogActionWarning(actionName, "ID is null."); return NotFound(); }
            EnrollmentViewModel? deleteInfo = null; // Khai báo ở ngoài
            try
            {
                var enrollment = _unitOfWork.Enrollments.GetById(id);
                if (enrollment == null) { LogActionWarning(actionName, $"Enrollment {id} not found."); TempData["ErrorMessage"] = $"Không tìm thấy đăng ký {id}."; return RedirectToAction("Index"); }

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
            catch (Exception ex) { LogActionError(actionName, ex, $"Error loading enrollment {id} for delete."); TempData["ErrorMessage"] = "Lỗi tải trang xóa."; return RedirectToAction("Index"); }
        }

        // POST: /EnrollmentManagement/Delete/{enrollmentId}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id) // id là EnrollmentId
        {
            string actionName = nameof(DeleteConfirmed) + " (POST)";
            LogActionEntry(actionName, $"Confirming deletion for Enrollment ID '{id}'.");
            if (string.IsNullOrWhiteSpace(id)) { LogActionWarning(actionName, "ID is null."); return BadRequest(); }
            try
            {
                var enrollmentToDelete = _unitOfWork.Enrollments.GetById(id);
                if (enrollmentToDelete == null) { LogActionWarning(actionName, $"Enrollment {id} not found."); TempData["ErrorMessage"] = $"Không tìm thấy đăng ký {id}."; return RedirectToAction("Index"); }

                bool hasGrades = _unitOfWork.Grades.GetByEnrollment(id).Any();
                if (hasGrades) { LogActionWarning(actionName, $"Deletion failed for enrollment {id}: Grades found."); TempData["ErrorMessage"] = "Không thể hủy đăng ký này vì đã có điểm."; return RedirectToAction("Index"); }

                _unitOfWork.Enrollments.Delete(id); _unitOfWork.SaveChanges();
                TempData["SuccessMessage"] = $"Đã hủy đăng ký (Mã: {id}).";
                LogActionSuccess(actionName, $"Deleted enrollment {id}.");
                return RedirectToAction("Index");
            }
            catch (Exception ex) { LogActionError(actionName, ex, $"Error deleting enrollment {id}."); TempData["ErrorMessage"] = "Lỗi hệ thống khi hủy đăng ký."; return RedirectToAction("Index"); }
        }

        // --- Helper Methods ---
        private void LoadStudentAndCourseLists(string? selectedStudentId = null, string? selectedCourseId = null)
        {
            try
            {
                // --- Load Students ---
                // 1. Lấy nguồn dữ liệu, đảm bảo không null
                var studentsSource = _unitOfWork.Students.GetAll() ?? Enumerable.Empty<Student>();
                var usersSource = _unitOfWork.Users.GetAll() ?? Enumerable.Empty<User>();

                // 2. Thực hiện Join, OrderBy và ToList để có List<> cụ thể (không phải anonymous nữa nếu có thể)
                //    Hoặc giữ anonymous nhưng xử lý khác
                var studentData = studentsSource
                                    .Join(usersSource,
                                          s => s.UserId, u => u.UserId,
                                          (s, u) => new // Tạo anonymous type
                                          {
                                              Value = s.StudentId,
                                              Text = $"{u.Username} ({s.StudentId})"
                                          })
                                    .OrderBy(x => x.Text)
                                    .ToList(); // studentData giờ là List<AnonymousType>, không null

                // 3. Tạo SelectList trực tiếp từ danh sách đã ToList()
                //    Nếu studentData rỗng, SelectList sẽ tự động rỗng.
                ViewBag.StudentList = new SelectList(studentData, "Value", "Text", selectedStudentId);
                LogActionInfo(nameof(LoadStudentAndCourseLists), $"Loaded {studentData.Count} students for dropdown.");


                // --- Load Courses ---
                // 1. Lấy nguồn dữ liệu, đảm bảo không null
                var coursesSource = _unitOfWork.Courses.GetAll() ?? Enumerable.Empty<Course>();

                // 2. OrderBy và ToList
                var courseData = coursesSource.OrderBy(c => c.CourseName).ToList(); // courseData là List<Course>, không null

                // 3. Tạo SelectList trực tiếp
                ViewBag.CourseList = new SelectList(courseData, "CourseId", "CourseName", selectedCourseId);
                LogActionInfo(nameof(LoadStudentAndCourseLists), $"Loaded {courseData.Count} courses for dropdown.");
            }
            catch (Exception ex)
            {
                LogActionError(nameof(LoadStudentAndCourseLists), ex);
                // Cung cấp SelectList rỗng khi có lỗi
                ViewBag.StudentList = new SelectList(Enumerable.Empty<SelectListItem>()); // Dùng kiểu rõ ràng hơn
                ViewBag.CourseList = new SelectList(Enumerable.Empty<SelectListItem>());
                TempData["ErrorMessageLoading"] = "Lỗi tải DS SV/KH.";
            }
        }

        // *** Đảm bảo hàm GenerateNewEnrollmentId tồn tại và đúng tên ***
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