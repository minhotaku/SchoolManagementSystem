using System.Collections.Generic;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Interfaces;
using System;

namespace SchoolManagementSystem.Services.Implementation
{
    public class FacultyService : IFacultyService
    {
        private static FacultyService _instance;
        private static readonly object _lock = new object();
        private readonly IUnitOfWork _unitOfWork;

        public static FacultyService GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new FacultyService();
                    }
                }
            }
            return _instance;
        }

        private FacultyService()
        {
            _unitOfWork = UnitOfWork.GetInstance();
        }

        public IEnumerable<Course> GetCoursesByFaculty(string facultyId)
        {
            return _unitOfWork.Courses.GetByFaculty(facultyId);
        }

        public Course GetCourseDetails(string courseId)
        {
            return _unitOfWork.Courses.GetById(courseId);
        }

        public void UpdateCourse(Course course)
        {
            _unitOfWork.Courses.Update(course);
            _unitOfWork.SaveChanges();
        }

        public void AddCourse(Course course)
        {
            _unitOfWork.Courses.Add(course);
            _unitOfWork.SaveChanges();
        }

        public IEnumerable<Enrollment> GetEnrollmentsByCourse(string courseId)
        {
            return _unitOfWork.Enrollments.GetByCourse(courseId);
        }

        public IEnumerable<Grade> GetGradesByEnrollment(string enrollmentId)
        {
            return _unitOfWork.Grades.GetByEnrollment(enrollmentId);
        }

        public void AddGrade(Grade grade)
        {
            var allGrades = _unitOfWork.Grades.GetAll().ToList();
            int maxId = 0;
            if (allGrades.Any())
            {
                maxId = allGrades
                    .Select(g => int.Parse(g.GradeId.Substring(1)))
                    .Max();
            }
            grade.GradeId = "G" + (maxId + 1).ToString("D3");
            _unitOfWork.Grades.Add(grade);
            _unitOfWork.SaveChanges();
        }

        public void UpdateGrade(Grade grade)
        {
            _unitOfWork.Grades.Update(grade);
            _unitOfWork.SaveChanges();
        }

        public void DeleteGrade(string gradeId)
        {
            _unitOfWork.Grades.Delete(gradeId);
            _unitOfWork.SaveChanges();
        }

        public bool CanManageCourse(string facultyId, string courseId)
        {
            var course = _unitOfWork.Courses.GetById(courseId);
            return course != null && course.FacultyId == facultyId;
        }

        public decimal CalculateAverageScore(string enrollmentId)
        {
            var grades = _unitOfWork.Grades.GetByEnrollment(enrollmentId);
            if (!grades.Any())
            {
                return 0;
            }

            decimal totalScore = grades.Sum(g => g.Score);
            return totalScore / grades.Count();
        }

        public string ClassifyResult(decimal averageScore)
        {
            if (averageScore < 65)
                return "Fail";
            else if (averageScore >= 65 && averageScore < 80)
                return "Pass";
            else if (averageScore >= 80 && averageScore < 90)
                return "Merit";
            else
                return "Distinction";
        }

        public Student GetStudentById(string studentId)
        {
            return _unitOfWork.Students.GetById(studentId);
        }

        public string GetSchoolProgramName(string schoolProgramId)
        {
            var program = _unitOfWork.SchoolPrograms.GetById(schoolProgramId);
            return program?.SchoolProgramName ?? "Unknown";
        }

        public void SendNotification(string userId, string message)
        {
            var notification = new Notification
            {
                NotificationId = "N" + (_unitOfWork.Notifications.GetAll().Count() + 1).ToString("D3"),
                UserId = userId,
                Message = message,
                Timestamp = DateTime.Now
            };
            _unitOfWork.Notifications.Add(notification);
            _unitOfWork.SaveChanges();
        }

        public (decimal ClassAverage, Dictionary<string, int> ClassificationStats) GetCourseStatistics(string courseId)
        {
            var enrollments = _unitOfWork.Enrollments.GetByCourse(courseId) ?? new List<Enrollment>();
            if (!enrollments.Any())
            {
                return (0, new Dictionary<string, int>
                {
                    { "Fail", 0 },
                    { "Pass", 0 },
                    { "Merit", 0 },
                    { "Distinction", 0 }
                });
            }

            var averageScores = new List<decimal>();
            var classificationStats = new Dictionary<string, int>
            {
                { "Fail", 0 },
                { "Pass", 0 },
                { "Merit", 0 },
                { "Distinction", 0 }
            };

            foreach (var enrollment in enrollments)
            {
                var averageScore = CalculateAverageScore(enrollment.EnrollmentId);
                averageScores.Add(averageScore);
                var classification = ClassifyResult(averageScore);
                classificationStats[classification]++;
            }

            decimal classAverage = averageScores.Any() ? averageScores.Average() : 0;
            return (classAverage, classificationStats);
        }

        public string GetFacultyIdByUserId(string userId)
        {
            var faculty = _unitOfWork.Faculty.GetAll().FirstOrDefault(f => f.UserId == userId);
            return faculty?.FacultyId;
        }
    }
}