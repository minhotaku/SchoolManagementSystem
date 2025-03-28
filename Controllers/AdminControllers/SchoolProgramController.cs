using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Data; // Để dùng IUnitOfWork
using SchoolManagementSystem.Entities; // Để dùng SchoolProgram, Student
using SchoolManagementSystem.Models; // Để dùng ViewModels

namespace SchoolManagementSystem.Controllers.AdminControllers
{
    public class SchoolProgramController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DateTime _currentDateTime = DateTime.UtcNow;
        private readonly string _currentUserLogin = "admin_son"; // Giả định admin là Sơn

        // Constructor: Inject IUnitOfWork (thông qua Singleton trong trường hợp này)
        public SchoolProgramController()
        {
            _unitOfWork = UnitOfWork.GetInstance();
        }

        // GET: /SchoolProgram/Index
        // Hiển thị danh sách chương trình học và số lượng sinh viên
        [HttpGet]
        public IActionResult Index()
        {
            string actionName = nameof(Index);
            LogActionEntry(actionName);
            try
            {
                // Lấy dữ liệu cần thiết
                var programs = _unitOfWork.SchoolPrograms.GetAll()?.ToList() ?? new List<SchoolProgram>();
                var students = _unitOfWork.Students.GetAll()?.ToList() ?? new List<Student>();

                // Tạo danh sách ViewModel, bao gồm việc đếm số sinh viên
                var viewModels = programs.Select(p => new SchoolProgramViewModel
                {
                    SchoolProgramId = p.SchoolProgramId,
                    SchoolProgramName = p.SchoolProgramName,
                    StudentCount = students.Count(s => s.SchoolProgramId == p.SchoolProgramId) // Đếm số SV
                }).OrderBy(vm => vm.SchoolProgramName).ToList(); // Sắp xếp theo tên

                LogActionSuccess(actionName, $"Displayed {viewModels.Count} programs.");
                return View("~/Views/Admin/SchoolProgram/Index.cshtml", viewModels);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi và hiển thị thông báo lỗi
                LogActionError(actionName, ex);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách chương trình học.";
                // Trả về View với danh sách rỗng
                return View("~/Views/Admin/SchoolProgram/Index.cshtml", new List<SchoolProgramViewModel>());
            }
        }

        // GET: /SchoolProgram/Create
        // Hiển thị form tạo mới chương trình học
        [HttpGet]
        public IActionResult Create()
        {
            LogActionEntry(nameof(Create) + " (GET)");
            // Chỉ cần hiển thị View trống
            return View("~/Views/Admin/SchoolProgram/Create.cshtml");
        }

        // POST: /SchoolProgram/Create
        // Xử lý việc tạo mới chương trình học
        [HttpPost]
        [ValidateAntiForgeryToken] // Chống tấn công CSRF
        public IActionResult Create(SchoolProgramCreateViewModel model)
        {
            string actionName = nameof(Create) + " (POST)";
            LogActionEntry(actionName, $"Attempting to create program '{model.SchoolProgramName}'.");

            // --- Validation phía Server ---
            // 1. Kiểm tra tên chương trình đã tồn tại chưa (không phân biệt hoa thường, loại bỏ khoảng trắng thừa)
            string trimmedName = model.SchoolProgramName?.Trim();
            if (!string.IsNullOrEmpty(trimmedName)) // Chỉ kiểm tra nếu tên không rỗng
            {
                bool nameExists = _unitOfWork.SchoolPrograms.GetAll()
                                    .Any(p => p.SchoolProgramName.Equals(trimmedName, StringComparison.OrdinalIgnoreCase));
                if (nameExists)
                {
                    ModelState.AddModelError(nameof(model.SchoolProgramName), "Tên chương trình học này đã tồn tại.");
                }
            }
            // 2. Kiểm tra ModelState (bao gồm cả lỗi từ validation attribute và lỗi vừa thêm)
            if (!ModelState.IsValid)
            {
                LogModelStateErrors(actionName);
                TempData["ErrorMessage"] = "Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại.";
                return View("~/Views/Admin/SchoolProgram/Create.cshtml", model); // Trả về View với lỗi
            }
            // --- Kết thúc Validation ---

            try
            {
                // Tạo mã chương trình học mới
                string newProgramId = GenerateNewProgramId();
                if (string.IsNullOrEmpty(newProgramId))
                {
                    // Lỗi nghiêm trọng khi không tạo được ID
                    LogActionError(actionName, new InvalidOperationException("Could not generate a new Program ID."));
                    TempData["ErrorMessage"] = "Lỗi hệ thống: Không thể tạo mã chương trình học mới. Vui lòng thử lại.";
                    return View("~/Views/Admin/SchoolProgram/Create.cshtml", model);
                }

                // Tạo đối tượng Entity mới
                var newProgram = new SchoolProgram
                {
                    SchoolProgramId = newProgramId,
                    SchoolProgramName = trimmedName // Dùng tên đã trim
                };

                // Thêm vào Repository và Lưu thay đổi
                _unitOfWork.SchoolPrograms.Add(newProgram);
                _unitOfWork.SaveChanges();

                // Thông báo thành công và chuyển hướng
                TempData["SuccessMessage"] = $"Đã tạo thành công chương trình học '{newProgram.SchoolProgramName}'.";
                LogActionSuccess(actionName, $"Created program '{newProgram.SchoolProgramName}' with ID '{newProgram.SchoolProgramId}'.");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Ghi log lỗi và hiển thị thông báo lỗi
                LogActionError(actionName, ex);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi tạo chương trình học.";
                return View("~/Views/Admin/SchoolProgram/Create.cshtml", model);
            }
        }

