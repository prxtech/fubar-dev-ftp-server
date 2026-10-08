# Security hardening

## Purpose

Fix security gaps found in the upstream code base (scan of 2026-10-07). Each fix ships with a test
in `test/FubarDev.FtpServer.Tests/Security/`. Safe behaviour is the default; opting out is explicit.

## FTP bounce protection (RFC 2577)

`PORT` / `EPRT` only accept the client address of the control connection and ports ≥ 1024.
Anything else returns `504`; malformed arguments return `501`. IPv4-mapped IPv6 addresses are
normalised, also in the PASV peer check.

| Option (`PortCommandOptions`) | Default | Effect |
|---|---|---|
| `AllowForeignAddress` | `false` | Allow targets other than the client address (FXP; enables bounce attacks) |
| `AllowPrivilegedPort` | `false` | Allow target ports below 1024 |

Tests: `Security/FtpBounceTests.cs`.

## Command line length limit

The command parser buffers input until a line terminator. Lines longer than
`FtpConnectionOptions.MaxCommandLineLength` (default `4096` bytes, excluding the terminator) throw
`FtpCommandTooLongException`; the connection is closed and a warning with the client IP is logged.
Applies before authentication.

Tests: `Security/CommandLineLengthTests.cs`.

## File system root confinement (DotNet provider)

Every name passed to `DotNetFileSystem` (lookup, create, mkdir, rename target) must be a single
plain file name: `.`, `..`, rooted names and characters invalid in file names on the host OS are
rejected with `553` (`FileNameNotAllowedException`). On Windows this covers `\`, `:` (drive and
alternate-stream syntax) and `/`. The resolved full path must also stay inside the root
(`SafePath`, `src/FubarDev.FtpServer.FileSystem.DotNet/SafePath.cs`).

`DotNetFileSystemOptions.RootPath` is **required** (no silent fallback to the temp directory).
Per-account roots from `IAccountDirectoryQuery` (user names, anonymous e-mail addresses) are
resolved segment by segment: every segment must be a plain file name (no `.`, `..`, separators
inside a segment) and at least one segment is required. An account therefore always gets its own
directory strictly below `RootPath`: never the shared root itself (`.`, `a/..`) and never another
account's directory (`camera-1/../camera-2`). Otherwise login to the file system fails with `553`.

Not covered: symbolic links already present below the root are followed.

Tests: `Security/DotNetFileSystemPathTests.cs`.

## Open items

Tracked in the hardening plan (`data/docs/`): TLS protocol pinning and `RequireTls`, brute-force
throttling, connection limits, `REST` offset validation, symlink handling.

## Verify

```bash
dotnet test --filter-namespace "FubarDev.FtpServer.Tests.Security"
```
