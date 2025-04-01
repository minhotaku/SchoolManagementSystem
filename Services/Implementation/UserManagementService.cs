using System;
using System.Collections.Generic;
using System.Linq;
using BCrypt.Net;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Interfaces;
using SchoolManagementSystem.Utils;

namespace SchoolManagementSystem.Services.Implementation
{
    public class UserManagementService : IUserManagementService
    {
        // Singleton pattern
        private static UserManagementService _instance;
        private static readonly object _lock = new object();

        private readonly IUnitOfWork _unitOfWork;

        // Ngày giờ hiện tại UTC và thông tin người dùng đăng nhập hiện tại
        private readonly DateTime _currentDateTime = new DateTime(2025, 03, 27, 15, 34, 30);
        private readonly string _currentUserLogin = "minhotaku";

        /// <summary>
        /// Lấy instance của UserManagementService (Singleton pattern)
        /// </summary>
        public static UserManagementService GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new UserManagementService();
                    }
                }
            }
            return _instance;
        }

        /// <summary>
        /// Constructor riêng tư cho Singleton pattern
        /// </summary>
        private UserManagementService()
        {
            _unitOfWork = UnitOfWork.GetInstance();
        }

        /// <summary>
        /// Đăng ký người dùng mới với vai trò cụ thể
        /// </summary>
        public string RegisterUser(string username, string password, string role, Dictionary<string, string> additionalInfo = null)
        {
            try
            {
                // Kiểm tra tính hợp lệ của dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
                {
                    return null; // Dữ liệu không hợp lệ
                }

                // Kiểm tra xem tên người dùng đã tồn tại chưa
                if (_unitOfWork.Users.GetByUsername(username) != null)
                {
                    return null; // Tên người dùng đã tồn tại
                }

                // Kiểm tra vai trò hợp lệ
                if (!IsValidRole(role))
                {
                    return null; // Vai trò không hợp lệ
                }

                // Tạo ID ngẫu nhiên cho người dùng mới
                var userId = GenerateUniqueId();

                // Tạo đối tượng người dùng mới với mật khẩu đã mã hóa bằng BCrypt
                var newUser = new User
                {
                    UserId = userId,
                    Username = username,
                    PasswordHash = HashPassword.ComputeSha256Hash(password),
                    Role = role
                };

                // Lưu thông tin người dùng vào repository
                _unitOfWork.Users.Add(newUser);

                // Tạo các đối tượng phụ thuộc dựa trên vai trò
                switch (role.ToLower())
                {
                    case "student":
                        // Kiểm tra thông tin chương trình học cần thiết cho sinh viên
                        if (additionalInfo == null || !additionalInfo.ContainsKey("SchoolProgramId"))
                        {
                            _unitOfWork.Users.Delete(userId);
                            _unitOfWork.SaveChanges();
                            return null; // Thiếu thông tin chương trình học
                        }

                        // Kiểm tra ID chương trình học có tồn tại không
                        string programId = additionalInfo["SchoolProgramId"];
                        if (_unitOfWork.SchoolPrograms.GetById(programId) == null)
                        {
                            _unitOfWork.Users.Delete(userId);
                            _unitOfWork.SaveChanges();
                            return null; // Chương trình học không tồn tại
                        }

                        // Tạo đối tượng sinh viên với thông tin cần thiết
                        var student = new Student
                        {
                            StudentId = GenerateUniqueId(),
                            UserId = userId,
                            SchoolProgramId = programId
                        };

                        // Lưu thông tin sinh viên
                        _unitOfWork.Students.Add(student);
                        break;

                    case "faculty":
                        // Tạo đối tượng giảng viên
                        var faculty = new Faculty
                        {
                            FacultyId = GenerateUniqueId(),
                            UserId = userId
                        };

                        // Lưu thông tin giảng viên
                        _unitOfWork.Faculty.Add(faculty);
                        break;

                    case "admin":
                        // Tạo đối tượng quản trị viên
                        var admin = new Admin
                        {
                            AdminId = GenerateUniqueId(),
                            UserId = userId
                        };

                        // Lưu thông tin quản trị viên
                        _unitOfWork.Admins.Add(admin);
                        break;
                }

                // Lưu tất cả thay đổi
                _unitOfWork.SaveChanges();

                // Ghi log hoạt động
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} created a new user with ID {userId} and role {role}");

                // Trả về ID người dùng đã tạo thành công
                return userId;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error during user registration: {ex.Message}");
                return null; // Lỗi trong quá trình đăng ký
            }
        }

        /// <summary>
        /// Xác thực người dùng bằng tên đăng nhập và mật khẩu
        /// </summary>
        public User AuthenticateUser(string username, string password)
        {
            // Kiểm tra tính hợp lệ của dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            try
            {
                // Tìm kiếm người dùng theo tên đăng nhập
                var user = _unitOfWork.Users.GetByUsername(username);
                if (user == null)
                {
                    // Ghi log thất bại
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Failed login attempt: Username '{username}' not found");
                    return null; // Không tìm thấy người dùng
                }

                // Kiểm tra mật khẩu đăng nhập có khớp với mật khẩu đã lưu không
                if (!password.Equals(HashPassword.ComputeSha256Hash(user.PasswordHash)))
                {
                    // Ghi log thất bại
                    System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Failed login attempt: Incorrect password for user '{username}'");
                    return null; // Mật khẩu không đúng
                }

                // Ghi log thành công
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Successful login: User '{username}' authenticated");

                // Trả về thông tin người dùng nếu xác thực thành công
                return user;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error during authentication: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy thông tin người dùng theo ID
        /// </summary>
        public User GetUserById(string userId)
        {
            // Kiểm tra tính hợp lệ của ID
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            try
            {
                // Trả về thông tin người dùng theo ID
                return _unitOfWork.Users.GetById(userId);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error retrieving user by ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy tất cả người dùng trong hệ thống
        /// </summary>
        public IEnumerable<User> GetAllUsers()
        {
            try
            {
                // Ghi log hoạt động
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} retrieved all users");

                return _unitOfWork.Users.GetAll();
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error retrieving all users: {ex.Message}");
                return new List<User>(); // Trả về danh sách rỗng nếu có lỗi
            }
        }

        /// <summary>
        /// Lấy danh sách người dùng theo vai trò
        /// </summary>
        public IEnumerable<User> GetUsersByRole(string role)
        {
            // Kiểm tra tính hợp lệ của vai trò
            if (string.IsNullOrWhiteSpace(role) || !IsValidRole(role))
            {
                return new List<User>(); // Trả về danh sách rỗng nếu vai trò không hợp lệ
            }

            try
            {
                // Ghi log hoạt động
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} retrieved users with role {role}");

                // Trả về danh sách người dùng có vai trò chỉ định
                return _unitOfWork.Users.GetByRole(role);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error retrieving users by role: {ex.Message}");
                return new List<User>(); // Trả về danh sách rỗng nếu có lỗi
            }
        }

        /// <summary>
        /// Cập nhật thông tin người dùng
        /// </summary>
        public bool UpdateUser(User user)
        {
            // Kiểm tra tính hợp lệ của đối tượng người dùng
            if (user == null || string.IsNullOrWhiteSpace(user.UserId))
            {
                return false;
            }

            try
            {
                // Kiểm tra người dùng có tồn tại không
                var existingUser = _unitOfWork.Users.GetById(user.UserId);
                if (existingUser == null)
                {
                    return false; // Người dùng không tồn tại
                }

                // Cập nhật thông tin người dùng
                _unitOfWork.Users.Update(user);
                _unitOfWork.SaveChanges();

                // Ghi log hoạt động
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} updated user with ID {user.UserId}");

                return true;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error updating user: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xóa người dùng và các thông tin liên quan theo ID
        /// </summary>
        public bool DeleteUser(string userId)
        {
            // Kiểm tra tính hợp lệ của ID
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            try
            {
                // Kiểm tra người dùng có tồn tại không
                var user = _unitOfWork.Users.GetById(userId);
                if (user == null)
                {
                    return false; // Người dùng không tồn tại
                }

                // Xóa các đối tượng liên quan dựa trên vai trò người dùng
                switch (user.Role.ToLower())
                {
                    case "student":
                        // Xóa thông tin sinh viên liên quan
                        var student = _unitOfWork.Students.GetByUserId(userId);
                        if (student != null)
                        {
                            _unitOfWork.Students.Delete(student.StudentId);
                        }
                        break;

                    case "faculty":
                        // Xóa thông tin giảng viên liên quan
                        var faculty = _unitOfWork.Faculty.GetByUserId(userId);
                        if (faculty != null)
                        {
                            _unitOfWork.Faculty.Delete(faculty.FacultyId);
                        }
                        break;

                    case "admin":
                        // Xóa thông tin quản trị viên liên quan
                        var admin = _unitOfWork.Admins.GetByUserId(userId);
                        if (admin != null)
                        {
                            _unitOfWork.Admins.Delete(admin.AdminId);
                        }
                        break;
                }

                // Xóa thông tin người dùng
                _unitOfWork.Users.Delete(userId);

                // Lưu tất cả thay đổi
                _unitOfWork.SaveChanges();

                // Ghi log hoạt động
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] User {_currentUserLogin} deleted user with ID {userId} and role {user.Role}");

                return true;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine($"[{_currentDateTime:yyyy-MM-dd HH:mm:ss}] Error deleting user: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra vai trò người dùng có hợp lệ không
        /// </summary>
        private bool IsValidRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return false;
            }

            // Danh sách các vai trò hợp lệ
            string roleLower = role.ToLower();
            return roleLower == "student" || roleLower == "faculty" || roleLower == "admin";
        }

        /// <summary>
        /// Tạo ID duy nhất cho đối tượng mới
        /// </summary>
        private string GenerateUniqueId()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }
    }
}