using System.Collections.Generic;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Interfaces
{
    // Interface đặc thù cho Student
    public interface IStudentRepository : IRepository<Student>
    {
        Student GetByUserId(string userId);
        IEnumerable<Student> GetBySchoolProgram(string schoolProgramId);
    }
}