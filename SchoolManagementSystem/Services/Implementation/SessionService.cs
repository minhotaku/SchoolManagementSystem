using Microsoft.AspNetCore.Http;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Services.Interfaces;
using System;

namespace SchoolManagementSystem.Services.Implementation
{
    public class SessionService : ISessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;

        // Singleton pattern
        private static SessionService _instance;
        private static readonly object _lock = new object();

        public static SessionService GetInstance(IHttpContextAccessor httpContextAccessor = null)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new SessionService(httpContextAccessor);
                    }
                }
            }
            return _instance;
        }

        private SessionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public void SetUserSession(string userId, string username, string role)
        {
            _session.SetString(SessionConstants.SessionKeyUserId, userId);
            _session.SetString(SessionConstants.SessionKeyUsername, username);
            _session.SetString(SessionConstants.SessionKeyUserRole, role);
        }

        public bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(GetUserId());
        }

        public bool IsInRole(string role)
        {
            if (!IsAuthenticated())
                return false;

            var userRole = GetUserRole();
            return userRole == role;
        }

        public bool IsInAnyRole(params string[] roles)
        {
            if (!IsAuthenticated())
                return false;

            var userRole = GetUserRole();
            foreach (var role in roles)
            {
                if (userRole == role)
                    return true;
            }
            return false;
        }

        public string GetUserId()
        {
            return _session.GetString(SessionConstants.SessionKeyUserId);
        }

        public string GetUsername()
        {
            return _session.GetString(SessionConstants.SessionKeyUsername);
        }

        public string GetUserRole()
        {
            return _session.GetString(SessionConstants.SessionKeyUserRole);
        }

        public void ClearSession()
        {
            _session.Clear();
        }
    }
}