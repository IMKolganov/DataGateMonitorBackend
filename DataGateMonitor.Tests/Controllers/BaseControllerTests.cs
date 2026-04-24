using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Controllers;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.Controllers
{
    public class BaseControllerTests
    {
        private readonly BaseController controller;

        public BaseControllerTests()
        {
            controller = new BaseController();
        }

        // -------------------------
        // Healthcheck
        // -------------------------

        [Fact]
        public void Healthcheck_ReturnsOk_WithSuccessResponse()
        {
            // Act
            var result = controller.Healthcheck();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<string>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal("Success", response.Message);
            Assert.Equal("Ok", response.Data);
        }

        // -------------------------
        // HealthcheckWithJwt
        // -------------------------

        [Fact]
        public void HealthcheckWithJwt_ReturnsOk_WithSuccessResponse()
        {
            // Act
            var result = controller.HealthcheckWithJwt();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<string>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal("Success", response.Message);
            Assert.Equal("Healthy", response.Data);
        }
    }
}