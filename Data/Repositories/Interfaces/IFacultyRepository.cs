using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Interfaces
{
    // Interface đặc thù cho Faculty
    public interface IFacultyRepository : IRepository<Faculty>
    {
        Faculty GetByUserId(string userId);
    }
}