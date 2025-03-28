using BCrypt.Net;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Interfaces;

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

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return null; // Authentication failed
            }

            return user; // Authentication successful
        }
    }
}
