// <copyright file="PortCommandOptions.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

namespace FubarDev.FtpServer
{
    /// <summary>
    /// Options for the <c>PORT</c> command.
    /// </summary>
    public class PortCommandOptions
    {
        /// <summary>
        /// Gets or sets the data port.
        /// </summary>
        public int? DataPort { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <c>PORT</c>/<c>EPRT</c> may target an address
        /// other than the address of the control connection.
        /// </summary>
        /// <remarks>
        /// Enabling this allows FTP bounce attacks (RFC 2577) and should only be used
        /// for server-to-server (FXP) transfers in trusted networks.
        /// </remarks>
        public bool AllowForeignAddress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <c>PORT</c>/<c>EPRT</c> may target a privileged port (below 1024).
        /// </summary>
        public bool AllowPrivilegedPort { get; set; }
    }
}
