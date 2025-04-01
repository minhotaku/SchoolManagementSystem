using Microsoft.AspNetCore.Http;

namespace SchoolManagementSystem.Services.Interfaces
{
    public interface ISessionService
    {
        void SetUserSession(string userId, string username, string role);
        bool IsAuthenticated();
        bool IsInRole(string role);
        bool IsInAnyRole(params string[] roles);
        string GetUserId();
        string GetUsername();
        string GetUserRole();
        void ClearSession();
    }
}