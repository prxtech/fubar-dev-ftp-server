// <copyright file="DotNetFileSystemPathTests.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.FtpServer.AccountManagement;
using FubarDev.FtpServer.AccountManagement.Directories;
using FubarDev.FtpServer.FileSystem;
using FubarDev.FtpServer.FileSystem.DotNet;
using FubarDev.FtpServer.FileSystem.Error;

using Microsoft.Extensions.Options;

using Xunit;

namespace FubarDev.FtpServer.Tests.Security
{
    /// <summary>
    /// The .NET file system must never touch files outside its root directory.
    /// </summary>
    public sealed class DotNetFileSystemPathTests : IDisposable
    {
        private readonly string _sandbox;
        private readonly string _root;
        private readonly DotNetFileSystem _fileSystem;

        public DotNetFileSystemPathTests()
        {
            _sandbox = Path.Combine(Path.GetTempPath(), "ftp-path-tests-" + Guid.NewGuid().ToString("N"));
            _root = Path.Combine(_sandbox, "root");
            Directory.CreateDirectory(_root);
            _fileSystem = new DotNetFileSystem(_root, false);
        }

        public static TheoryData<string> EscapingNames() => new()
        {
            "..",
            ".",
            "../escape.txt",
            "/escape.txt",
            "sub/escape.txt",
            "with\0nul.txt",
            string.Empty,
        };

        [Theory]
        [MemberData(nameof(EscapingNames))]
        public async Task CreateRejectsNamesOutsideDirectory(string name)
        {
            using var data = new MemoryStream(Encoding.ASCII.GetBytes("x"));

            await Assert.ThrowsAsync<FileNameNotAllowedException>(
                () => _fileSystem.CreateAsync(_fileSystem.Root, name, data, CancellationToken.None));

            AssertNothingOutsideRoot();
        }

        [Theory]
        [MemberData(nameof(EscapingNames))]
        public async Task GetEntryByNameRejectsNamesOutsideDirectory(string name)
        {
            File.WriteAllText(Path.Combine(_sandbox, "secret.txt"), "secret");

            await Assert.ThrowsAsync<FileNameNotAllowedException>(
                () => _fileSystem.GetEntryByNameAsync(_fileSystem.Root, name, CancellationToken.None));
        }

        [Theory]
        [MemberData(nameof(EscapingNames))]
        public async Task CreateDirectoryRejectsNamesOutsideDirectory(string name)
        {
            await Assert.ThrowsAsync<FileNameNotAllowedException>(
                () => _fileSystem.CreateDirectoryAsync(_fileSystem.Root, name, CancellationToken.None));

            AssertNothingOutsideRoot();
        }

        [Theory]
        [MemberData(nameof(EscapingNames))]
        public async Task MoveRejectsNamesOutsideDirectory(string name)
        {
            using var data = new MemoryStream(Encoding.ASCII.GetBytes("x"));
            await _fileSystem.CreateAsync(_fileSystem.Root, "source.txt", data, CancellationToken.None);
            var source = await _fileSystem.GetEntryByNameAsync(_fileSystem.Root, "source.txt", CancellationToken.None);

            await Assert.ThrowsAsync<FileNameNotAllowedException>(
                () => _fileSystem.MoveAsync(_fileSystem.Root, source!, _fileSystem.Root, name, CancellationToken.None));

            Assert.True(File.Exists(Path.Combine(_root, "source.txt")));
            AssertNothingOutsideRoot();
        }

        [Theory]
        [InlineData(@"..\escape.txt")]
        [InlineData(@"C:\escape.txt")]
        [InlineData(@"C:escape.txt")]
        public async Task WindowsPathSyntaxNeverEscapesRoot(string name)
        {
            // Windows: backslash and colon are path syntax and must be rejected.
            // Unix: they are ordinary file name characters, so the file stays inside the root.
            using var data = new MemoryStream(Encoding.ASCII.GetBytes("x"));
            try
            {
                await _fileSystem.CreateAsync(_fileSystem.Root, name, data, CancellationToken.None);
                Assert.False(OperatingSystem.IsWindows(), "Windows must reject path syntax in names");
                Assert.True(File.Exists(Path.Combine(_root, name)));
            }
            catch (FileNameNotAllowedException)
            {
                Assert.True(OperatingSystem.IsWindows(), "Unix must accept the literal name");
            }

            AssertNothingOutsideRoot();
        }

