using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // Thêm dòng này
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;
using SchoolManagementSystem.Controllers.StudentControllers;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Models.Student;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SchoolManagementSystem.IntegrationTests.Controllers
{
    public class StudentControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly Mock<IStudentService> _studentServiceMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly StudentController _controller;

        public StudentControllerTests()
        {
            _studentServiceMock = new Mock<IStudentService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            // Use reflection to set the private static _instance to null and create a new instance
            var instanceField = typeof(StudentService).GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            instanceField.SetValue(null, null);

            var studentService = StudentService.GetInstance();

            // Use reflection to set the private _unitOfWork field
            var unitOfWorkField = typeof(StudentService).GetField("_unitOfWork", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            unitOfWorkField.SetValue(studentService, _unitOfWorkMock.Object);

            _controller = new StudentController();

            // Use reflection to set the private _studentService field
            var studentServiceField = typeof(StudentController).GetField("_studentService", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            studentServiceField.SetValue(_controller, _studentServiceMock.Object);
        }

        private void SetupControllerSession(string userId)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new MockHttpSession();
            if (!string.IsNullOrEmpty(userId))
            {
                httpContext.Session.SetString("_UserId", userId);
            }
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()); // Thêm dòng này
        }

        [Fact]
        public void Dashboard_ValidSession_ReturnsViewWithStudentViewModel()
        {
            // Arrange
            string userId = "U001";
            SetupControllerSession(userId);

            var studentViewModel = new StudentViewModel
            {
                StudentId = "S001",
                StudentName = "student1",
                ProgramName = "Computer Science",
                Courses = new List<CourseViewModel>(),
                RecentGrades = new List<GradeViewModel>(),
                Notifications = new List<NotificationViewModel>()
            };
            _studentServiceMock.Setup(s => s.GetStudentDashboard(userId)).Returns(studentViewModel);

            // Act
            var result = _controller.Dashboard() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<StudentViewModel>(result.Model);
            var model = result.Model as StudentViewModel;
            Assert.Equal("S001", model.StudentId);
        }

        [Fact]
        public void Dashboard_SessionExpired_RedirectsToLogin()
        {
            // Arrange
            SetupControllerSession(null);

            // Act
            var result = _controller.Dashboard() as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
            Assert.Equal("Account", result.ControllerName);
            Assert.Equal("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public void Courses_ValidSession_ReturnsViewWithCourses()
        {
            // Arrange
            string userId = "U001";
            SetupControllerSession(userId);

            var studentViewModel = new StudentViewModel { StudentId = "S001" };
            var courses = new List<CourseViewModel>
            {
                new CourseViewModel { CourseId = "C001", CourseName = "Math", Credits = 3 }
            };
            _studentServiceMock.Setup(s => s.GetStudentDashboard(userId)).Returns(studentViewModel);
            _studentServiceMock.Setup(s => s.GetStudentCourses("S001")).Returns(courses);

            // Act
            var result = _controller.Courses() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<CourseViewModel>>(result.Model);
            var model = result.Model as List<CourseViewModel>;
            Assert.Single(model);
            Assert.Equal("C001", model[0].CourseId);
        }

        [Fact]
        public void CourseDetail_CourseExists_ReturnsViewWithCourse()
        {
            // Arrange
            string userId = "U001";
            string courseId = "C001";
            SetupControllerSession(userId);

            var studentViewModel = new StudentViewModel { StudentId = "S001" };
            var courses = new List<CourseViewModel>
            {
                new CourseViewModel { CourseId = "C001", CourseName = "Math", Credits = 3 }
            };
            _studentServiceMock.Setup(s => s.GetStudentDashboard(userId)).Returns(studentViewModel);
            _studentServiceMock.Setup(s => s.GetStudentCourses("S001")).Returns(courses);

            // Act
            var result = _controller.CourseDetail(courseId) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<CourseViewModel>(result.Model);
            var model = result.Model as CourseViewModel;
            Assert.Equal("C001", model.CourseId);
        }

        [Fact]
        public void Profile_ValidSession_ReturnsViewWithProfile()
        {
            // Arrange
            string userId = "U001";
            SetupControllerSession(userId);

            var studentViewModel = new StudentViewModel { StudentId = "S001" };
            var profile = new StudentProfileViewModel
            {
                StudentId = "S001",
                Username = "student1",
                ProgramName = "Computer Science",
                TotalCourses = 2
            };
            _studentServiceMock.Setup(s => s.GetStudentDashboard(userId)).Returns(studentViewModel);
            _studentServiceMock.Setup(s => s.GetStudentProfile("S001")).Returns(profile);

            // Act
            var result = _controller.Profile() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<StudentProfileViewModel>(result.Model);
            var model = result.Model as StudentProfileViewModel;
            Assert.Equal("S001", model.StudentId);
        }
    }

    // Helper class to mock HttpSession
    public class MockHttpSession : ISession
    {
        private readonly Dictionary<string, byte[]> _sessionStorage = new Dictionary<string, byte[]>();

        public IEnumerable<string> Keys => _sessionStorage.Keys;

        public string Id => "MockSessionId";

        public bool IsAvailable => true;

        public void Clear() => _sessionStorage.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _sessionStorage.Remove(key);

        public void Set(string key, byte[] value) => _sessionStorage[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value);

        public void SetString(string key, string value)
        {
            Set(key, System.Text.Encoding.UTF8.GetBytes(value));
        }

        public string GetString(string key)
        {
            if (TryGetValue(key, out var value))
            {
                return System.Text.Encoding.UTF8.GetString(value);
            }
            return null;
        }
    }
}