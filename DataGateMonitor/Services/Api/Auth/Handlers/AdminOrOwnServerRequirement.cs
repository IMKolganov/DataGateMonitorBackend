using Microsoft.AspNetCore.Authorization;

namespace DataGateMonitor.Services.Api.Auth.Handlers;

public sealed class AdminOrOwnServerRequirement : IAuthorizationRequirement;