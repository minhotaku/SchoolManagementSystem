using System.Collections.Generic;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Services.Interfaces
{
    public interface IUserManagementService
    {
        /// <summary>
        /// Đăng ký người dùng mới với vai trò cụ thể trong hệ thống
        /// </summary>
        /// <param name="username">Tên đăng nhập duy nhất</param>
        /// <param name="password">Mật khẩu</param>
        /// <param name="role">Vai trò (Student, Faculty, Admin)</param>
        /// <param name="additionalInfo">Thông tin bổ sung tùy theo vai trò</param>
        /// <returns>ID người dùng đã tạo hoặc null nếu thất bại</returns>
        string RegisterUser(string username, string password, string role, Dictionary<string, string> additionalInfo = null);

        /// <summary>
        /// Xác thực người dùng
        /// </summary>
        /// <param name="username">Tên đăng nhập</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns>Đối tượng User đã xác thực hoặc null nếu thông tin không hợp lệ</returns>
        User AuthenticateUser(string username, string password);

        /// <summary>
        /// Lấy thông tin người dùng theo ID
        /// </summary>
        /// <param name="userId">ID người dùng</param>
        /// <returns>Thông tin người dùng</returns>
        User GetUserById(string userId);

        /// <summary>
        /// Lấy tất cả người dùng
        /// </summary>
        /// <returns>Danh sách người dùng</returns>
        IEnumerable<User> GetAllUsers();

        /// <summary>
        /// Lấy người dùng theo vai trò
        /// </summary>
        /// <param name="role">Vai trò người dùng (Student, Faculty, Admin)</param>
        /// <returns>Danh sách người dùng có vai trò chỉ định</returns>
        IEnumerable<User> GetUsersByRole(string role);

        /// <summary>
        /// Cập nhật thông tin người dùng
        /// </summary>
        /// <param name="user">Đối tượng User đã cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        bool UpdateUser(User user);

        /// <summary>
        /// Xóa người dùng theo ID
        /// </summary>
        /// <param name="userId">ID người dùng cần xóa</param>
        /// <returns>True nếu xóa thành công</returns>
        bool DeleteUser(string userId);
    }
}