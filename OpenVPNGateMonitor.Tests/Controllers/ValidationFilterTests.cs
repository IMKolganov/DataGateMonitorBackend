using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Tests.Controllers
{
    public class ValidationFilterTests
    {
        [Fact]
        public void OnActionExecuting_WhenModelStateInvalid_SetsBadRequestResult()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Field1", "Error 1");
            modelState.AddModelError("Field2", "Error 2");

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor(),
                modelState);

            var filters = new List<IFilterMetadata>();
            var actionArguments = new Dictionary<string, object?>();

            var executingContext = new ActionExecutingContext(
                actionContext,
                filters,
                actionArguments,
                controller: new object());

            var filter = new ValidationFilter();

            // Act
            filter.OnActionExecuting(executingContext);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(executingContext.Result);
            var response = Assert.IsType<ApiResponse<List<string>>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.NotNull(response.Message);
            Assert.Contains("Error 1", response.Message);
            Assert.Contains("Error 2", response.Message);
        }

        [Fact]
        public void OnActionExecuting_WhenModelStateValid_DoesNotSetResult()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary(); // valid (no errors)

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor(),
                modelState);

            var filters = new List<IFilterMetadata>();
            var actionArguments = new Dictionary<string, object?>();

            var executingContext = new ActionExecutingContext(
                actionContext,
                filters,
                actionArguments,
                controller: new object());

            var filter = new ValidationFilter();

            // Act
            filter.OnActionExecuting(executingContext);

            // Assert
            Assert.Null(executingContext.Result);
        }
    }
}
