// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileHelper.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   FileHelper class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Common
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Text;
    using Extensions;

    /// <summary>FileHelper class.</summary>
    internal static class FileHelper
    {
        /// <summary>Writes the file to hard drive</summary>
        /// <param name="path">Path of file</param>
        /// <param name="content">The contents of file</param>
        /// <param name="encoding">The encoding for output file. If encoding is null, the default UTF-8 encoding is used.</param>
        internal static void WriteFile(string path, string content, Encoding encoding)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(path));
            Contract.Requires(content != null);

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var writer = new StreamWriter(path, false, encoding ?? Encoding.UTF8))
            {
                writer.Write(content);
            }
        }

        /// <summary>
        /// Copy a file from source to destination
        /// </summary>
        /// <param name="source">The source location</param>
        /// <param name="destination">The destination location</param>
        internal static void CopyFile(string source, string destination)
        {
            Contract.Requires(File.Exists(source));
            Contract.Requires(!string.IsNullOrWhiteSpace(destination));

            var directory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(source, destination);
        }
    }
}