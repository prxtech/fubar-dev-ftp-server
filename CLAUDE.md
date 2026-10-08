# CLAUDE.md

Guidance for Claude Code working in this repo. See `README.md` for upstream library usage.

## Project

Fork of [FubarDevelopment/FtpServer](https://github.com/FubarDevelopment/FtpServer) (MIT, forked at
`8bfbe96`, see `NOTICE.md`), ported to .NET 10 and hardened. Used as the in-process **FTP/FTPS ingest**
for the ANPR project (`../alert-anpr-poc`): cameras upload images that are processed **in memory**,
without temp files on disk. SFTP is out of scope (handled by SFTPGo/Rebex in the ANPR design).

- `src/FubarDev.FtpServer.Abstractions/` — interfaces, features, file system abstraction (`IUnixFileSystem`, `IFileSystemClassFactory`).
- `src/FubarDev.FtpServer.Commands/` — FTP command handlers (`[FtpCommandHandler("XXX")]`).
- `src/FubarDev.FtpServer/` — TCP server, connection pipeline, TLS, DI registration (`ServiceCollectionExtensions`).
- `src/FubarDev.FtpServer.FileSystem.{InMemory,DotNet,S3}/` — file system providers. S3 is kept as an **example** only.
- `samples/QuickStart.GenericHost/` — the only remaining sample.
- `test/FubarDev.FtpServer.Tests/` — xunit v3; protocol-level tests use `RawFtpClient`, security tests live in `Security/`.
- `docs/` root (`api/`, `articles/`, `index.html`, …) is the **upstream docfx site** — do not edit; `docfx_project/` is its source.

## Documentation workflow (required)

Two locations, different lifecycles:

- **Plans & specs → `data/docs/`** — `data/` is **gitignored** (local-only working docs). Put design drafts, specs, and implementation plans here. Name dated: `YYYY-MM-DD-<topic>.md`.
- **Feature docs → `docs/features/`** — **committed**, one markdown per feature. When you add or materially change a feature, write/update its `docs/features/<feature>.md` in the same change.

**Never write a spec or plan anywhere under `docs/`.** This overrides any skill's default location
(e.g. superpowers writes to `docs/superpowers/specs/` — wrong here).

A feature doc covers: purpose, public API / options, key behaviour and security rules, and how to verify.
"Feature" includes infrastructure (build, CI, packaging). Index: `docs/features/README.md`.

## Design principles

- **DRY** — before writing a helper, search for an existing one (e.g. `GetRequiredFeature<T>()`, path helpers in `Abstractions/FileSystem/`). Shared logic gets one home; no copy-paste between file system providers or command handlers.
- **SOLID**
  - *Single responsibility*: no new god classes; `FtpConnection.cs` is already too big — extract, don't append.
  - *Open/closed*: extend through the existing extension points (`IUnixFileSystem`, `IFtpMiddleware`, `IFtpDataConnectionValidator`, options classes) rather than editing core flow.
  - *Dependency inversion*: constructor injection. Do not add new `IServiceProvider.GetService` (service locator) calls.
- **YAGNI** — only what the ANPR ingest needs; don't revive removed providers (GoogleDrive, Unix, PAM).
- **Security by default** — new options default to the safe value; opting out must be explicit and documented.
- **TDD for fixes** — write the failing test first (`test/.../Security/` for security fixes), then the fix.
- Match surrounding code: file header copyright comment, XML docs on public members, StyleCop rules, async all the way (no `.Result` / `.Wait()`).

## Commit guidelines

- **Conventional Commits**, caveman style: terse, no filler. Subject ≤50 chars, imperative. Body only when the *why* isn't obvious from the subject.
- Format: `type(scope): summary` — e.g. `fix(server): limit command line length`. Scopes: `abstractions`, `commands`, `server`, `fs-inmemory`, `fs-dotnet`, `fs-s3`, `tests`, `build`, `docs`.
- **NEVER** add `Co-Authored-By:` or any AI/Claude attribution footer. Not ever.
- Propose the message and **ask before committing**; never push without explicit OK.
- Remote: `git@github-personal:prxtech/fubar-dev-ftp-server.git`, default branch `main`. Work on feature branches (e.g. `net10-port`).

## Build & test

- **Central Package Management**: all NuGet versions live in `Directory.Packages.props`. Do **not** add inline `Version=` to `.csproj` — add a `<PackageVersion>` to the props file and a bare `<PackageReference>` to the project. Shared MSBuild settings (net10.0, nullable, analyzers, `Alert.*` PackageId) are in `Directory.Build.props`.
- `nuget.config` uses nuget.org only. `global.json` pins SDK 10.0.x and opts into Microsoft.Testing.Platform.

```bash
dotnet build FubarDev.FtpServer.sln
dotnet test                                     # MTP runner; Windows-only path tests are skipped on Linux
dotnet list package --vulnerable --include-transitive
```
