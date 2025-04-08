using SchoolManagementSystem.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolManagementSystem.Tests.Utils
{
    public class HashPasswordTests
    {
        [Fact]
        public void ComputeSha256Hash_ValidInput_ReturnsHashedPassword()
        {
            // Arrange
            string password = "password123";

            // Act
            string hashedPassword = HashPassword.ComputeSha256Hash(password);

            // Assert
            Assert.NotNull(hashedPassword);
            Assert.Equal(64, hashedPassword.Length); // SHA256 hash length is 64 characters
            Assert.NotEqual(password, hashedPassword); // Ensure the password is hashed
        }
    }
}
