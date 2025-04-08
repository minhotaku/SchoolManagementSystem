using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Interfaces
{
    // Interface đặc thù cho Program
    public interface ISchoolProgramRepository : IRepository<SchoolProgram>
    {
        SchoolProgram GetByName(string name);
    }
}