        [Fact]
        public async Task NormalNamesStillWork()
        {
            using var data = new MemoryStream(Encoding.ASCII.GetBytes("x"));
            await _fileSystem.CreateAsync(_fileSystem.Root, "plate_20261008.jpg", data, CancellationToken.None);
            var dir = await _fileSystem.CreateDirectoryAsync(_fileSystem.Root, "camera-1", CancellationToken.None);

            Assert.NotNull(await _fileSystem.GetEntryByNameAsync(_fileSystem.Root, "plate_20261008.jpg", CancellationToken.None));
            Assert.NotNull(await _fileSystem.GetEntryByNameAsync(_fileSystem.Root, "camera-1", CancellationToken.None));
            Assert.Null(await _fileSystem.GetEntryByNameAsync(dir, "missing.jpg", CancellationToken.None));
        }

        [Fact]
        public void ProviderRequiresExplicitRootPath()
        {
            Assert.Throws<OptionsValidationException>(
                () => CreateProvider(rootPath: null, accountRoot: null));
        }

        [Theory]
        [InlineData("..")]
        [InlineData("../other")]
        [InlineData("a/../../other")]
        [InlineData(".")]
        [InlineData("/")]
        [InlineData("a/..")]
        [InlineData("camera-1/../camera-2")]
        [InlineData("users/./camera-1")]
        public async Task ProviderRejectsAccountRootOutsideOwnDirectory(string accountRoot)
        {
            var provider = CreateProvider(_root, accountRoot);

            await Assert.ThrowsAsync<FileNameNotAllowedException>(
                () => provider.Create(new TestAccountInformation("camera-1")));
        }

        [Fact]
        public async Task ProviderAcceptsAccountRootInsideRootPath()
        {
            var provider = CreateProvider(_root, "camera-1");

            var fileSystem = await provider.Create(new TestAccountInformation("camera-1"));

            Assert.True(Directory.Exists(Path.Combine(_root, "camera-1")));
            Assert.NotNull(fileSystem.Root);
        }

        [Fact]
        public async Task ProviderAcceptsNestedAccountRoot()
        {
            var provider = CreateProvider(_root, "users/camera-1");

            await provider.Create(new TestAccountInformation("camera-1"));

            Assert.True(Directory.Exists(Path.Combine(_root, "users", "camera-1")));
        }

        public void Dispose()
        {
            Directory.Delete(_sandbox, true);
        }

        private static DotNetFileSystemProvider CreateProvider(string? rootPath, string? accountRoot)
            => new(
                Options.Create(new DotNetFileSystemOptions { RootPath = rootPath }),
                new FixedAccountDirectoryQuery(accountRoot));

        private void AssertNothingOutsideRoot()
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(_sandbox))
            {
                var name = Path.GetFileName(entry);
                Assert.True(name is "root" or "secret.txt", $"Unexpected entry outside root: {entry}");
            }
        }

        private sealed class FixedAccountDirectoryQuery : IAccountDirectoryQuery
        {
            private readonly string? _rootPath;

            public FixedAccountDirectoryQuery(string? rootPath) => _rootPath = rootPath;

            // Bypasses GenericAccountDirectories' RemoveRoot on purpose: custom queries may return anything.
            public IAccountDirectories GetDirectories(IAccountInformation accountInformation)
                => new RawAccountDirectories(_rootPath);
        }

        private sealed class RawAccountDirectories : IAccountDirectories
        {
            public RawAccountDirectories(string? rootPath) => RootPath = rootPath;

            public string? RootPath { get; }

            public string? HomePath => null;
        }

        private sealed class TestAccountInformation : IAccountInformation
        {
            public TestAccountInformation(string name)
            {
                FtpUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, name) }, "test"));
            }

#pragma warning disable CS0618 // obsolete member still required by the interface
            public IFtpUser User => throw new NotSupportedException();
#pragma warning restore CS0618

            public ClaimsPrincipal FtpUser { get; }

            public IMembershipProvider MembershipProvider => throw new NotSupportedException();
        }
    }
}
