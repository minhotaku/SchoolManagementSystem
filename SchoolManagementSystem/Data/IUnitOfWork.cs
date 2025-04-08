using SchoolManagementSystem.Data.Repositories.Interfaces;

namespace SchoolManagementSystem.Data
{
    // Interface chỉ chứa các repository cần thiết
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        IStudentRepository Students { get; }
        IFacultyRepository Faculty { get; }
        IAdminRepository Admins { get; }
        ICourseRepository Courses { get; }
        IEnrollmentRepository Enrollments { get; }
        IGradeRepository Grades { get; }
        ISchoolProgramRepository SchoolPrograms { get; }
        INotificationRepository Notifications { get; }

        void SaveChanges();
    }
}