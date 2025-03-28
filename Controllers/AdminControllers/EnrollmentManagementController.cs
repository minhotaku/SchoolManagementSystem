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
        // Hiển thị danh sách tất cả đăng ký, có thể filter theo SV, Khóa học, Học kỳ
        [HttpGet]
        public IActionResult Index(string? studentFilter, string? courseFilter, string? semesterFilter)
        {
            System.Diagnostics.Debug.WriteLine($"Accessed Enrollment Index. Filters: Student='{studentFilter}', Course='{courseFilter}', Semester='{semesterFilter}'");
            try
            {
                // Lấy tất cả dữ liệu cần thiết
                var allEnrollments = _unitOfWork.Enrollments.GetAll()?.ToList() ?? new List<Enrollment>();
                var allStudents = _unitOfWork.Students.GetAll()?.ToList() ?? new List<Student>();
                var allUsers = _unitOfWork.Users.GetAll()?.ToList() ?? new List<User>();
                var allCourses = _unitOfWork.Courses.GetAll()?.ToList() ?? new List<Course>();
                var allFaculties = _unitOfWork.Faculty.GetAll()?.ToList() ?? new List<Faculty>(); // Để lấy tên GV của khóa học

                // Join dữ liệu để tạo ViewModel
                var enrollmentViewModels = (
                    from enrollment in allEnrollments
                    join student in allStudents on enrollment.StudentId equals student.StudentId
                    join userStudent in allUsers on student.UserId equals userStudent.UserId // User của Student
                    join course in allCourses on enrollment.CourseId equals course.CourseId
                    join facultyMember in allFaculties on course.FacultyId equals facultyMember.FacultyId into fmGroup
                    from fm in fmGroup.DefaultIfEmpty() // Left join Faculty
                    join userFaculty in allUsers on fm?.UserId equals userFaculty.UserId into ufGroup
                    from uf in ufGroup.DefaultIfEmpty() // Left join User của Faculty
                    select new EnrollmentViewModel
                    {
                        EnrollmentId = enrollment.EnrollmentId,
                        StudentId = student.StudentId,
                        StudentCode = student.StudentId, // Giả sử Mã SV là StudentId
                        StudentUsername = userStudent.Username,
                        CourseId = course.CourseId,
                        CourseCode = course.CourseId, // Giả sử Mã KH là CourseId
                        CourseName = course.CourseName,
                        Credits = course.Credits,
                        FacultyUsername = uf?.Username ?? "(Chưa gán GV)",
                        Semester = enrollment.Semester
                    }
                ).AsQueryable(); // Dùng AsQueryable để dễ filter

                // --- Áp dụng Filter ---
                if (!string.IsNullOrEmpty(studentFilter))
                {
                    // Tìm kiếm gần đúng trong tên hoặc mã SV
                    enrollmentViewModels = enrollmentViewModels.Where(e => e.StudentCode.Contains(studentFilter, StringComparison.OrdinalIgnoreCase) ||
                                                                       e.StudentUsername.Contains(studentFilter, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrEmpty(courseFilter))
                {
                    // Tìm kiếm gần đúng trong tên hoặc mã KH
                    enrollmentViewModels = enrollmentViewModels.Where(e => e.CourseCode.Contains(courseFilter, StringComparison.OrdinalIgnoreCase) ||
                                                                       e.CourseName.Contains(courseFilter, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrEmpty(semesterFilter))
                {
                    enrollmentViewModels = enrollmentViewModels.Where(e => e.Semester.Equals(semesterFilter, StringComparison.OrdinalIgnoreCase));
                }

                var finalResult = enrollmentViewModels.OrderBy(e => e.Semester).ThenBy(e => e.StudentUsername).ThenBy(e => e.CourseName).ToList();

                // Chuẩn bị danh sách cho các dropdown filter (nếu cần)
                ViewBag.StudentFilterList = new SelectList(allStudents.Join(allUsers, s => s.UserId, u => u.UserId, (s, u) => new { s.StudentId, Display = $"{u.Username} ({s.StudentId})" }).OrderBy(x => x.Display), "StudentId", "Display");
                ViewBag.CourseFilterList = new SelectList(allCourses.OrderBy(c => c.CourseName), "CourseId", "CourseName");
                ViewBag.SemesterFilterList = new SelectList(allEnrollments.Select(e => e.Semester).Distinct().OrderBy(s => s));


                return View("~/Views/Admin/EnrollmentManagement/Index.cshtml", finalResult);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Enrollment Index: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Lỗi tải danh sách đăng ký.";
                return View("~/Views/Admin/EnrollmentManagement/Index.cshtml", new List<EnrollmentViewModel>());
            }
        }

        // GET: /EnrollmentManagement/Create
        [HttpGet]
        public IActionResult Create()
        {
            LoadStudentAndCourseLists(); // Load dropdowns
            return View("~/Views/Admin/EnrollmentManagement/Create.cshtml");
        }

        // POST: /EnrollmentManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(EnrollmentCreateViewModel model)
        {
            System.Diagnostics.Debug.WriteLine($"Attempting to enroll Student '{model.StudentId}' into Course '{model.CourseId}' for Semester '{model.Semester}'.");
            LoadStudentAndCourseLists(model.StudentId, model.CourseId); // Load lại dropdowns

            // Kiểm tra StudentId và CourseId có hợp lệ không
            var studentExists = _unitOfWork.Students.GetById(model.StudentId) != null;
            var courseExists = _unitOfWork.Courses.GetById(model.CourseId) != null;

            if (!studentExists) ModelState.AddModelError(nameof(model.StudentId), "Sinh viên không tồn tại.");
            if (!courseExists) ModelState.AddModelError(nameof(model.CourseId), "Khóa học không tồn tại.");

            // Kiểm tra xem sinh viên đã đăng ký khóa học này trong học kỳ này chưa
            bool alreadyEnrolled = _unitOfWork.Enrollments.GetAll()
                                    .Any(e => e.StudentId == model.StudentId &&
                                              e.CourseId == model.CourseId &&
                                              e.Semester.Equals(model.Semester, StringComparison.OrdinalIgnoreCase));
            if (alreadyEnrolled)
            {
                ModelState.AddModelError(string.Empty, $"Sinh viên đã đăng ký khóa học này trong học kỳ {model.Semester}.");
            }


            if (!ModelState.IsValid)
            {
                LogModelStateErrors("CreateEnrollment");
                TempData["ErrorMessage"] = "Thông tin không hợp lệ.";
                return View("~/Views/Admin/EnrollmentManagement/Create.cshtml", model);
            }

            try
            {
                var newEnrollment = new Enrollment
                {
                    EnrollmentId = GenerateNewEnrollmentId(), // Tạo ID mới
                    StudentId = model.StudentId,
                    CourseId = model.CourseId,
                    Semester = model.Semester.Trim() // Trim để loại bỏ khoảng trắng thừa
                };

                _unitOfWork.Enrollments.Add(newEnrollment);
                _unitOfWork.SaveChanges();

                TempData["SuccessMessage"] = $"Đã đăng ký thành công sinh viên vào khóa học cho học kỳ {model.Semester}.";
                System.Diagnostics.Debug.WriteLine($"Successfully created enrollment '{newEnrollment.EnrollmentId}'.");
                return RedirectToAction("Index"); // Chuyển về trang danh sách
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating enrollment: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Lỗi hệ thống khi tạo đăng ký.";
                return View("~/Views/Admin/EnrollmentManagement/Create.cshtml", model);
            }
        }

        // GET: /EnrollmentManagement/Delete/{enrollmentId}
        [HttpGet]
        public IActionResult Delete(string id) // Nhận EnrollmentId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Mã đăng ký không hợp lệ.");
            System.Diagnostics.Debug.WriteLine($"Viewing delete confirmation for Enrollment ID '{id}'.");
            try
            {
                var enrollment = _unitOfWork.Enrollments.GetById(id);
                if (enrollment == null) { TempData["ErrorMessage"] = $"Không tìm thấy đăng ký {id}."; return RedirectToAction("Index"); }

                // Lấy thông tin để hiển thị xác nhận
                var student = _unitOfWork.Students.GetById(enrollment.StudentId);
                var user = (student != null) ? UserManagementService.GetInstance().GetUserById(student.UserId) : null;
                var course = _unitOfWork.Courses.GetById(enrollment.CourseId);

                // Tạo ViewModel tạm để gửi thông tin sang View Delete (hoặc dùng ViewBag)
                var deleteInfo = new EnrollmentViewModel
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    StudentUsername = user?.Username ?? "N/A",
                    CourseName = course?.CourseName ?? "N/A",
                    Semester = enrollment.Semester,
                    StudentId = enrollment.StudentId, // Gửi kèm để dùng nếu cần
                    CourseId = enrollment.CourseId   // Gửi kèm để dùng nếu cần
                };

                return View("~/Views/Admin/EnrollmentManagement/Delete.cshtml", deleteInfo);
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Lỗi tải trang xóa."; System.Diagnostics.Debug.WriteLine($"Error loading delete enrollment {id}: {ex.Message}"); return RedirectToAction("Index"); }
        }

        // POST: /EnrollmentManagement/Delete/{enrollmentId}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id) // id là EnrollmentId
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("Mã đăng ký không hợp lệ.");
            System.Diagnostics.Debug.WriteLine($"Confirming deletion for Enrollment ID '{id}'.");
            try
            {
                var enrollmentToDelete = _unitOfWork.Enrollments.GetById(id);
                if (enrollmentToDelete == null) { TempData["ErrorMessage"] = $"Không tìm thấy đăng ký {id}."; return RedirectToAction("Index"); }

                // *** Kiểm tra ràng buộc: Có điểm cho đăng ký này chưa? ***
                bool hasGrades = _unitOfWork.Grades.GetByEnrollment(id).Any();
                if (hasGrades)
                {
                    TempData["ErrorMessage"] = $"Không thể hủy đăng ký này vì đã có điểm được nhập.";
                    System.Diagnostics.Debug.WriteLine($"Deletion failed for enrollment {id}: Existing grades found.");
                    return RedirectToAction("Index"); // Hoặc trang chi tiết
                }

                _unitOfWork.Enrollments.Delete(id);
                _unitOfWork.SaveChanges();

                TempData["SuccessMessage"] = $"Đã hủy đăng ký thành công (Mã: {id}).";
                System.Diagnostics.Debug.WriteLine($"Successfully deleted enrollment {id}.");
                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Lỗi hệ thống khi hủy đăng ký."; System.Diagnostics.Debug.WriteLine($"Error deleting enrollment {id}: {ex.Message}"); return RedirectToAction("Index"); }
        }


        // --- Helper Methods ---
        private void LoadStudentAndCourseLists(string? selectedStudentId = null, string? selectedCourseId = null)
        {
            try
            {
                var students = _unitOfWork.Students.GetAll()
                                .Join(_unitOfWork.Users.GetAll(), s => s.UserId, u => u.UserId, (s, u) => new { s.StudentId, Display = $"{u.Username} ({s.StudentId})" })
                                .OrderBy(x => x.Display).ToList();
                ViewBag.StudentList = new SelectList(students, "StudentId", "Display", selectedStudentId);

                var courses = _unitOfWork.Courses.GetAll()?.OrderBy(c => c.CourseName).ToList() ?? new List<Course>();
                ViewBag.CourseList = new SelectList(courses, "CourseId", "CourseName", selectedCourseId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading student/course lists: {ex.Message}");
                ViewBag.StudentList = new SelectList(new List<object>());
                ViewBag.CourseList = new SelectList(new List<Course>());
                TempData["ErrorMessageLoading"] = "Lỗi tải danh sách sinh viên hoặc khóa học.";
            }
        }

        private string GenerateNewEnrollmentId()
        {
            try
            {
                var existingIds = _unitOfWork.Enrollments.GetAll()
                                    .Select(e => e.EnrollmentId)
                                    .Where(id => id.StartsWith("E") && id.Length > 1 && int.TryParse(id.Substring(1), out _))
                                    .Select(id => int.Parse(id.Substring(1)))
                                    .ToList();
                int nextIdNumber = existingIds.Any() ? existingIds.Max() + 1 : 1;
                return $"E{nextIdNumber:D3}"; // Ví dụ E001, E010, E100
            }
            catch { return $"E{Guid.NewGuid().ToString().Substring(0, 5)}"; } // Backup ID nếu lỗi
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