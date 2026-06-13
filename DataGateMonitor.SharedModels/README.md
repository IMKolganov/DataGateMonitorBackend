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

1. Bump `<Version>` in `DataGateMonitor.SharedModels.csproj`.
2. `dotnet build -c Release` then `dotnet pack -c Release` and publish to nuget.org.
3. Bump `PackageReference` `Version="…"` in all consuming projects to the same value.

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
