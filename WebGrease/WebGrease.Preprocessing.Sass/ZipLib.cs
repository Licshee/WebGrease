// ----------------------------------------------------------------------------------------------------
// <copyright file="ZipLib.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease.Preprocessing.Sass
{
    using System;
    using System.IO;

    using ICSharpCode.SharpZipLib.Core;
    using ICSharpCode.SharpZipLib.Zip;

    /// <summary>
    /// This helper class, encapsulates methods calling into the SharpZipLib library gotten from nuget.
    /// </summary>
    public static class ZipLib
    {
        #region Public Methods and Operators

        /// <summary>
        /// Exstracts and embedded resource to the target folder
        /// only unpacks files that are non-existent, newer or have a different file length.
        /// </summary>
        /// <param name="resourceName">The full name of the embedded resource. (Including namespace etc...)</param>
        /// <param name="outFolder">The target folder to unpack to.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Not what is happening.")]
        public static void ExtractEmbeddedResource(string resourceName, string outFolder)
        {
            using (var zipStream = typeof(ZipLib).Assembly.GetManifestResourceStream(resourceName))
            {
                using (var zf = new ZipFile(zipStream))
                {
                    Extract(zf, outFolder);
                }
            }
        }

        /// <summary>
        /// Exstracts a file on disk to the target folder
        /// only unpacks files that are non-existent, newer or have a different file length.
        /// </summary>
        /// <param name="archiveFileName">The full path to the archive.</param>
        /// <param name="outFolder">The target folder to unpack to.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Not what is happening.")]
        public static void ExtractZipFile(string archiveFileName, string outFolder)
        {
            using (var fs = File.OpenRead(archiveFileName))
            {
                using (var zf = new ZipFile(fs))
                {
                    Extract(zf, outFolder);
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Extracts a ZipFile object to the targetFolder
        /// only unpacks files that are non-existent, newer or have a different file length.
        /// </summary>
        /// <param name="zf">The zip file object</param>
        /// <param name="outFolder">The target folder.</param>
        private static void Extract(ZipFile zf, string outFolder)
        {
            try
            {
                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue; // Ignore directories
                    }
                    var entryFileName = zipEntry.Name;

                    var fullZipToPath = Path.Combine(outFolder, entryFileName);
                    var directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (File.Exists(fullZipToPath))
                    {
                        var currentFileInfo = new FileInfo(fullZipToPath);
                        if (currentFileInfo.Exists
                            &&
                            (currentFileInfo.CreationTimeUtc == zipEntry.DateTime
                             && currentFileInfo.Length == zipEntry.Size))
                        {
                            continue;
                        }
                    }

                    // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    var buffer = new byte[4096]; // 4K is optimum
                    var zipStream = zf.GetInputStream(zipEntry);

                    // Manipulate the output filename here as desired.
                    if (!string.IsNullOrEmpty(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                    // of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }

                    var fileInfo = new FileInfo(fullZipToPath);
                    fileInfo.CreationTimeUtc = zipEntry.DateTime;
                }
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zf.Close(); // Ensure we release resources
                }
            }
        }

        #endregion
    }
}