// <copyright file="RawFtpClient.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FubarDev.FtpServer.Tests
{
    /// <summary>
    /// Minimal line-based FTP control connection for protocol-level tests.
    /// </summary>
    public sealed class RawFtpClient : IAsyncDisposable
    {
        private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(10);

        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly StreamReader _reader;

        private RawFtpClient(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
            _reader = new StreamReader(_stream, Encoding.UTF8);
        }

        public static async Task<RawFtpClient> ConnectAsync(int port)
        {
            var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port);
            var result = new RawFtpClient(client);
            await result.ReadResponseAsync(); // 220 greeting
            return result;
        }

        /// <summary>
        /// Sends raw bytes without a line terminator.
        /// </summary>
        public Task SendRawAsync(byte[] data) => _stream.WriteAsync(data).AsTask();

        /// <summary>
        /// Sends a command and returns the final response line.
        /// </summary>
        public async Task<string?> SendAsync(string command)
        {
            await _stream.WriteAsync(Encoding.UTF8.GetBytes(command + "\r\n"));
            return await ReadResponseAsync();
        }

        /// <summary>
        /// Reads a (possibly multi-line) response and returns its final line, or <see langword="null"/> when the server closed the connection.
        /// </summary>
        public async Task<string?> ReadResponseAsync()
        {
            using var cts = new CancellationTokenSource(ReadTimeout);
            try
            {
                while (true)
                {
                    var line = await _reader.ReadLineAsync(cts.Token);
                    if (line == null)
                    {
                        return null;
                    }

                    // Final line of a reply: "NNN " (multi-line replies use "NNN-")
                    if (line.Length >= 4 && char.IsDigit(line[0]) && line[3] == ' ')
                    {
                        return line;
                    }
                }
            }
            catch (IOException)
            {
                // Connection reset by the server
                return null;
            }
        }

        public async Task LoginAnonymousAsync()
        {
            await SendAsync("USER anonymous");
            var response = await SendAsync("PASS test@test.net");
            if (response?.StartsWith("230", StringComparison.Ordinal) != true)
            {
                throw new InvalidOperationException($"Login failed: {response}");
            }
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            _reader.Dispose();
            _client.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
