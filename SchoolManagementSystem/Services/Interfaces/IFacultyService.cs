using System.Collections.Generic;
using SchoolManagementSystem.Entities;

namespace SchoolManagementSystem.Services.Interfaces
{
    public interface IFacultyService
    {
        IEnumerable<Course> GetCoursesByFaculty(string facultyId);
        Course GetCourseDetails(string courseId);
        void UpdateCourse(Course course);
        void AddCourse(Course course);
        IEnumerable<Enrollment> GetEnrollmentsByCourse(string courseId);
        IEnumerable<Grade> GetGradesByEnrollment(string enrollmentId);
        void AddGrade(Grade grade);
        void UpdateGrade(Grade grade);
        void DeleteGrade(string gradeId);
        bool CanManageCourse(string facultyId, string courseId);
        decimal CalculateAverageScore(string enrollmentId);
        string ClassifyResult(decimal averageScore);
        Student GetStudentById(string studentId);
        string GetSchoolProgramName(string schoolProgramId);
        void SendNotification(string userId, string message);
        (decimal ClassAverage, Dictionary<string, int> ClassificationStats) GetCourseStatistics(string courseId);
        string GetFacultyIdByUserId(string userId);
    }
}