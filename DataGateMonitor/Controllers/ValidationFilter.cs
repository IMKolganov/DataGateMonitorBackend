using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            context.Result = new BadRequestObjectResult(ApiResponse<List<string>>.ErrorResponse(string.Join("; ", errors)));
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}