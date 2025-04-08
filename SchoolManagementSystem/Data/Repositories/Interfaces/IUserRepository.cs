using System.Collections.Generic;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Interfaces
{
    // Interface đặc thù cho User, chỉ chứa các phương thức liên quan đến User
    public interface IUserRepository : IRepository<User>
    {
        User GetByUsername(string username);
        IEnumerable<User> GetByRole(string role);
    }
}