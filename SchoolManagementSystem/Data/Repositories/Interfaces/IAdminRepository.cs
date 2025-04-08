using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Interfaces
{
    // Interface đặc thù cho Admin
    public interface IAdminRepository : IRepository<Admin>
    {
        Admin GetByUserId(string userId);
    }
}