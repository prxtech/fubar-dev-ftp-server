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

## Open items

Tracked in the hardening plan (`data/docs/`): path traversal on Windows, TLS protocol pinning and
`RequireTls`, brute-force throttling, connection limits, `REST` offset validation, account root
sanitisation.

## Verify

```bash
dotnet test --filter-namespace "FubarDev.FtpServer.Tests.Security"
```
