using Moq;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Models.Student;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Data;
using System;
using System.Collections.Generic;
using Xunit;

namespace SchoolManagementSystem.Tests.Services
{
    public class StudentServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly StudentService _studentService;

        public StudentServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            // Use reflection to set the private static _instance to null and create a new instance
            var instanceField = typeof(StudentService).GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            instanceField.SetValue(null, null);

            _studentService = StudentService.GetInstance();

            // Use reflection to set the private _unitOfWork field
            var unitOfWorkField = typeof(StudentService).GetField("_unitOfWork", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            unitOfWorkField.SetValue(_studentService, _unitOfWorkMock.Object);
        }

        [Fact]
        public void GetStudentDashboard_ValidUserId_ReturnsStudentViewModel()
        {
            // Arrange
            string userId = "U001";
            string studentId = "S001";
            var student = new Student { StudentId = studentId, UserId = userId, SchoolProgramId = "P001" };
            var program = new SchoolProgram { SchoolProgramId = "P001", SchoolProgramName = "Computer Science" };
            var user = new User { UserId = userId, Username = "student1" };

            _unitOfWorkMock.Setup(u => u.Students.GetByUserId(userId)).Returns(student);
            _unitOfWorkMock.Setup(u => u.SchoolPrograms.GetById("P001")).Returns(program);
            _unitOfWorkMock.Setup(u => u.Users.GetById(userId)).Returns(user);
            _unitOfWorkMock.Setup(u => u.Enrollments.GetByStudent(studentId)).Returns(new List<Enrollment>());
            _unitOfWorkMock.Setup(u => u.Grades.GetByEnrollment(It.IsAny<string>())).Returns(new List<Grade>());
            _unitOfWorkMock.Setup(u => u.Notifications.GetByUser(userId)).Returns(new List<Notification>());

            // Act
            var result = _studentService.GetStudentDashboard(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(studentId, result.StudentId);
            Assert.Equal("student1", result.StudentName);
            Assert.Equal("Computer Science", result.ProgramName);
        }

        [Fact]
        public void GetStudentProfile_StudentNotFound_ThrowsException()
        {
            // Arrange
            string studentId = "S999";
            _unitOfWorkMock.Setup(u => u.Students.GetById(studentId)).Returns((Student)null);

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => _studentService.GetStudentProfile(studentId));
            Assert.Equal("Không tìm thấy thông tin học sinh!", exception.Message);
        }
    }
}