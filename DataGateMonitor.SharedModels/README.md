# DataGateMonitor.SharedModels

Shared DTOs and enums for the **DataGate Monitor** API, published as the NuGet package `DataGateMonitor.SharedModels`.

Used by the backend, tests, and any external clients that need stable API contracts.

## Consumption

Add a package reference — **never** project-reference this repo from `DataGateMonitor` backend or tests:

```xml
<PackageReference Include="DataGateMonitor.SharedModels" Version="…" />
```

After adding types here: bump version in `.csproj`, publish to NuGet, then bump `Version="…"` in all consuming projects.

## License

MIT
