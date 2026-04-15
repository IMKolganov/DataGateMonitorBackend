using System.Reflection;
using Microsoft.AspNetCore.Builder;
using DataGateMonitor.Configurations;
using Xunit;

namespace DataGateMonitor.Tests.Configurations;

/// <summary>
/// ConfigurePipeline applies migrations (relational DB), MapHub, MapHealthChecks, etc.
/// Full run requires full app host; here we only ensure the extension exists and has the expected method.
/// </summary>
public class PipelineConfigurationTests
{
    [Fact]
    public void PipelineConfiguration_ConfigurePipeline_ExtensionMethodExists()
    {
        var method = typeof(PipelineConfiguration).GetMethod(nameof(PipelineConfiguration.ConfigurePipeline),
            BindingFlags.Public | BindingFlags.Static, new[] { typeof(Microsoft.AspNetCore.Builder.WebApplication) });
        Assert.NotNull(method);
    }
}
