// <copyright file="SafePath.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.IO;

using FubarDev.FtpServer.FileSystem.Error;

namespace FubarDev.FtpServer.FileSystem.DotNet
{
    /// <summary>
    /// Builds file system paths that are guaranteed to stay inside a root directory.
    /// </summary>
    internal static class SafePath
    {
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        /// <summary>
        /// Combines a directory with a single file or directory name.
        /// </summary>
        /// <param name="rootFullPath">The full path of the file system root.</param>
        /// <param name="directoryFullPath">The full path of the directory containing the entry.</param>
        /// <param name="name">The name of the entry. Must be a single path segment.</param>
        /// <returns>The full path of the entry.</returns>
        /// <exception cref="FileNameNotAllowedException">The name is not a plain file name or the result leaves the root.</exception>
        public static string GetChildPath(string rootFullPath, string directoryFullPath, string name)
        {
            // GetInvalidFileNameChars covers the directory separators, NUL and, on Windows, ':' (drive/stream syntax).
            if (string.IsNullOrEmpty(name)
                || name is "." or ".."
                || name.IndexOfAny(InvalidFileNameChars) >= 0
                || Path.IsPathRooted(name))
            {
                throw new FileNameNotAllowedException($"Invalid file name: {name}");
            }

            var fullPath = Path.GetFullPath(Path.Combine(directoryFullPath, name));
            EnsureWithinRoot(rootFullPath, fullPath);
            return fullPath;
        }

        /// <summary>
        /// Ensures that <paramref name="fullPath"/> is <paramref name="rootFullPath"/> or below it.
        /// </summary>
        /// <param name="rootFullPath">The full path of the root directory.</param>
        /// <param name="fullPath">The full path to check.</param>
        /// <exception cref="FileNameNotAllowedException">The path is outside of the root.</exception>
        public static void EnsureWithinRoot(string rootFullPath, string fullPath)
        {
            var root = Path.TrimEndingDirectorySeparator(rootFullPath);
            var path = Path.TrimEndingDirectorySeparator(fullPath);
            if (string.Equals(path, root, PathComparison)
                || path.StartsWith(root + Path.DirectorySeparatorChar, PathComparison))
            {
                return;
            }

            throw new FileNameNotAllowedException("Path is outside of the root directory.");
        }
    }
}
