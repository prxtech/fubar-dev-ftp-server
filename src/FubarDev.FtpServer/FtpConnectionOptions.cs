// <copyright file="FtpConnectionOptions.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Text;

namespace FubarDev.FtpServer
{
    /// <summary>
    /// Options for the FTP connection.
    /// </summary>
    public class FtpConnectionOptions
    {
        /// <summary>
        /// The default value for <see cref="MaxCommandLineLength"/>.
        /// </summary>
        public const int DefaultMaxCommandLineLength = 4096;

        /// <summary>
        /// Gets or sets the default connection encoding.
        /// </summary>
        public Encoding DefaultEncoding { get; set; } = Encoding.ASCII;

        /// <summary>
        /// Gets or sets the default connection inactivity timeout.
        /// </summary>
        public TimeSpan? InactivityTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the maximum length of a single command line in bytes (excluding the line terminator).
        /// </summary>
        /// <remarks>
        /// Clients that send longer lines are disconnected. This protects against memory exhaustion
        /// by unauthenticated clients that never send a line terminator.
        /// </remarks>
        public int MaxCommandLineLength { get; set; } = DefaultMaxCommandLineLength;
    }
}
