// <copyright file="Issue82ProtocolViolation.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentFTP;

using Xunit;

namespace FubarDev.FtpServer.Tests.Issues
{
    public class Issue82ProtocolViolation : FtpServerTestsBase
    {
        public Issue82ProtocolViolation(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task TestParallelRequests()
        {
            const int maxTasks = 100;
            var tasks = new List<Task>();
            for (var i = 0; i != maxTasks; i++)
            {
                var task = Task.Run(GetFilesAsync);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSerialRequests()
        {
            const int maxTasks = 20;
            for (var i = 0; i != maxTasks; i++)
            {
                await GetFilesAsync();
            }
        }

        private async Task<IList<string>> GetFilesAsync()
        {
            // One fresh connection per request, like the original FtpWebRequest with KeepAlive = false.
            using var client = new AsyncFtpClient("127.0.0.1", "anonymous", "foo@bar.com", Server.Port);
            await client.Connect().ConfigureAwait(false);
            var files = await client.GetNameListing().ConfigureAwait(false);
            await client.Disconnect().ConfigureAwait(false);
            return files.ToList();
        }
    }
}
