using System;
using System.Collections.Generic;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Interfaces;

namespace SchoolManagementSystem.Services.Implementation
{
    public class UserService : IUserService
    {
        // Singleton pattern
        private static UserService _instance;
        private static readonly object _lock = new object();

        private readonly IUnitOfWork _unitOfWork;

        public static UserService GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new UserService();
                    }
                }
            }
            return _instance;
        }

        private UserService()
        {
            _unitOfWork = UnitOfWork.GetInstance();
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _unitOfWork.Users.GetAll();
        }

        public User GetUserById(string id)
        {
            return _unitOfWork.Users.GetById(id);
        }

        public User GetUserByUsername(string username)
        {
            return _unitOfWork.Users.GetByUsername(username);
        }

        public IEnumerable<User> GetUsersByRole(string role)
        {
            return _unitOfWork.Users.GetByRole(role);
        }

        public (bool Success, string Message) AddUser(User user)
        {
            try
            {
                // Kiểm tra username đã tồn tại chưa
                if (UsernameExists(user.Username))
                {
                    return (false, $"Username '{user.Username}' đã tồn tại. Vui lòng chọn username khác.");
                }

                // Kiểm tra UserID đã tồn tại chưa
                if (UserIdExists(user.UserId))
                {
                    return (false, $"User ID '{user.UserId}' đã tồn tại. Vui lòng chọn User ID khác.");
                }

                // Hash mật khẩu nếu cần
                if (!string.IsNullOrEmpty(user.PasswordHash) && !user.PasswordHash.StartsWith("$2a$"))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                }

                _unitOfWork.Users.Add(user);
                _unitOfWork.SaveChanges();

                return (true, $"Người dùng '{user.Username}' với ID '{user.UserId}' đã được tạo thành công.");
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                return (false, $"Lỗi khi tạo người dùng: {ex.Message}");
            }
        }

        public (bool Success, string Message) UpdateUser(User user)
        {
            try
            {
                // Kiểm tra người dùng tồn tại
                var existingUser = GetUserById(user.UserId);
                if (existingUser == null)
                {
                    return (false, "Không tìm thấy người dùng để cập nhật.");
                }

                // Kiểm tra username trùng lặp với người dùng khác
                if (existingUser.Username != user.Username)
                {
                    var userWithSameUsername = GetUserByUsername(user.Username);
                    if (userWithSameUsername != null && userWithSameUsername.UserId != user.UserId)
                    {
                        return (false, $"Username '{user.Username}' đã được sử dụng bởi người dùng khác.");
                    }
                }

                // Hash mật khẩu nếu cần
                if (!string.IsNullOrEmpty(user.PasswordHash) && !user.PasswordHash.StartsWith("$2a$"))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                }
                else if (string.IsNullOrEmpty(user.PasswordHash))
                {
                    // Giữ lại mật khẩu cũ nếu không cung cấp mật khẩu mới
                    user.PasswordHash = existingUser.PasswordHash;
                }

                _unitOfWork.Users.Update(user);
                _unitOfWork.SaveChanges();

                return (true, $"Người dùng '{user.Username}' (ID: {user.UserId}) đã được cập nhật thành công.");
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                return (false, $"Lỗi khi cập nhật người dùng: {ex.Message}");
            }
        }

        public (bool Success, string Message) DeleteUser(string id)
        {
            try
            {
                var user = GetUserById(id);
                if (user == null)
                {
                    return (false, "Không tìm thấy người dùng để xóa.");
                }

                // Kiểm tra xem người dùng có liên kết đến bảng khác không
                // Ví dụ: student, faculty, admin
                // TODO: Implement check for related records

                _unitOfWork.Users.Delete(id);
                _unitOfWork.SaveChanges();

                return (true, $"Người dùng '{user.Username}' (ID: {user.UserId}) đã được xóa thành công.");
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                return (false, $"Lỗi khi xóa người dùng: {ex.Message}");
            }
        }

        public bool ValidateCredentials(string username, string password)
        {
            var user = _unitOfWork.Users.GetByUsername(username);
            if (user == null)
            {
                return false;
            }

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        public bool UsernameExists(string username)
        {
            return _unitOfWork.Users.GetByUsername(username) != null;
        }

        public bool UserIdExists(string userId)
        {
            return _unitOfWork.Users.GetById(userId) != null;
        }
    }
}
