using System;
using System.IO;
using SchoolManagementSystem.Data.Repositories.Implementation;
using SchoolManagementSystem.Data.Repositories.Interfaces;

namespace SchoolManagementSystem.Data
{
    // Class tuân thủ SRP - chỉ có trách nhiệm quản lý và điều phối repositories
    public class UnitOfWork : IUnitOfWork
    {
        private readonly string _basePath;

        // Repositories
        public IUserRepository Users { get; private set; }
        public IStudentRepository Students { get; private set; }
        public IFacultyRepository Faculty { get; private set; }
        public IAdminRepository Admins { get; private set; }
        public ICourseRepository Courses { get; private set; }
        public IEnrollmentRepository Enrollments { get; private set; }
        public IGradeRepository Grades { get; private set; }
        public ISchoolProgramRepository SchoolPrograms { get; private set; }
        public INotificationRepository Notifications { get; private set; }

        // Singleton pattern
        private static UnitOfWork _instance;
        private static readonly object _lock = new object();

        public static UnitOfWork GetInstance(string basePath = null)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        if (string.IsNullOrEmpty(basePath))
                        {
                            basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "CSV");
                        }
                        _instance = new UnitOfWork(basePath);
                    }
                }
            }
            return _instance;
        }

        private UnitOfWork(string basePath)
        {
            _basePath = basePath;

            // Khởi tạo repositories
            Users = new UserRepository(basePath);
            Students = new StudentRepository(basePath);
            Faculty = new FacultyRepository(basePath);
            Admins = new AdminRepository(basePath);
            Courses = new CourseRepository(basePath);
            Enrollments = new EnrollmentRepository(basePath);
            Grades = new GradeRepository(basePath);
            SchoolPrograms = new SchoolProgramRepository(basePath);
            Notifications = new NotificationRepository(basePath);
        }

        public void SaveChanges()
        {
            ((IWriteRepository<Entities.User>)Users).Save();
            ((IWriteRepository<Entities.Student>)Students).Save();
            ((IWriteRepository<Entities.Faculty>)Faculty).Save();
            ((IWriteRepository<Entities.Admin>)Admins).Save();
            ((IWriteRepository<Entities.Course>)Courses).Save();
            ((IWriteRepository<Entities.Enrollment>)Enrollments).Save();
            ((IWriteRepository<Entities.Grade>)Grades).Save();
            ((IWriteRepository<Entities.SchoolProgram>)SchoolPrograms).Save();
            ((IWriteRepository<Entities.Notification>)Notifications).Save();
        }
    }
}