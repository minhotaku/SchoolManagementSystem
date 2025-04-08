using System;
using System.Collections.Generic;
using System.Linq;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Models.Student;
using SchoolManagementSystem.Services.Interfaces;

namespace SchoolManagementSystem.Services.Implementation
{
    public class StudentService : IStudentService
    {
        // Singleton pattern
        private static StudentService _instance;
        private static readonly object _lock = new object();

        private readonly IUnitOfWork _unitOfWork;

        public static StudentService GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new StudentService();
                    }
                }
            }
            return _instance;
        }

        private StudentService()
        {
            _unitOfWork = UnitOfWork.GetInstance();
        }

        public StudentViewModel GetStudentDashboard(string userId)
        {
            // Lấy thông tin học sinh từ userId
            var student = _unitOfWork.Students.GetByUserId(userId);
            if (student == null)
            {
                throw new Exception("Không tìm thấy thông tin học sinh!");
            }

            // Lấy thông tin chương trình học
            var program = _unitOfWork.SchoolPrograms.GetById(student.SchoolProgramId);

            // Lấy thông tin user
            var user = _unitOfWork.Users.GetById(userId);

            // Tạo ViewModel
            var viewModel = new StudentViewModel
            {
                StudentId = student.StudentId,
                StudentName = user.Username,
                ProgramName = program?.SchoolProgramName ?? "Chưa có thông tin",
                Courses = GetStudentCourses(student.StudentId),
                RecentGrades = GetStudentGrades(student.StudentId).Take(5).ToList(),
                Notifications = GetStudentNotifications(userId).Take(5).ToList()
            };

            return viewModel;
        }

        public List<CourseViewModel> GetStudentCourses(string studentId)
        {
            // Lấy danh sách khóa học của học sinh
            var enrollments = _unitOfWork.Enrollments.GetByStudent(studentId).ToList();
            var courses = new List<CourseViewModel>();

            foreach (var enrollment in enrollments)
            {
                var course = _unitOfWork.Courses.GetById(enrollment.CourseId);
                if (course != null)
                {
                    var faculty = _unitOfWork.Faculty.GetById(course.FacultyId);
                    var user = faculty != null ? _unitOfWork.Users.GetById(faculty.UserId) : null;

                    courses.Add(new CourseViewModel
                    {
                        CourseId = course.CourseId,
                        CourseName = course.CourseName,
                        Credits = course.Credits,
                        EnrollmentId = enrollment.EnrollmentId,
                        Semester = enrollment.Semester,
                        FacultyName = user?.Username ?? "Chưa phân công"
                    });
                }
            }

            return courses;
        }

        public List<GradeViewModel> GetStudentGrades(string studentId)
        {
            var enrollments = _unitOfWork.Enrollments.GetByStudent(studentId).ToList();
            var gradeViewModels = new List<GradeViewModel>();

            foreach (var enrollment in enrollments)
            {
                var course = _unitOfWork.Courses.GetById(enrollment.CourseId);
                var grades = _unitOfWork.Grades.GetByEnrollment(enrollment.EnrollmentId).ToList();

                if (course != null && grades.Any())
                {
                    var gradeViewModel = new GradeViewModel
                    {
                        EnrollmentId = enrollment.EnrollmentId,
                        CourseId = course.CourseId,
                        CourseName = course.CourseName,
                        Semester = enrollment.Semester,
                        Components = new List<GradeComponentViewModel>()
                    };

                    decimal totalScore = 0;
                    foreach (var grade in grades)
                    {
                        gradeViewModel.Components.Add(new GradeComponentViewModel
                        {
                            Component = grade.Component,
                            Score = grade.Score
                        });
                        totalScore += grade.Score;
                    }

                    // Tính điểm trung bình
                    gradeViewModel.AverageScore = Math.Round(totalScore / grades.Count, 2);
                    gradeViewModels.Add(gradeViewModel);
                }
            }

            return gradeViewModels.OrderByDescending(g => g.Semester).ToList();
        }

        public List<NotificationViewModel> GetStudentNotifications(string userId)
        {
            var notifications = _unitOfWork.Notifications.GetByUser(userId).ToList();
            var notificationViewModels = new List<NotificationViewModel>();

            foreach (var notification in notifications)
            {
                notificationViewModels.Add(new NotificationViewModel
                {
                    NotificationId = notification.NotificationId,
                    Message = notification.Message,
                    Timestamp = notification.Timestamp
                });
            }

            return notificationViewModels.OrderByDescending(n => n.Timestamp).ToList();
        }

        public StudentProfileViewModel GetStudentProfile(string studentId)
        {
            var student = _unitOfWork.Students.GetById(studentId);
            if (student == null)
            {
                throw new Exception("Không tìm thấy thông tin học sinh!");
            }

            var user = _unitOfWork.Users.GetById(student.UserId);
            var program = _unitOfWork.SchoolPrograms.GetById(student.SchoolProgramId);

            return new StudentProfileViewModel
            {
                StudentId = student.StudentId,
                Username = user?.Username ?? "Không có thông tin",
                ProgramName = program?.SchoolProgramName ?? "Chưa có thông tin",
                TotalCourses = _unitOfWork.Enrollments.GetByStudent(studentId).Count(),
                EnrollmentDate = "Chưa có thông tin" // Có thể thêm trường này vào Student entity sau này
            };
        }
    }
}