//-----------------------------------------------------------------------
// <copyright file="FtpCommandCollector.cs" company="Fubar Development Junker">
//     Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>
// <author>Mark Junker</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FubarDev.FtpServer
{
    /// <summary>
    /// Collects FTP commands using the current <see cref="System.Text.Encoding"/>.
    /// </summary>
    public sealed class FtpCommandCollector
    {
        private readonly Func<Encoding> _getActiveEncodingFunc;

        private readonly FtpTelnetInputParser _telnetInputParser;

        private readonly List<byte[]> _buffer = new List<byte[]>();

        private readonly int _maxLineLength;

        private int _pendingLength;

        private bool _skipLineFeed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpCommandCollector"/> class.
        /// </summary>
        /// <param name="getActiveEncodingFunc">The delegate to get the current encoding for.</param>
        /// <param name="maxLineLength">The maximum length of a command line in bytes (excluding the line terminator).</param>
        public FtpCommandCollector(Func<Encoding> getActiveEncodingFunc, int maxLineLength = FtpConnectionOptions.DefaultMaxCommandLineLength)
        {
            if (maxLineLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLineLength), maxLineLength, "The maximum line length must be positive.");
            }

            _telnetInputParser = new FtpTelnetInputParser(this);
            _getActiveEncodingFunc = getActiveEncodingFunc;
            _maxLineLength = maxLineLength;
        }

        /// <summary>
        /// Gets the currently active <see cref="System.Text.Encoding"/>.
        /// </summary>
        public Encoding Encoding => _getActiveEncodingFunc();

        /// <summary>
        /// Gets a value indicating whether this collector contains unused data.
        /// </summary>
        public bool IsEmpty => _buffer.Count == 0;

        /// <summary>
        /// Collects the data from the <paramref name="buffer"/> and tries to build <see cref="FtpCommand"/> objects from it.
        /// </summary>
        /// <param name="buffer">The buffer to collect the data from.</param>
        /// <returns>The found <see cref="FtpCommand"/>s.</returns>
        /// <exception cref="FtpCommandTooLongException">A command line exceeds the maximum line length.</exception>
        public IEnumerable<FtpCommand> Collect(ReadOnlySpan<byte> buffer)
        {
            var commands = new List<FtpCommand>();
            commands.AddRange(_telnetInputParser.Collect(buffer));
            return commands;
        }

        private IEnumerable<FtpCommand> InternalCollect(ReadOnlySpan<byte> buffer)
        {
            var commands = new List<FtpCommand>();

            if (buffer.Length == 0)
            {
                return commands;
            }

            if (_skipLineFeed && buffer[0] == '\n')
            {
                buffer = buffer.Slice(1);
            }

            _skipLineFeed = false;

            do
            {
                var carriageReturnPos = buffer.IndexOf((byte)'\r');
                if (carriageReturnPos == -1)
                {
                    break;
                }

                _skipLineFeed = true;
                if (carriageReturnPos != 0)
                {
                    // Store the found data into the buffer
                    AddPending(buffer.Slice(0, carriageReturnPos));
                }

                if (_buffer.Count != 0)
                {
                    // We've got a non-empty line
                    var bufferLength = _buffer.Sum(x => x.Length);
                    var data = new byte[bufferLength];
                    var dataIndex = 0;
                    foreach (var item in _buffer)
                    {
                        Array.Copy(item, 0, data, dataIndex, item.Length);
                        dataIndex += item.Length;
                    }

                    _buffer.Clear();
                    _pendingLength = 0;

                    commands.Add(CreateFtpCommand(data));
                }

                if (carriageReturnPos < buffer.Length - 1)
                {
                    if (buffer[carriageReturnPos + 1] == '\n')
                    {
                        buffer = buffer.Slice(carriageReturnPos + 2);
                        _skipLineFeed = false;
                    }
                    else
                    {
                        buffer = buffer.Slice(carriageReturnPos + 1);
                    }
                }
                else
                {
                    buffer = ReadOnlySpan<byte>.Empty;
                }
            }
            while (buffer.Length != 0);

            if (buffer.Length != 0)
            {
                AddPending(buffer);
            }

            return commands;
        }

        private void AddPending(ReadOnlySpan<byte> data)
        {
            _pendingLength += data.Length;
            if (_pendingLength > _maxLineLength)
            {
                throw new FtpCommandTooLongException(_maxLineLength);
            }

            _buffer.Add(data.ToArray());
        }

        private FtpCommand CreateFtpCommand(byte[] command)
        {
            var message = Encoding.GetString(command, 0, command.Length);
            return FtpCommand.Parse(message);
        }

        private class FtpTelnetInputParser : TelnetInputParser<FtpCommand>
        {
            private readonly FtpCommandCollector _collector;

            public FtpTelnetInputParser(FtpCommandCollector collector)
            {
                _collector = collector;
            }

            protected override IEnumerable<FtpCommand> DataReceived(ReadOnlySpan<byte> data)
            {
                return _collector.InternalCollect(data);
            }

            /// <inheritdoc />
            protected override IEnumerable<FtpCommand> InterruptProcess()
            {
                return new[]
                {
                    new FtpCommand("ABOR", null),
                };
            }
        }
    }
}
