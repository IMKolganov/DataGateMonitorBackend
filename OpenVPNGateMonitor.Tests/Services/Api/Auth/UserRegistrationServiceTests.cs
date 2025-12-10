using Microsoft.AspNetCore.Identity;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.UserCredentialTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;

namespace OpenVPNGateMonitor.Tests.Services.Api.Auth
{
    public class UserRegistrationServiceTests
    {
        private readonly Mock<IPasswordHasher<User>> passwordHasherMock;
        private readonly Mock<ICommandService<User, int>> userCommandServiceMock;
        private readonly Mock<ICommandService<UserCredential, int>> userCredentialCommandServiceMock;
        private readonly Mock<IUserCredentialQueryService> userCredentialQueryServiceMock;
        private readonly Mock<IUserQueryService> userQueryServiceMock;

        private readonly UserRegistrationService service;

        public UserRegistrationServiceTests()
        {
            passwordHasherMock = new Mock<IPasswordHasher<User>>();
            userCommandServiceMock = new Mock<ICommandService<User, int>>();
            userCredentialCommandServiceMock = new Mock<ICommandService<UserCredential, int>>();
            userCredentialQueryServiceMock = new Mock<IUserCredentialQueryService>();
            userQueryServiceMock = new Mock<IUserQueryService>();

            service = new UserRegistrationService(
                passwordHasherMock.Object,
                userCommandServiceMock.Object,
                userCredentialCommandServiceMock.Object,
                userCredentialQueryServiceMock.Object,
                userQueryServiceMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_ValidRequest_CreatesUserAndCredentialAndReturnsResponse()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                DisplayName = "Test User",
                Email = "test@example.com",
                Login = "test-login",
                Password = "StrongPass123!",
                ConfirmPassword = "StrongPass123!"
            };

            var normalizedLogin = request.Login.ToUpperInvariant();

            userCredentialQueryServiceMock
                .Setup(s => s.GetByNormalizedLogin(normalizedLogin, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserCredential?)null);

            userQueryServiceMock
                .Setup(s => s.AnyByEmailAsync(request.Email!, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var createdUser = (User?)null;
            userCommandServiceMock
                .Setup(s => s.AddAsync(It.IsAny<User>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User u, bool _, CancellationToken _) =>
                {
                    u.Id = 42;
                    createdUser = u;
                    return u;
                });

            passwordHasherMock
                .Setup(h => h.HashPassword(It.IsAny<User>(), request.Password))
                .Returns("HASHED_PASSWORD");

            UserCredential? createdCredential = null;
            userCredentialCommandServiceMock
                .Setup(s => s.AddAsync(It.IsAny<UserCredential>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserCredential c, bool _, CancellationToken _) =>
                {
                    createdCredential = c;
                    return c;
                });

            userCommandServiceMock
                .Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var ct = CancellationToken.None;

            // Act
            var result = await service.RegisterAsync(request, ct);

            // Assert
            Assert.Equal(42, result.UserId);
            Assert.Equal(request.DisplayName, result.DisplayName);
            Assert.Equal(request.Email, result.Email);
            Assert.True(result.HasDashboardAccess);

            Assert.NotNull(createdUser);
            Assert.Equal(request.DisplayName, createdUser!.DisplayName);
            Assert.Equal(request.Email, createdUser.Email);
            Assert.False(createdUser.IsAdmin);
            Assert.False(createdUser.IsBlocked);
            Assert.True(createdUser.HasDashboardAccess);

            Assert.NotNull(createdCredential);
            Assert.Equal(createdUser.Id, createdCredential!.UserId);
            Assert.Equal(request.Login, createdCredential.Login);
            Assert.Equal(normalizedLogin, createdCredential.NormalizedLogin);
            Assert.Equal("HASHED_PASSWORD", createdCredential.PasswordHash);
            Assert.Equal("AspNetCoreV3", createdCredential.PasswordAlgo);

            userCredentialQueryServiceMock.Verify(
                s => s.GetByNormalizedLogin(normalizedLogin, ct),
                Times.Once);

            userQueryServiceMock.Verify(
                s => s.AnyByEmailAsync(request.Email!, ct),
                Times.Once);

            userCommandServiceMock.Verify(
                s => s.AddAsync(It.IsAny<User>(), false, ct),
                Times.Once);

            userCredentialCommandServiceMock.Verify(
                s => s.AddAsync(It.IsAny<UserCredential>(), false, ct),
                Times.Once);

            userCommandServiceMock.Verify(
                s => s.SaveChangesAsync(ct),
                Times.Once);

            passwordHasherMock.Verify(
                h => h.HashPassword(It.IsAny<User>(), request.Password),
                Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WhenLoginAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                DisplayName = "Test User",
                Email = "test@example.com",
                Login = "existing",
                Password = "StrongPass123!",
                ConfirmPassword = "StrongPass123!"
            };

            var normalizedLogin = request.Login.ToUpperInvariant();

            userCredentialQueryServiceMock
                .Setup(s => s.GetByNormalizedLogin(normalizedLogin, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserCredential { NormalizedLogin = normalizedLogin });

            var ct = CancellationToken.None;

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.RegisterAsync(request, ct));

            // Assert
            Assert.Equal("Login is already in use.", ex.Message);

            userCommandServiceMock.Verify(
                s => s.AddAsync(It.IsAny<User>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never);

            userCredentialCommandServiceMock.Verify(
                s => s.AddAsync(It.IsAny<UserCredential>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never);

            userCommandServiceMock.Verify(
                s => s.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_WhenEmailAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                DisplayName = "Test User",
                Email = "taken@example.com",
                Login = "new-login",
                Password = "StrongPass123!",
                ConfirmPassword = "StrongPass123!"
            };

            var normalizedLogin = request.Login.ToUpperInvariant();

            userCredentialQueryServiceMock
                .Setup(s => s.GetByNormalizedLogin(normalizedLogin, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserCredential?)null);

            userQueryServiceMock
                .Setup(s => s.AnyByEmailAsync(request.Email!, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var ct = CancellationToken.None;

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.RegisterAsync(request, ct));

            // Assert
            Assert.Equal("Email is already in use.", ex.Message);

            userCommandServiceMock.Verify(
                s => s.AddAsync(It.IsAny<User>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never);

            userCredentialCommandServiceMock.Verify(
                s => s.AddAsync(It.IsAny<UserCredential>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never);

            userCommandServiceMock.Verify(
                s => s.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_WhenPasswordsDoNotMatch_ThrowsArgumentException()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                DisplayName = "Test User",
                Email = "test@example.com",
                Login = "login",
                Password = "StrongPass123!",
                ConfirmPassword = "OtherPass456!"
            };

            var ct = CancellationToken.None;

            // Act
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => service.RegisterAsync(request, ct));

            // Assert
            Assert.Equal("Passwords do not match.", ex.Message);

            userCredentialQueryServiceMock.Verify(
                s => s.GetByNormalizedLogin(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);

            userCommandServiceMock.Verify(
                s => s.AddAsync(It.IsAny<User>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(null, "login", "pass1234", "Display name is required.")]
        [InlineData("   ", "login", "pass1234", "Display name is required.")]
        [InlineData("User", null, "pass1234", "Login is required.")]
        [InlineData("User", "   ", "pass1234", "Login is required.")]
        [InlineData("User", "login", null, "Password is required.")]
        [InlineData("User", "login", "   ", "Password is required.")]
        [InlineData("User", "login", "short", "Password must be at least 8 characters long.")]
        public async Task RegisterAsync_InvalidBasicFields_ThrowsArgumentException(
            string? displayName,
            string? login,
            string? password,
            string expectedMessage)
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                DisplayName = displayName,
                Email = "test@example.com",
                Login = login,
                Password = password,
                ConfirmPassword = password
            };

            var ct = CancellationToken.None;

            // Act
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => service.RegisterAsync(request, ct));

            // Assert
            Assert.Equal(expectedMessage, ex.Message);

            userCredentialQueryServiceMock.Verify(
                s => s.GetByNormalizedLogin(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);

            userCommandServiceMock.Verify(
                s => s.AddAsync(It.IsAny<User>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
