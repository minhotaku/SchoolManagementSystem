using System.Collections.Generic;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Data.Repositories.Interfaces
{
    // Interface đặc thù cho Enrollment
    public interface IEnrollmentRepository : IRepository<Enrollment>
    {
        IEnumerable<Enrollment> GetByStudent(string studentId);
        IEnumerable<Enrollment> GetByCourse(string courseId);
        IEnumerable<Enrollment> GetBySemester(string semester);
    }
}