        // GET: /SchoolProgram/Edit/{programId}
        // Hiển thị form chỉnh sửa chương trình học
        [HttpGet]
        public IActionResult Edit(string id) // Nhận ProgramId từ route
        {
            string actionName = nameof(Edit) + " (GET)";
            LogActionEntry(actionName, $"Loading edit page for Program ID '{id}'.");

            // Kiểm tra ID hợp lệ
            if (string.IsNullOrWhiteSpace(id))
            {
                LogActionWarning(actionName, "Program ID is null or whitespace.");
                return NotFound("Mã chương trình học không hợp lệ.");
            }

            try
            {
                // Lấy chương trình học từ Repository
                var program = _unitOfWork.SchoolPrograms.GetById(id);
                if (program == null)
                {
                    // Không tìm thấy
                    LogActionWarning(actionName, $"Program with ID '{id}' not found.");
                    TempData["ErrorMessage"] = $"Không tìm thấy chương trình học với mã {id}.";
                    return RedirectToAction("Index");
                }

                // Tạo ViewModel từ Entity
                var model = new SchoolProgramEditViewModel
                {
                    SchoolProgramId = program.SchoolProgramId,
                    SchoolProgramName = program.SchoolProgramName
                };

                // Trả về View Edit với ViewModel
                LogActionSuccess(actionName, $"Loaded program '{program.SchoolProgramName}' for edit.");
                return View("~/Views/Admin/SchoolProgram/Edit.cshtml", model);
            }
            catch (Exception ex)
            {
                LogActionError(actionName, ex, $"Error loading program {id} for edit.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải trang chỉnh sửa.";
                return RedirectToAction("Index");
            }
        }

        // POST: /SchoolProgram/Edit/{programId}
        // Xử lý việc cập nhật chương trình học
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, SchoolProgramEditViewModel model) // id là ProgramId từ route
        {
            string actionName = nameof(Edit) + " (POST)";
            LogActionEntry(actionName, $"Saving changes for Program ID '{id}'.");

            // Kiểm tra ID khớp
            if (id != model.SchoolProgramId)
            {
                LogActionWarning(actionName, $"Route ID '{id}' does not match model ID '{model.SchoolProgramId}'.");
                return BadRequest("Mã chương trình học không khớp.");
            }

            // --- Validation phía Server ---
            // 1. Kiểm tra tên mới có trùng với tên khác không (trừ chính nó)
            string trimmedName = model.SchoolProgramName?.Trim();
            if (!string.IsNullOrEmpty(trimmedName))
            {
                bool nameExists = _unitOfWork.SchoolPrograms.GetAll()
                                    .Any(p => p.SchoolProgramId != id && // Quan trọng: Loại trừ chính nó
                                               p.SchoolProgramName.Equals(trimmedName, StringComparison.OrdinalIgnoreCase));
                if (nameExists)
                {
                    ModelState.AddModelError(nameof(model.SchoolProgramName), "Tên chương trình học này đã được sử dụng bởi một chương trình khác.");
                }
            }
            // 2. Kiểm tra ModelState
            if (!ModelState.IsValid)
            {
                LogModelStateErrors(actionName);
                TempData["ErrorMessage"] = "Dữ liệu nhập không hợp lệ.";
                return View("~/Views/Admin/SchoolProgram/Edit.cshtml", model); // Trả về View với lỗi
            }
            // --- Kết thúc Validation ---

            try
            {
                // Lấy chương trình học hiện tại từ Repository
                var existingProgram = _unitOfWork.SchoolPrograms.GetById(id);
                if (existingProgram == null)
                {
                    LogActionWarning(actionName, $"Program with ID '{id}' not found for update.");
                    TempData["ErrorMessage"] = $"Không tìm thấy chương trình học với mã {id} để cập nhật.";
                    return RedirectToAction("Index");
                }

                // Kiểm tra xem có thay đổi thực sự không
                if (existingProgram.SchoolProgramName != trimmedName)
                {
                    // Cập nhật tên
                    existingProgram.SchoolProgramName = trimmedName;
                    _unitOfWork.SchoolPrograms.Update(existingProgram);
                    _unitOfWork.SaveChanges(); // Lưu thay đổi

                    TempData["SuccessMessage"] = $"Đã cập nhật thành công chương trình học '{existingProgram.SchoolProgramName}'.";
                    LogActionSuccess(actionName, $"Updated program {id}. New name: '{existingProgram.SchoolProgramName}'.");
                }
                else
                {
                    // Không có gì thay đổi
                    TempData["SuccessMessage"] = "Không có thay đổi nào được thực hiện."; // Hoặc dùng TempData["InfoMessage"]
                    LogActionInfo(actionName, $"No changes detected for program {id}.");
                }
                // Chuyển hướng về Index sau khi thành công hoặc không có thay đổi
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Ghi log lỗi và hiển thị thông báo
                LogActionError(actionName, ex, $"Error saving changes for program {id}.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi cập nhật chương trình học.";
                return View("~/Views/Admin/SchoolProgram/Edit.cshtml", model); // Trả về View Edit với model cũ
            }
        }

        // GET: /SchoolProgram/Delete/{programId}
        // Hiển thị trang xác nhận xóa
        [HttpGet]
        public IActionResult Delete(string id) // Nhận ProgramId
        {
            string actionName = nameof(Delete) + " (GET)";
            LogActionEntry(actionName, $"Viewing delete confirmation for Program ID '{id}'.");

            if (string.IsNullOrWhiteSpace(id)) { LogActionWarning(actionName, "ID is null."); return NotFound(); }

            try
            {
                var program = _unitOfWork.SchoolPrograms.GetById(id);
                if (program == null) { LogActionWarning(actionName, $"Program {id} not found."); TempData["ErrorMessage"] = $"Không tìm thấy chương trình {id}."; return RedirectToAction("Index"); }

                // Đếm số sinh viên để cảnh báo
                int studentCount = _unitOfWork.Students.GetBySchoolProgram(id).Count();
                ViewBag.StudentCount = studentCount;
                LogActionInfo(actionName, $"Program {id} ('{program.SchoolProgramName}') has {studentCount} students.");

                return View("~/Views/Admin/SchoolProgram/Delete.cshtml", program);
            }
            catch (Exception ex) { LogActionError(actionName, ex, $"Error loading program {id} for delete."); TempData["ErrorMessage"] = "Lỗi tải trang xóa."; return RedirectToAction("Index"); }
        }

        // POST: /SchoolProgram/Delete/{programId}
        // Xử lý việc xóa chương trình học
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id) // id là ProgramId
        {
            string actionName = nameof(DeleteConfirmed) + " (POST)";
            LogActionEntry(actionName, $"Confirming deletion for Program ID '{id}'.");

            if (string.IsNullOrWhiteSpace(id)) { LogActionWarning(actionName, "ID is null."); return BadRequest(); }

            try
            {
                var programToDelete = _unitOfWork.SchoolPrograms.GetById(id);
                if (programToDelete == null) { LogActionWarning(actionName, $"Program {id} not found."); TempData["ErrorMessage"] = $"Không tìm thấy chương trình {id}."; return RedirectToAction("Index"); }

                // *** Kiểm tra ràng buộc sinh viên ***
                bool hasStudents = _unitOfWork.Students.GetBySchoolProgram(id).Any();
                if (hasStudents)
                {
                    LogActionWarning(actionName, $"Deletion failed for program {id}: Students assigned.");
                    TempData["ErrorMessage"] = $"Không thể xóa chương trình '{programToDelete.SchoolProgramName}' vì đang có sinh viên tham gia.";
                    // Không xóa, chỉ chuyển hướng về Index với lỗi
                    return RedirectToAction("Index");
                }

                // Thực hiện xóa
                _unitOfWork.SchoolPrograms.Delete(id);
                _unitOfWork.SaveChanges();

                TempData["SuccessMessage"] = $"Đã xóa chương trình '{programToDelete.SchoolProgramName}'.";
                LogActionSuccess(actionName, $"Deleted program {id}.");
                return RedirectToAction("Index");
            }
            catch (Exception ex) { LogActionError(actionName, ex, $"Error deleting program {id}."); TempData["ErrorMessage"] = "Lỗi hệ thống khi xóa."; return RedirectToAction("Index"); }
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
                return null; // Trả về null nếu có lỗi
            }
        }

        // Helper ghi log (ví dụ)
        private void LogActionEntry(string actionName, string? message = null) { System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:T}] User '{_currentUserLogin}' entered {actionName}. {message}"); }
        private void LogActionSuccess(string actionName, string message) { System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:T}] SUCCESS: {actionName} by '{_currentUserLogin}'. {message}"); }
        private void LogActionInfo(string actionName, string message) { System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:T}] INFO: {actionName} by '{_currentUserLogin}'. {message}"); }
        private void LogActionWarning(string actionName, string message) { System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:T}] WARNING: {actionName} by '{_currentUserLogin}'. {message}"); }
        private void LogActionError(string actionName, Exception ex, string? message = null) { System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:T}] ERROR: {actionName} by '{_currentUserLogin}'. {message} Exception: {ex.Message}"); /* Ghi đầy đủ lỗi vào file log thực tế */ }
        private void LogModelStateErrors(string actionName)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            LogActionWarning(actionName, $"ModelState Invalid. Errors: {string.Join("; ", errors)}");
        }
    }
}