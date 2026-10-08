// <copyright file="CommandLineLengthTests.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace FubarDev.FtpServer.Tests.Security
{
    /// <summary>
    /// An unauthenticated client must not be able to grow the command buffer without bound.
    /// </summary>
    public class CommandLineLengthTests : FtpServerTestsBase
    {
        public CommandLineLengthTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CollectorRejectsUnterminatedLineOverLimit()
        {
            var collector = new FtpCommandCollector(() => Encoding.ASCII, maxLineLength: 16);

            Assert.Throws<FtpCommandTooLongException>(
                () => collector.Collect(Encoding.ASCII.GetBytes(new string('A', 17))).ToList());
        }

        [Fact]
        public void CollectorCountsBytesAcrossChunks()
        {
            var collector = new FtpCommandCollector(() => Encoding.ASCII, maxLineLength: 16);
            collector.Collect(Encoding.ASCII.GetBytes(new string('A', 10))).ToList();

            Assert.Throws<FtpCommandTooLongException>(
                () => collector.Collect(Encoding.ASCII.GetBytes(new string('A', 7))).ToList());
        }

        [Fact]
        public void CollectorAcceptsLineAtLimit()
        {
            var collector = new FtpCommandCollector(() => Encoding.ASCII, maxLineLength: 16);

            var commands = collector.Collect(Encoding.ASCII.GetBytes("NOOP " + new string('A', 11) + "\r\n")).ToList();

            Assert.Single(commands);
            Assert.True(collector.IsEmpty);
        }

        [Fact]
        public void CollectorLimitAppliesPerLine()
        {
            var collector = new FtpCommandCollector(() => Encoding.ASCII, maxLineLength: 16);
            var input = string.Concat(Enumerable.Repeat("NOOP AAAAAAAAAAA\r\n", 10));

            var commands = collector.Collect(Encoding.ASCII.GetBytes(input)).ToList();

            Assert.Equal(10, commands.Count);
        }

        [Fact]
        public void DefaultLimitIs4096Bytes()
        {
            Assert.Equal(4096, new FtpConnectionOptions().MaxCommandLineLength);
        }

        [Fact]
        public async Task ServerClosesConnectionWhenLineExceedsLimit()
        {
            await using var client = await RawFtpClient.ConnectAsync(Server.Port);

            // No login needed: this must be enforced before authentication.
            await client.SendRawAsync(Encoding.ASCII.GetBytes(new string('A', 64 * 1024)));

            var response = await client.ReadResponseAsync();
            Assert.True(response == null || response.StartsWith("500", StringComparison.Ordinal), $"Unexpected response: {response}");
            Assert.Null(await client.ReadResponseAsync());
        }
    }
}
