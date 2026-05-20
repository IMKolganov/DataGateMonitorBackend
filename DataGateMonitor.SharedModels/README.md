# DataGateMonitor.SharedModels

Shared models for the DataGateMonitor project.  
These models are used for API communication between backend services and consumers.

## Features

- DTOs for OpenVPN Gate Monitor API
- Simple and lightweight
- Compatible with .NET 6 7 8 9

## Versioning

Check the latest version on [nuget.org](https://www.nuget.org/packages/DataGateMonitor.SharedModels).

After changing DTOs in this project:

1. Bump `<Version>` in `DataGateMonitor.SharedModels.csproj` (next patch after nuget.org, e.g. `1.0.17` → `1.0.18`).
2. `dotnet pack` and publish to nuget.org.
3. Bump `PackageReference` `Version="…"` in all consuming projects (backend, tests, telegrambot, etc.).

**1.0.18** (on nuget.org) adds admin TOTP fields on `LoginResponse` / `GoogleLoginResponse` and TOTP auth request/response types.

**1.0.19** (publish to nuget.org before bumping consumers): `AuthSessionPolicyResponse`, admin session list/revoke DTOs (`UserSessionDto`, `GetUserSessionsResponse`, `RevokeUserSessionsRequest`).

Do **not** use a local NuGet feed or `ProjectReference` to SharedModels — only published versions from nuget.org in `PackageReference`.

## Usage

Install via NuGet:

```bash
dotnet add package DataGateMonitor.SharedModels
```

Import in your code:

```csharp
using DataGateMonitor.SharedModels;
```

## License

MIT