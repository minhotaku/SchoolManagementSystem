using System.Collections.Generic;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Services.Interfaces
{
    public interface IUserService
    {
        IEnumerable<User> GetAllUsers();
        User GetUserById(string id);
        User GetUserByUsername(string username);
        IEnumerable<User> GetUsersByRole(string role);

        (bool Success, string Message) AddUser(User user);
        (bool Success, string Message) UpdateUser(User user);
        (bool Success, string Message) DeleteUser(string id);

        bool ValidateCredentials(string username, string password);
        bool UsernameExists(string username);
        bool UserIdExists(string userId);
    }
}