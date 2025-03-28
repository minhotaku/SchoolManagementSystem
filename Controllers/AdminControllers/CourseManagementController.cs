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
                var allUsers = _unitOfWork.Users.GetAll()?.ToList() ?? new List<User>(); // Cần để lấy username giảng viên

                System.Diagnostics.Debug.WriteLine($"Found {allCourses.Count} courses, {allFaculties.Count} faculty records, {allUsers.Count} users.");

                // Join để lấy thông tin ViewModel
                var courseViewModels = (
                    from course in allCourses
                    join facultyMember in allFaculties
                        on course.FacultyId equals facultyMember.FacultyId // Join course với faculty
                        into facultyGroup
                    from fm in facultyGroup.DefaultIfEmpty() // Left Join phòng trường hợp FacultyId sai
                    join user in allUsers
                        on fm?.UserId equals user.UserId // Join faculty với user để lấy username
                        into userGroup
                    from u in userGroup.DefaultIfEmpty() // Left Join phòng trường hợp user bị thiếu
                    orderby course.CourseName // Sắp xếp theo tên khóa học
                    select new CourseViewModel
                    {
                        CourseId = course.CourseId,
                        CourseName = course.CourseName,
                        Credits = course.Credits,
                        FacultyId = course.FacultyId, // Có thể ẩn đi nếu không cần
                        FacultyUsername = u?.Username ?? (fm != null ? "(Tài khoản GV bị thiếu)" : "(Chưa gán GV)") // Hiển thị username hoặc thông báo
                    }
                ).ToList();

                System.Diagnostics.Debug.WriteLine($"Generated {courseViewModels.Count} CourseViewModels.");
                // Thêm Debugging join nếu cần

                return View("~/Views/Admin/CourseManagement/Index.cshtml", courseViewModels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Course Index: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Lỗi tải danh sách khóa học.";
                return View("~/Views/Admin/CourseManagement/Index.cshtml", new List<CourseViewModel>());
            }
        }

        // GET: /CourseManagement/Create
        [HttpGet]
        public IActionResult Create()
        {
            LoadFacultyList(); // Chuẩn bị danh sách giảng viên cho dropdown
            return View("~/Views/Admin/CourseManagement/Create.cshtml");
        }

        // POST: /CourseManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CourseCreateViewModel model)
        {
            System.Diagnostics.Debug.WriteLine($"Attempting to create course '{model.CourseName}'.");
            LoadFacultyList(model.FacultyId); // Load lại danh sách giảng viên

            // Kiểm tra xem FacultyId có hợp lệ không
            if (_unitOfWork.Faculty.GetById(model.FacultyId) == null)
            {
                ModelState.AddModelError(nameof(model.FacultyId), "Giảng viên được chọn không hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                LogModelStateErrors("Create");
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return View("~/Views/Admin/CourseManagement/Create.cshtml", model);
            }

            try
            {
                // Tạo CourseId mới (ví dụ: CXXX)
                string newCourseId = GenerateNewCourseId();
                if (string.IsNullOrEmpty(newCourseId)) // Kiểm tra nếu không tạo được ID
                {
                    TempData["ErrorMessage"] = "Lỗi hệ thống: Không thể tạo mã khóa học mới.";
                    return View("~/Views/Admin/CourseManagement/Create.cshtml", model);
                }


                var newCourse = new Course
                {
                    CourseId = newCourseId, // Gán ID mới
                    CourseName = model.CourseName,
                    Credits = model.Credits,
                    FacultyId = model.FacultyId
                };

                _unitOfWork.Courses.Add(newCourse);
                _unitOfWork.SaveChanges(); // Lưu khóa học mới

                TempData["SuccessMessage"] = $"Đã tạo thành công khóa học '{model.CourseName}'.";
                System.Diagnostics.Debug.WriteLine($"Successfully created course '{model.CourseName}' with ID '{newCourse.CourseId}'.");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating course: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Lỗi hệ thống khi tạo khóa học.";
                return View("~/Views/Admin/CourseManagement/Create.cshtml", model);
            }
        }

        // GET: /CourseManagement/Edit/{courseId}
        [HttpGet]
        public IActionResult Edit(string id) // Nhận CourseId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Mã khóa học không hợp lệ.");
            System.Diagnostics.Debug.WriteLine($"Loading edit page for Course ID '{id}'.");
            try
            {
                var course = _unitOfWork.Courses.GetById(id);
                if (course == null) { TempData["ErrorMessage"] = $"Không tìm thấy khóa học {id}."; return RedirectToAction("Index"); }

                var model = new CourseEditViewModel
                {
                    CourseId = course.CourseId,
                    CourseName = course.CourseName,
                    Credits = course.Credits,
                    FacultyId = course.FacultyId
                };

                LoadFacultyList(model.FacultyId); // Load dropdown giảng viên, chọn sẵn giảng viên hiện tại

                return View("~/Views/Admin/CourseManagement/Edit.cshtml", model);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading edit course {id}: {ex.Message}"); TempData["ErrorMessage"] = "Lỗi tải trang sửa."; return RedirectToAction("Index"); }
        }

        // POST: /CourseManagement/Edit/{courseId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, CourseEditViewModel model) // id là CourseId
        {
            if (id != model.CourseId) return BadRequest("Mã khóa học không khớp.");
            System.Diagnostics.Debug.WriteLine($"Saving changes for Course ID '{id}'.");
            LoadFacultyList(model.FacultyId); // Load lại danh sách giảng viên

            // Kiểm tra FacultyId hợp lệ
            if (_unitOfWork.Faculty.GetById(model.FacultyId) == null) { ModelState.AddModelError(nameof(model.FacultyId), "Giảng viên được chọn không hợp lệ."); }


            if (!ModelState.IsValid) { LogModelStateErrors(id); TempData["ErrorMessage"] = "Dữ liệu không hợp lệ."; return View("~/Views/Admin/CourseManagement/Edit.cshtml", model); }

            try
            {
                var existingCourse = _unitOfWork.Courses.GetById(id);
                if (existingCourse == null) { TempData["ErrorMessage"] = $"Không tìm thấy khóa học {id}."; return RedirectToAction("Index"); }

                // Cập nhật thông tin
                bool changed = false;
                if (existingCourse.CourseName != model.CourseName) { existingCourse.CourseName = model.CourseName; changed = true; }
                if (existingCourse.Credits != model.Credits) { existingCourse.Credits = model.Credits; changed = true; }
                if (existingCourse.FacultyId != model.FacultyId) { existingCourse.FacultyId = model.FacultyId; changed = true; }

                if (changed)
                {
                    _unitOfWork.Courses.Update(existingCourse);
                    _unitOfWork.SaveChanges(); // Lưu thay đổi
                    System.Diagnostics.Debug.WriteLine($"Course {id} updated.");
                    TempData["SuccessMessage"] = $"Đã cập nhật khóa học '{model.CourseName}'.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Không có thay đổi nào cho khóa học '{model.CourseName}'.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Lỗi hệ thống khi cập nhật."; System.Diagnostics.Debug.WriteLine($"Error saving course {id}: {ex.Message}"); return View("~/Views/Admin/CourseManagement/Edit.cshtml", model); }
        }

        // GET: /CourseManagement/Delete/{courseId}
        [HttpGet]
        public IActionResult Delete(string id) // Nhận CourseId
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound("Mã khóa học không hợp lệ.");
            System.Diagnostics.Debug.WriteLine($"Viewing delete confirmation for Course ID '{id}'.");
            try
            {
                var course = _unitOfWork.Courses.GetById(id);
                if (course == null) { TempData["ErrorMessage"] = $"Không tìm thấy khóa học {id}."; return RedirectToAction("Index"); }

                // Có thể lấy thêm thông tin giảng viên để hiển thị nếu muốn
                var faculty = _unitOfWork.Faculty.GetById(course.FacultyId);
                var user = (faculty != null) ? UserManagementService.GetInstance().GetUserById(faculty.UserId) : null;
                ViewBag.FacultyNameToDelete = user?.Username ?? "(Chưa gán)";

                return View("~/Views/Admin/CourseManagement/Delete.cshtml", course); // Gửi Course entity đến View
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Lỗi tải trang xóa."; System.Diagnostics.Debug.WriteLine($"Error loading delete course {id}: {ex.Message}"); return RedirectToAction("Index"); }
        }

        // POST: /CourseManagement/Delete/{courseId}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id) // id là CourseId
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("Mã khóa học không hợp lệ.");
            System.Diagnostics.Debug.WriteLine($"Confirming deletion for Course ID '{id}'.");
            try
            {
                var courseToDelete = _unitOfWork.Courses.GetById(id);
                if (courseToDelete == null) { TempData["ErrorMessage"] = $"Không tìm thấy khóa học {id}."; return RedirectToAction("Index"); }

                // *** Kiểm tra ràng buộc trước khi xóa (QUAN TRỌNG) ***
                // Ví dụ: Kiểm tra xem có Enrollment nào đang tham chiếu đến khóa học này không
                var enrollmentsExist = _unitOfWork.Enrollments.GetByCourse(id).Any();
                if (enrollmentsExist)
                {
                    TempData["ErrorMessage"] = $"Không thể xóa khóa học '{courseToDelete.CourseName}' vì đang có sinh viên đăng ký.";
                    System.Diagnostics.Debug.WriteLine($"Deletion failed for course {id}: Existing enrollments found.");
                    return RedirectToAction("Index"); // Hoặc trang chi tiết khóa học
                }

                _unitOfWork.Courses.Delete(id);
                _unitOfWork.SaveChanges();

                TempData["SuccessMessage"] = $"Đã xóa khóa học '{courseToDelete.CourseName}'.";
                System.Diagnostics.Debug.WriteLine($"Successfully deleted course {id}.");
                return RedirectToAction("Index");
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Lỗi hệ thống khi xóa."; System.Diagnostics.Debug.WriteLine($"Error deleting course {id}: {ex.Message}"); return RedirectToAction("Index"); }
        }


        // --- Helper Methods ---
        private void LoadFacultyList(string? selectedFacultyId = null)
        {
            try
            {
                // Lấy danh sách User là Faculty và thông tin Faculty tương ứng
                var facultyUsers = (from user in _unitOfWork.Users.GetAll()
                                    where user.Role.Equals("Faculty", StringComparison.OrdinalIgnoreCase)
                                    join faculty in _unitOfWork.Faculty.GetAll() on user.UserId equals faculty.UserId
                                    orderby user.Username // Sắp xếp theo tên cho dễ chọn
                                    select new
                                    {
                                        FacultyId = faculty.FacultyId, // Giá trị value của option
                                        DisplayText = $"{user.Username} ({faculty.FacultyId})" // Text hiển thị trong dropdown
                                    }).ToList();

                ViewBag.FacultyList = new SelectList(facultyUsers, "FacultyId", "DisplayText", selectedFacultyId);
                System.Diagnostics.Debug.WriteLine($"Loaded {facultyUsers.Count} faculty members for dropdown.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading faculty list: {ex.Message}");
                ViewBag.FacultyList = new SelectList(new List<object>()); // Tránh lỗi null
                TempData["ErrorMessageLoading"] = "Lỗi tải danh sách giảng viên.";
            }
        }

        // Helper tạo CourseId mới (ví dụ)
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
                // Đảm bảo ID có đủ 3 chữ số sau chữ 'C' (ví dụ C001, C010, C100)
                return $"C{nextIdNumber:D3}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating new Course ID: {ex.Message}");
                return null; // Trả về null nếu có lỗi
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