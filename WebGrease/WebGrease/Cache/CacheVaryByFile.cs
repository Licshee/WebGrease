// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CacheVaryByFile.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease
{
    /// <summary>The cache vary by file.</summary>
    public class CacheVaryByFile
    {
        #region Constructors and Destructors

        /// <summary>Prevents a default instance of the <see cref="CacheVaryByFile"/> class from being created.</summary>
        private CacheVaryByFile()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>Gets the hash.</summary>
        public string Hash { get; private set; }

        /// <summary>Gets the original absolute file path.</summary>
        public string OriginalAbsoluteFilePath { get; private set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>Creates a <see cref="CacheVaryByFile"/> from content.</summary>
        /// <param name="context">The context.</param>
        /// <param name="fileContent">The file content.</param>
        /// <returns>The <see cref="CacheVaryByFile"/>.</returns>
        public static CacheVaryByFile FromContent(IWebGreaseContext context, string fileContent)
        {
            return new CacheVaryByFile { Hash = context.GetContentHash(fileContent) };
        }

        /// <summary>Creates a <see cref="CacheVaryByFile"/> from a file.</summary>
        /// <param name="context">The context.</param>
        /// <param name="absoluteFilePath">The absolute file path.</param>
        /// <returns>The <see cref="CacheVaryByFile"/>.</returns>
        public static CacheVaryByFile FromFile(IWebGreaseContext context, string absoluteFilePath)
        {
            return new CacheVaryByFile
                       {
                           OriginalAbsoluteFilePath = absoluteFilePath, 
                           Hash = context.GetFileHash(absoluteFilePath), 
                       };
        }

        #endregion
    }
}