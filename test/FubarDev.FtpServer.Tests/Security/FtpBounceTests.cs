// <copyright file="FtpBounceTests.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading.Tasks;

using Xunit;

namespace FubarDev.FtpServer.Tests.Security
{
    /// <summary>
    /// PORT/EPRT must not be usable to make the server connect to third-party hosts (RFC 2577 "bounce attack").
    /// </summary>
    public class FtpBounceTests : FtpServerTestsBase
    {
        public FtpBounceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory]
        [InlineData("PORT 10,0,0,1,4,1")] // foreign host, port 1025
        [InlineData("EPRT |1|10.0.0.1|1025|")] // foreign host
        [InlineData("PORT 127,0,0,1,0,25")] // client host, privileged port 25 (SMTP)
        [InlineData("EPRT |1|127.0.0.1|25|")] // client host, privileged port
        [InlineData("EPRT |2|::1|1025|")] // IPv6 loopback is not the IPv4 client address
        public async Task ActiveModeToForeignOrPrivilegedTargetIsRejected(string command)
        {
            await using var client = await RawFtpClient.ConnectAsync(Server.Port);
            await client.LoginAnonymousAsync();

            var response = await client.SendAsync(command);

            Assert.NotNull(response);
            Assert.StartsWith("504", response);
        }

        [Theory]
        [InlineData("PORT 127,0,0,1,4,1")]
        [InlineData("EPRT |1|127.0.0.1|1025|")]
        [InlineData("EPRT |||1025|")] // no address: client address is used
        public async Task ActiveModeToClientAddressIsAccepted(string command)
        {
            await using var client = await RawFtpClient.ConnectAsync(Server.Port);
            await client.LoginAnonymousAsync();

            var response = await client.SendAsync(command);

            Assert.NotNull(response);
            Assert.StartsWith("200", response);
        }

        [Theory]
        [InlineData("PORT 127,0,0,1,4")] // too few parts
        [InlineData("PORT 127,0,0,1,x,1")] // not a number
        [InlineData("PORT 127,0,0,1,256,1")] // port byte out of range
        [InlineData("EPRT |1|127.0.0.1|99999|")] // port out of range
        public async Task MalformedActiveModeArgumentIsSyntaxError(string command)
        {
            await using var client = await RawFtpClient.ConnectAsync(Server.Port);
            await client.LoginAnonymousAsync();

            var response = await client.SendAsync(command);

            Assert.NotNull(response);
            Assert.StartsWith("501", response);
        }
    }
}
