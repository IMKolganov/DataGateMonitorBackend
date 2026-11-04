using DataGateMonitor.Controllers;
using DataGateMonitor.SharedModels.DataGateMonitor.LocalIssuance.Requests;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DataGateMonitor.Configurations;

/// <summary>The signed endpoint reads raw bytes before deserialization; describe its shared body without changing that order.</summary>
public sealed class LocalIssuanceEventOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(VpnNodeLocalIssuanceController)
            || context.MethodInfo.Name != nameof(VpnNodeLocalIssuanceController.PostLocalIssuanceEvent))
            return;

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new()
                {
                    Schema = context.SchemaGenerator.GenerateSchema(typeof(LocalIssuanceEventRequest), context.SchemaRepository)
                }
            }
        };
    }
}
