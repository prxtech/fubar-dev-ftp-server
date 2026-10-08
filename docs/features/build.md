# Build

## Purpose

Build the fork on .NET 10 with one shared configuration, reproducible package versions and no
dependency on private feeds.

## Configuration

| File | Role |
|---|---|
| `global.json` | SDK 10.0.x (`latestFeature`), test runner `Microsoft.Testing.Platform` |
| `Directory.Build.props` | `net10.0`, `Nullable=enable`, `LangVersion=latest`, .NET analyzers, StyleCop, `Alert.<ProjectName>` PackageId for `src/` projects, version `4.0.0-alert` |
| `Directory.Packages.props` | All NuGet versions (central package management) |
| `nuget.config` | nuget.org only |

`FubarDev.FtpServer.Abstractions` uses `<FrameworkReference Include="Microsoft.AspNetCore.App" />` for
`IFeatureCollection` (replaces the frozen `Microsoft.AspNetCore.Http.Features` 5.x package).

## Project set

Kept: Abstractions, Commands, core server, FileSystem.InMemory, FileSystem.DotNet, FileSystem.S3
(example, AWSSDK v4), `samples/QuickStart.GenericHost`, tests.

Removed from upstream: GoogleDrive, Unix and PAM providers, legacy samples, Mono build props,
GnuSslStream/ReadLine shared projects, Azure pipeline.

## Rules

- No inline `Version=` in `.csproj`; add a `<PackageVersion>` to `Directory.Packages.props`.
- Required connection features are read with `Features.GetRequiredFeature<T>()`; optional ones with `Get<T>()` plus a null check.

## Verify

```bash
dotnet build FubarDev.FtpServer.sln
dotnet test
dotnet list package --vulnerable --include-transitive
```
