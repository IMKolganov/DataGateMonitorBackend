// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Configuration;
// using Moq;
// using OpenVPNGateMonitor.Controllers;
// using OpenVPNGateMonitor.Models;
// using OpenVPNGateMonitor.Models.Helpers.Auth;
// using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;
// using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;
// using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;
// using OpenVPNGateMonitor.SharedModels.Responses;
//
// namespace OpenVPNGateMonitor.Tests.Controllers
// {
//     public class AuthControllerTests
//     {
//         private readonly Mock<IApplicationService> appServiceMock;
//         private readonly Mock<IMicroserviceTokenService> microserviceTokenServiceMock;
//         private readonly Mock<IUserRegistrationService> userRegistrationServiceMock;
//         private readonly Mock<IGoogleAuthService> googleAuthServiceMock;
//         private readonly IConfiguration configuration;
//         private readonly AuthController controller;
//
//         public AuthControllerTests()
//         {
//             appServiceMock = new Mock<IApplicationService>();
//             microserviceTokenServiceMock = new Mock<IMicroserviceTokenService>();
//             userRegistrationServiceMock = new Mock<IUserRegistrationService>();
//             googleAuthServiceMock = new Mock<IGoogleAuthService>();
//
//             configuration = new ConfigurationBuilder()
//                 .AddInMemoryCollection(new Dictionary<string, string?>
//                 {
//                     ["Jwt:Secret"] = "VeryStrongTestSecretKey1234567890"
//                 })
//                 .Build();
//
//             controller = new AuthController(
//                 configuration,
//                 appServiceMock.Object,
//                 microserviceTokenServiceMock.Object,
//                 userRegistrationServiceMock.Object,
//                 googleAuthServiceMock.Object);
//         }
//
//         // ---------------------------
//         // GetSystemStatus
//         // ---------------------------
//
//         [Fact]
//         public async Task GetSystemStatus_ReturnsOk_WithSystemSetTrue()
//         {
//             // Arrange
//             appServiceMock
//                 .Setup(s => s.IsSystemApplicationSetAsync(It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(true);
//
//             var ct = CancellationToken.None;
//
//             // Act
//             var result = await controller.GetSystemStatus(ct);
//
//             // Assert
//             var ok = Assert.IsType<OkObjectResult>(result.Result);
//             var response = Assert.IsType<ApiResponse<SystemSecretStatusResponse>>(ok.Value);
//
//             Assert.True(response.Success);
//             Assert.Equal("Success", response.Message);
//             Assert.NotNull(response.Data);
//             Assert.True(response.Data.SystemSet);
//
//             appServiceMock.Verify(s => s.IsSystemApplicationSetAsync(ct), Times.Once);
//         }
//
//         // ---------------------------
//         // SetSystemSecret
//         // ---------------------------
//
//         [Fact]
//         public async Task SetSystemSecret_WhenSystemAlreadySet_ReturnsBadRequest()
//         {
//             // Arrange
//             var request = new SetSecretRequest
//             {
//                 ClientId = "sys-client",
//                 ClientSecret = "new-secret"
//             };
//
//             var existingSystemApp = new ClientApplication
//             {
//                 ClientId = request.ClientId,
//                 ClientSecret = "already-set-secret",
//                 Name = "SystemApp",
//                 IsSystem = true
//             };
//
//             appServiceMock
//                 .Setup(s => s.GetApplicationSystemByClientIdAsync(request.ClientId, It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(existingSystemApp);
//
//             var ct = CancellationToken.None;
//
//             // Act
//             var result = await controller.SetSystemSecret(request, ct);
//
//             // Assert
//             var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
//             var response = Assert.IsType<ApiResponse<AuthResponse>>(badRequest.Value);
//
//             Assert.False(response.Success);
//             Assert.Equal("System application is already set", response.Message);
//             Assert.Null(response.Data);
//
//             appServiceMock.Verify(
//                 s => s.GetApplicationSystemByClientIdAsync(request.ClientId, ct),
//                 Times.Once);
//
//             appServiceMock.Verify(
//                 s => s.UpdateApplicationAsync(It.IsAny<ClientApplication>(), It.IsAny<CancellationToken>()),
//                 Times.Never);
//         }
//
//         [Fact]
//         public async Task SetSystemSecret_WhenSystemNotExists_CreatesSystemAndReturnsOk()
//         {
//             // Arrange
//             var request = new SetSecretRequest
//             {
//                 ClientId = "new-system-client",
//                 ClientSecret = "super-secret"
//             };
//
//             appServiceMock
//                 .Setup(s => s.GetApplicationSystemByClientIdAsync(request.ClientId, It.IsAny<CancellationToken>()))
//                 .ReturnsAsync((ClientApplication?)null);
//
//             ClientApplication? savedEntity = null;
//
//             appServiceMock
//                 .Setup(s => s.UpdateApplicationAsync(It.IsAny<ClientApplication>(), It.IsAny<CancellationToken>()))
//                 .ReturnsAsync((ClientApplication entity, CancellationToken _) =>
//                 {
//                     savedEntity = entity;
//                     return entity;
//                 });
//
//             var ct = CancellationToken.None;
//
//             // Act
//             var result = await controller.SetSystemSecret(request, ct);
//
//             // Assert
//             var ok = Assert.IsType<OkObjectResult>(result.Result);
//             var response = Assert.IsType<ApiResponse<AuthResponse>>(ok.Value);
//
//             Assert.True(response.Success);
//             Assert.Equal("Success", response.Message);
//             Assert.NotNull(response.Data);
//             Assert.Equal("ClientSecret set successfully", response.Data.Message);
//
//             appServiceMock.Verify(
//                 s => s.GetApplicationSystemByClientIdAsync(request.ClientId, ct),
//                 Times.Once);
//
//             appServiceMock.Verify(
//                 s => s.UpdateApplicationAsync(It.IsAny<ClientApplication>(), ct),
//                 Times.Once);
//
//             Assert.NotNull(savedEntity);
//             Assert.Equal(request.ClientId, savedEntity!.ClientId);
//             Assert.True(savedEntity.IsSystem);
//             Assert.False(string.IsNullOrWhiteSpace(savedEntity.ClientSecret));
//         }
//
//         // ---------------------------
//         // GenerateToken
//         // ---------------------------
//
//         [Fact]
//         public async Task GenerateToken_WhenAppNotFound_ReturnsUnauthorized()
//         {
//             // Arrange
//             var request = new TokenRequest
//             {
//                 ClientId = "unknown-client",
//                 ClientSecret = "secret"
//             };
//
//             appServiceMock
//                 .Setup(s => s.GetApplicationByClientIdAsync(request.ClientId, It.IsAny<CancellationToken>()))
//                 .ReturnsAsync((ClientApplication?)null);
//
//             var ct = CancellationToken.None;
//
//             // Act
//             var result = await controller.GenerateToken(request, ct);
//
//             // Assert
//             var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
//             var response = Assert.IsType<ApiResponse<TokenResponse>>(unauthorized.Value);
//
//             Assert.False(response.Success);
//             Assert.Equal("Invalid credentials", response.Message);
//             Assert.Null(response.Data);
//
//             appServiceMock.Verify(
//                 s => s.GetApplicationByClientIdAsync(request.ClientId, ct),
//                 Times.Once);
//         }
//
//         [Fact]
//         public async Task GenerateToken_SystemApp_WithHashedSecret_ReturnsOkWithToken()
//         {
//             // Arrange
//             var request = new TokenRequest
//             {
//                 ClientId = "sys-client",
//                 ClientSecret = "plain-secret"
//             };
//
//             var hashed = BCrypt.Net.BCrypt.HashPassword(request.ClientSecret);
//
//             var systemApp = new ClientApplication
//             {
//                 ClientId = request.ClientId,
//                 ClientSecret = hashed,
//                 Name = "SystemApp",
//                 IsSystem = true
//             };
//
//             appServiceMock
//                 .Setup(s => s.GetApplicationByClientIdAsync(request.ClientId, It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(systemApp);
//
//             var ct = CancellationToken.None;
//
//             // Act
//             var result = await controller.GenerateToken(request, ct);
//
//             // Assert
//             var ok = Assert.IsType<OkObjectResult>(result.Result);
//             var response = Assert.IsType<ApiResponse<TokenResponse>>(ok.Value);
//
//             Assert.True(response.Success);
//             Assert.Equal("Success", response.Message);
//             Assert.NotNull(response.Data);
//             Assert.False(string.IsNullOrWhiteSpace(response.Data.Token));
//
//             Assert.True(response.Data.Expiration > DateTimeOffset.UtcNow);
//
//             appServiceMock.Verify(
//                 s => s.GetApplicationByClientIdAsync(request.ClientId, ct),
//                 Times.Once);
//         }
//
//         [Fact]
//         public async Task GenerateToken_NonSystemApp_WithWrongSecret_ReturnsUnauthorized()
//         {
//             // Arrange
//             var request = new TokenRequest
//             {
//                 ClientId = "app-client",
//                 ClientSecret = "wrong-secret"
//             };
//
//             var app = new ClientApplication
//             {
//                 ClientId = request.ClientId,
//                 ClientSecret = "real-secret",
//                 Name = "NormalApp",
//                 IsSystem = false
//             };
//
//             appServiceMock
//                 .Setup(s => s.GetApplicationByClientIdAsync(request.ClientId, It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(app);
//
//             var ct = CancellationToken.None;
//
//             // Act
//             var result = await controller.GenerateToken(request, ct);
//
//             // Assert
//             var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
//             var response = Assert.IsType<ApiResponse<TokenResponse>>(unauthorized.Value);
//
//             Assert.False(response.Success);
//             Assert.Equal("Invalid credentials", response.Message);
//             Assert.Null(response.Data);
//
//             appServiceMock.Verify(
//                 s => s.GetApplicationByClientIdAsync(request.ClientId, ct),
//                 Times.Once);
//         }
//
//         [Fact]
//         public async Task GenerateToken_NonSystemApp_WithCorrectSecret_ReturnsOkWithToken()
//         {
//             // Arrange
//             var request = new TokenRequest
//             {
//                 ClientId = "app-client",
//                 ClientSecret = "real-secret"
//             };
//
//             var app = new ClientApplication
//             {
//                 ClientId = request.ClientId,
//                 ClientSecret = "real-secret",
//                 Name = "NormalApp",
//                 IsSystem = false
//             };
//
//             appServiceMock
//                 .Setup(s => s.GetApplicationByClientIdAsync(request.ClientId, It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(app);
//
//             var ct = CancellationToken.None;
//
//             // Act
//             var result = await controller.GenerateToken(request, ct);
//
//             // Assert
//             var ok = Assert.IsType<OkObjectResult>(result.Result);
//             var response = Assert.IsType<ApiResponse<TokenResponse>>(ok.Value);
//
//             Assert.True(response.Success);
//             Assert.Equal("Success", response.Message);
//             Assert.NotNull(response.Data);
//             Assert.False(string.IsNullOrWhiteSpace(response.Data.Token));
//             Assert.True(response.Data.Expiration > DateTimeOffset.UtcNow);
//
//             appServiceMock.Verify(
//                 s => s.GetApplicationByClientIdAsync(request.ClientId, ct),
//                 Times.Once);
//         }
//
//         // ---------------------------
//         // GetPublicKeyForMicroservice
//         // ---------------------------
//
//         [Fact]
//         public void GetPublicKeyForMicroservice_WhenPinTooSmall_ReturnsBadRequest()
//         {
//             // Arrange
//             const int pin = 9999;
//
//             // Act
//             var result = controller.GetPublicKeyForMicroservice(pin);
//
//             // Assert
//             var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
//             var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);
//
//             Assert.False(response.Success);
//             Assert.Equal("Invalid pin", response.Message);
//             Assert.Null(response.Data);
//
//             microserviceTokenServiceMock.Verify(
//                 s => s.GetPublicKeyPem(),
//                 Times.Never);
//         }
//
//         [Fact]
//         public void GetPublicKeyForMicroservice_WhenPinValid_ReturnsOkWithKey()
//         {
//             // Arrange
//             const int pin = 12345;
//             const string publicKey = "-----BEGIN PUBLIC KEY-----\nTEST\n-----END PUBLIC KEY-----";
//
//             microserviceTokenServiceMock
//                 .Setup(s => s.GetPublicKeyPem())
//                 .Returns(publicKey);
//
//             // Act
//             var result = controller.GetPublicKeyForMicroservice(pin);
//
//             // Assert
//             var ok = Assert.IsType<OkObjectResult>(result.Result);
//             var response = Assert.IsType<ApiResponse<string>>(ok.Value);
//
//             Assert.True(response.Success);
//             Assert.Equal("Success", response.Message);
//             Assert.Equal(publicKey, response.Data);
//
//             microserviceTokenServiceMock.Verify(
//                 s => s.GetPublicKeyPem(),
//                 Times.Once);
//         }
//
//         // ---------------------------
//         // Register
//         // ---------------------------
//
//         [Fact]
//         public async Task Register_WhenValidRequest_ReturnsOkWithSuccessResponse()
//         {
//             // Arrange
//             var request = new RegisterUserRequest
//             {
//                 DisplayName = "Test User",
//                 Email = "test@example.com",
//                 Login = "test-login",
//                 Password = "StrongPass123!",
//                 ConfirmPassword = "StrongPass123!"
//             };
//
//             var serviceResult = new RegisterUserResponse
//             {
//                 UserId = 42,
//                 DisplayName = "Test User",
//                 Email = "test@example.com",
//                 HasDashboardAccess = true
//             };
//
//             userRegistrationServiceMock
//                 .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(serviceResult);
//
//             var ct = CancellationToken.None;
//
//             // Act
//             var result = await controller.Register(request, ct);
//
//             // Assert
//             var ok = Assert.IsType<OkObjectResult>(result.Result);
//             var response = Assert.IsType<ApiResponse<RegisterUserResponse>>(ok.Value);
//
//             Assert.True(response.Success);
//             Assert.Equal("Success", response.Message);
//             Assert.NotNull(response.Data);
//             Assert.Equal(serviceResult.UserId, response.Data.UserId);
//             Assert.Equal(serviceResult.DisplayName, response.Data.DisplayName);
//             Assert.Equal(serviceResult.Email, response.Data.Email);
//             Assert.Equal(serviceResult.HasDashboardAccess, response.Data.HasDashboardAccess);
//
//             userRegistrationServiceMock.Verify(
//                 s => s.RegisterAsync(request, ct),
//                 Times.Once);
//         }
//
//         // ---------------------------
//         // GoogleLogin
//         // ---------------------------
//
//         [Fact]
//         public async Task GoogleLogin_WhenValidRequest_ReturnsOkWithSuccessResponse()
//         {
//             // Arrange
//             var request = new GoogleLoginRequest
//             {
//                 IdToken = "google-id-token"
//             };
//
//             var serviceResult = new GoogleLoginResponse
//             {
//                 Token = "jwt-token",
//                 Expiration = DateTimeOffset.UtcNow.AddHours(1),
//                 UserId = 7,
//                 DisplayName = "Google User",
//                 Email = "google@example.com",
//                 IsNewUser = true
//             };
//
//             googleAuthServiceMock
//                 .Setup(s => s.LoginWithGoogleAsync(request.IdToken, It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(serviceResult);
//
//             var ct = CancellationToken.None;
//
//             // Act
//             var result = await controller.GoogleLogin(request, ct);
//
//             // Assert
//             var ok = Assert.IsType<OkObjectResult>(result.Result);
//             var response = Assert.IsType<ApiResponse<GoogleLoginResponse>>(ok.Value);
//
//             Assert.True(response.Success);
//             Assert.Equal("Success", response.Message);
//             Assert.NotNull(response.Data);
//             Assert.Equal(serviceResult.Token, response.Data.Token);
//             Assert.Equal(serviceResult.UserId, response.Data.UserId);
//             Assert.Equal(serviceResult.DisplayName, response.Data.DisplayName);
//             Assert.Equal(serviceResult.Email, response.Data.Email);
//             Assert.Equal(serviceResult.IsNewUser, response.Data.IsNewUser);
//
//             googleAuthServiceMock.Verify(
//                 s => s.LoginWithGoogleAsync(request.IdToken, ct),
//                 Times.Once);
//         }
//     }
// }
