using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Services.Interfaces
{
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authenticates a user based on username and password.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password (plain text).</param>
        /// <returns>The authenticated User object if successful, otherwise null.</returns>
        User Authenticate(string username, string password);
    }
}