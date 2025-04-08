using System.Collections.Generic;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Interfaces
{
    // Interface đặc thù cho Grade
    public interface IGradeRepository : IRepository<Grade>
    {
        IEnumerable<Grade> GetByEnrollment(string enrollmentId);
    }
}