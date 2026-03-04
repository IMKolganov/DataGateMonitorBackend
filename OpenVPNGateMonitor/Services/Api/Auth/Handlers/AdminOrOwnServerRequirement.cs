using Microsoft.AspNetCore.Authorization;

namespace OpenVPNGateMonitor.Services.Api.Auth.Handlers;

public sealed class AdminOrOwnServerRequirement : IAuthorizationRequirement;