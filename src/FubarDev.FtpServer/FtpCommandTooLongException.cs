// <copyright file="FtpCommandTooLongException.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;

namespace FubarDev.FtpServer
{
    /// <summary>
    /// Thrown when a client sends a command line that exceeds <see cref="FtpConnectionOptions.MaxCommandLineLength"/>.
    /// </summary>
    public class FtpCommandTooLongException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FtpCommandTooLongException"/> class.
        /// </summary>
        /// <param name="maxLineLength">The maximum allowed line length in bytes.</param>
        public FtpCommandTooLongException(int maxLineLength)
            : base($"Command line exceeds the maximum length of {maxLineLength} bytes.")
        {
            MaxLineLength = maxLineLength;
        }

        /// <summary>
        /// Gets the maximum allowed line length in bytes.
        /// </summary>
        public int MaxLineLength { get; }
    }
}
