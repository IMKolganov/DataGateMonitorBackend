using Mapster;
using Microsoft.AspNetCore.Mvc;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.Mapping.QuotaPlans.Mappings;
using DataGateMonitor.Models;
using DataGateMonitor.Services.QuotaPlans;
using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlans.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlans.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.Controllers
{
    public class QuotaPlanControllerTests
    {
        private readonly Mock<IQuotaPlanService> _service = new();
        private readonly QuotaPlanController _controller;

        public QuotaPlanControllerTests()
        {
            TypeAdapterConfig.GlobalSettings.Scan(typeof(QuotaPlanMapping).Assembly);
            _controller = new QuotaPlanController(_service.Object);
        }

        [Fact]
        public async Task GetAll_ExcludeInactive_FiltersOutInactive()
        {
            // Arrange
            var plans = new List<QuotaPlan>
            {
                new() { Id = 1, Name = "Active", IsActive = true },
                new() { Id = 2, Name = "Inactive", IsActive = false }
            };

            _service
                .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(plans);

            var request = new GetQuotaPlansRequest { IncludeInactive = false };

            // Act
            var result = await _controller.GetAll(request, CancellationToken.None);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<QuotaPlansResponse>>(ok.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            var data = response.Data;
            Assert.NotNull(data.QuotaPlans);
            Assert.Single(data.QuotaPlans); // only active
            Assert.Equal(1, data.QuotaPlans[0].Id);
            Assert.Equal("Active", data.QuotaPlans[0].Name);

            _service.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAll_IncludeInactive_ReturnsAll()
        {
            // Arrange
            var plans = new List<QuotaPlan>
            {
                new() { Id = 1, Name = "Active", IsActive = true },
                new() { Id = 2, Name = "Inactive", IsActive = false }
            };

            _service
                .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(plans);

            var request = new GetQuotaPlansRequest { IncludeInactive = true };

            // Act
            var result = await _controller.GetAll(request, CancellationToken.None);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<QuotaPlansResponse>>(ok.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            var data = response.Data;
            Assert.NotNull(data.QuotaPlans);
            Assert.Equal(2, data.QuotaPlans.Count);
            Assert.Equal(1, data.QuotaPlans[0].Id);
            Assert.Equal("Active", data.QuotaPlans[0].Name);
            Assert.Equal(2, data.QuotaPlans[1].Id);
            Assert.Equal("Inactive", data.QuotaPlans[1].Name);

            _service.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetById_WhenFound_ReturnsOk()
        {
            // Arrange
            var plan = new QuotaPlan
            {
                Id = 10,
                Name = "Test plan",
                IsActive = true
            };

            _service
                .Setup(s => s.GetByIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(plan);

            // Act
            var result = await _controller.GetById(10, CancellationToken.None);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<QuotaPlanResponse>>(ok.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            var data = response.Data;
            Assert.NotNull(data.QuotaPlan);
            Assert.Equal(10, data.QuotaPlan.Id);
            Assert.Equal("Test plan", data.QuotaPlan.Name);
            Assert.True(data.QuotaPlan.IsActive);

            _service.Verify(s => s.GetByIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetById_WhenNotFound_ReturnsNotFound()
        {
            // Arrange
            _service
                .Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((QuotaPlan?)null);

            // Act
            var result = await _controller.GetById(99, CancellationToken.None);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<QuotaPlanResponse>>(notFound.Value);

            Assert.False(response.Success);
            _service.Verify(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Create_Returns_Ok_WithCreatedPlan()
        {
            var request = new CreateOrUpdateQuotaPlanRequest
            {
                Name = "New plan",
                IsActive = true,
                IsDefault = false
            };

            var created = new QuotaPlan
            {
                Id = 5,
                Name = "New plan",
                IsActive = true
            };

            _service
                .Setup(s => s.CreateAsync(
                    It.IsAny<QuotaPlan>(),
                    request.IsDefault,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(created);

            var result = await _controller.Create(request, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<QuotaPlanResponse>>(ok.Value);

            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal(created.Id,   response.Data.QuotaPlan.Id);
            Assert.Equal(created.Name, response.Data.QuotaPlan.Name);

            _service.Verify(
                s => s.CreateAsync(It.IsAny<QuotaPlan>(), request.IsDefault, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Update_Returns_Ok_AndCallsService()
        {
            // Arrange
            var request = new CreateOrUpdateQuotaPlanRequest
            {
                Id = 7,
                Name = "Updated",
                IsActive = true,
                IsDefault = false
            };

            _service
                .Setup(s => s.UpdateAsync(It.IsAny<QuotaPlan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _controller.Update(request, CancellationToken.None);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<string>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal("Updated successfully", response.Data);

            _service.Verify(
                s => s.UpdateAsync(It.IsAny<QuotaPlan>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Delete_Returns_Ok_AndCallsService()
        {
            // Arrange
            _service
                .Setup(s => s.DeleteAsync(3, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _controller.Delete(3, CancellationToken.None);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<string>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal("Deleted successfully", response.Data);

            _service.Verify(s => s.DeleteAsync(3, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SetDefault_Returns_Ok_AndCallsService()
        {
            // Arrange
            _service
                .Setup(s => s.SetDefaultAsync(4, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SetDefault(4, CancellationToken.None);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<string>>(ok.Value);

            Assert.True(response.Success);
            Assert.Equal("Default plan updated", response.Data);

            _service.Verify(s => s.SetDefaultAsync(4, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>Ensures List&lt;QuotaPlan&gt; is mapped into QuotaPlansResponse.QuotaPlans (catches missing Mapster config).</summary>
        [Fact]
        public void Map_ListQuotaPlan_To_QuotaPlansResponse_FillsQuotaPlans()
        {
            TypeAdapterConfig.GlobalSettings.Scan(typeof(QuotaPlanMapping).Assembly);

            var plans = new List<QuotaPlan>
            {
                new() { Id = 1, Name = "Plan A", IsActive = true },
                new() { Id = 2, Name = "Plan B", IsActive = false }
            };

            var response = plans.Adapt<QuotaPlansResponse>();

            Assert.NotNull(response);
            Assert.NotNull(response.QuotaPlans);
            Assert.Equal(2, response.QuotaPlans.Count);
            Assert.Equal(1, response.QuotaPlans[0].Id);
            Assert.Equal("Plan A", response.QuotaPlans[0].Name);
            Assert.Equal(2, response.QuotaPlans[1].Id);
            Assert.Equal("Plan B", response.QuotaPlans[1].Name);
        }

        /// <summary>Ensures QuotaPlan is mapped into QuotaPlanResponse.QuotaPlan (catches missing Mapster config).</summary>
        [Fact]
        public void Map_QuotaPlan_To_QuotaPlanResponse_FillsQuotaPlan()
        {
            TypeAdapterConfig.GlobalSettings.Scan(typeof(QuotaPlanMapping).Assembly);

            var plan = new QuotaPlan { Id = 42, Name = "Single", Description = "Desc", IsActive = true, IsDefault = true };

            var response = plan.Adapt<QuotaPlanResponse>();

            Assert.NotNull(response);
            Assert.NotNull(response.QuotaPlan);
            Assert.Equal(42, response.QuotaPlan.Id);
            Assert.Equal("Single", response.QuotaPlan.Name);
            Assert.Equal("Desc", response.QuotaPlan.Description);
            Assert.True(response.QuotaPlan.IsActive);
            Assert.True(response.QuotaPlan.IsDefault);
        }
    }
}
