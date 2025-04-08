
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Interfaces;
using SchoolManagementSystem.Utils; 
using System; 

namespace SchoolManagementSystem.Services.Implementation
{
    public class AuthenticationService : IAuthenticationService
    {
        private static AuthenticationService _instance;
        private static readonly object _lock = new object();
        private readonly IUnitOfWork _unitOfWork;
        public static AuthenticationService GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new AuthenticationService();
                    }
                }
            }
            return _instance;
        }

        private AuthenticationService()
        {
            _unitOfWork = UnitOfWork.GetInstance();
        }


        public User Authenticate(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var user = _unitOfWork.Users.GetByUsername(username);

            // Nếu user không tồn tại, trả về null ngay lập tức
            if (user == null)
            {
                return null; // User not found
            }

            string providedPasswordHash = HashPassword.ComputeSha256Hash(password);

            if (!string.Equals(providedPasswordHash, user.PasswordHash, StringComparison.OrdinalIgnoreCase))
            {
                // Mật khẩu không khớp
                return null; // Authentication failed
            }
            // --- KẾT THÚC THAY ĐỔI ---

            return user; // Authentication successful
        }


    }
}