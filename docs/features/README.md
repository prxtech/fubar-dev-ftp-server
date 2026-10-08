# Feature docs

One markdown per feature. Add/update a feature's doc in the same change that adds or
materially changes the feature. Each doc covers: purpose, public API / options, key
behaviour and security rules, and how to verify.

Plans and specs live in `data/docs/` (gitignored, local-only). Feature docs here are committed.
The rest of `docs/` is the upstream docfx site and is not maintained in this fork.

## Index

Infrastructure:

- [x] [build.md](build.md) — .NET 10, central package management, trimmed project set, test runner

Security:

- [x] [security-hardening.md](security-hardening.md) — FTP bounce protection, command line length limit
