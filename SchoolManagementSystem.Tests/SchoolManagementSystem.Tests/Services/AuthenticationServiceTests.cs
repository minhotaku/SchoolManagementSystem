using Moq;
using SchoolManagementSystem.Entities;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Data;
using Xunit;

namespace SchoolManagementSystem.Tests.Services
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly AuthenticationService _authenticationService;

        public AuthenticationServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            // Use reflection to set the private static _instance to null and create a new instance
            var instanceField = typeof(AuthenticationService).GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            instanceField.SetValue(null, null);

            _authenticationService = AuthenticationService.GetInstance();

            // Use reflection to set the private _unitOfWork field
            var unitOfWorkField = typeof(AuthenticationService).GetField("_unitOfWork", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            unitOfWorkField.SetValue(_authenticationService, _unitOfWorkMock.Object);
        }

        [Fact]
        public void Authenticate_ValidCredentials_ReturnsUser()
        {
            // Arrange
            string username = "admin";
            string password = "password123";
            string hashedPassword = SchoolManagementSystem.Utils.HashPassword.ComputeSha256Hash(password);

            var user = new User { Username = username, PasswordHash = hashedPassword };
            _unitOfWorkMock.Setup(u => u.Users.GetByUsername(username)).Returns(user);

            // Act
            var result = _authenticationService.Authenticate(username, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(username, result.Username);
        }

        [Fact]
        public void Authenticate_WrongPassword_ReturnsNull()
        {
            // Arrange
            string username = "admin";
            string password = "wrongpassword";
            string correctHashedPassword = SchoolManagementSystem.Utils.HashPassword.ComputeSha256Hash("password123");

            var user = new User { Username = username, PasswordHash = correctHashedPassword };
            _unitOfWorkMock.Setup(u => u.Users.GetByUsername(username)).Returns(user);

            // Act
            var result = _authenticationService.Authenticate(username, password);

            // Assert
            Assert.Null(result);
        }
    }
}