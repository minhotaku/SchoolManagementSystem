using System.Collections.Generic;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Interfaces
{
    // Interface đặc thù cho Course
    public interface ICourseRepository : IRepository<Course>
    {
        IEnumerable<Course> GetByFaculty(string facultyId);
        IEnumerable<Course> GetByCredits(int credits);
    }
}