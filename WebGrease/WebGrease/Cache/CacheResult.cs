// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CacheResult.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System;
    using System.IO;

    using WebGrease.Extensions;

    /// <summary>The cache result.</summary>
    public class CacheResult
    {
        #region Constructors and Destructors

        /// <summary>Prevents a default instance of the <see cref="CacheResult"/> class from being created.</summary>
        private CacheResult()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>Gets the absolute path.</summary>
        public string AbsolutePath { get; private set; }

        /// <summary>Gets the cached file path.</summary>
        public string CachedFilePath { get; private set; }

        /// <summary>Gets the category.</summary>
        public string Category { get; private set; }

        /// <summary>Gets the content hash.</summary>
        public string ContentHash { get; private set; }

        /// <summary>Gets a value indicating whether end result.</summary>
        public bool EndResult { get; private set; }

        /// <summary>Gets the original relative path.</summary>
        public string OriginalRelativePath { get; private set; }

        /// <summary>Gets the relative path.</summary>
        public string RelativePath { get; private set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>The from content.</summary>
        /// <param name="context">The context.</param>
        /// <param name="cacheCategory">The cache category.</param>
        /// <param name="endResult">The end result.</param>
        /// <param name="fileCategory">The file category.</param>
        /// <param name="content">The content.</param>
        /// <returns>The <see cref="CacheResult"/>.</returns>
        public static CacheResult FromContent(IWebGreaseContext context, string cacheCategory, bool endResult, string fileCategory, string content)
        {
            return new CacheResult
                       {
                           EndResult = endResult, 
                           CachedFilePath = context.Cache.StoreContentInCache(cacheCategory, content), 
                           ContentHash = context.GetContentHash(content), 
                           Category = fileCategory, 
                       };
        }

        /// <summary>The from result file.</summary>
        /// <param name="context">The context.</param>
        /// <param name="cacheCategory">The cache category.</param>
        /// <param name="endResult">The end result.</param>
        /// <param name="fileCategory">The file category.</param>
        /// <param name="resultFile">The result file.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <returns>The <see cref="CacheResult"/>.</returns>
        public static CacheResult FromResultFile(
            IWebGreaseContext context, string cacheCategory, bool endResult, string fileCategory, ResultFile resultFile, string relativePath)
        {
            var cacheFile = (!string.IsNullOrWhiteSpace(resultFile.OriginalPath)
                             && resultFile.OriginalPath.StartsWith(context.Configuration.SourceDirectory, StringComparison.OrdinalIgnoreCase))
                                ? resultFile.OriginalPath
                                : context.Cache.StoreFileInCache(cacheCategory, resultFile.Path);

            return new CacheResult
                       {
                           EndResult = endResult, 
                           CachedFilePath = cacheFile, 
                           Category = fileCategory, 
                           ContentHash = context.GetFileHash(resultFile.Path), 
                           OriginalRelativePath = resultFile.OriginalRelativePath, 
                           AbsolutePath = resultFile.Path, 
                           RelativePath = relativePath, 
                       };
        }

        /// <summary>The from result file.</summary>
        /// <param name="context">The context.</param>
        /// <param name="cacheCategory">The cache category.</param>
        /// <param name="endResult">The end result.</param>
        /// <param name="fileCategory">The file category.</param>
        /// <param name="absolutePath">The absolute path.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <returns>The <see cref="CacheResult"/>.</returns>
        public static CacheResult FromResultFile(
            IWebGreaseContext context, string cacheCategory, bool endResult, string fileCategory, string absolutePath, string relativePath)
        {
            return new CacheResult
                       {
                           EndResult = endResult, 
                           CachedFilePath = context.Cache.StoreFileInCache(cacheCategory, absolutePath), 
                           Category = fileCategory, 
                           ContentHash = context.GetFileHash(absolutePath), 
                           AbsolutePath = absolutePath, 
                           RelativePath = relativePath, 
                       };
        }

        /// <summary>The restore content.</summary>
        /// <returns>The <see cref="string"/>.</returns>
        public string RestoreContent()
        {
            var cachedFileInfo = new FileInfo(this.CachedFilePath);
            if (!cachedFileInfo.Exists)
            {
                return null;
            }

            return File.ReadAllText(cachedFileInfo.FullName);
        }

        /// <summary>The restore file.</summary>
        /// <param name="absoluteTargetPath">The absolute target path.</param>
        /// <param name="overwrite">The overwrite.</param>
        public void RestoreFile(string absoluteTargetPath, bool overwrite)
        {
            var cachedFileInfo = new FileInfo(this.CachedFilePath);
            if (!cachedFileInfo.Exists)
            {
                throw new FileNotFoundException("Could not find cache file: {0}".InvariantFormat(cachedFileInfo.FullName));
            }

            var targetFileInfo = new FileInfo(absoluteTargetPath);
            if (targetFileInfo.Directory != null && !targetFileInfo.Directory.Exists)
            {
                targetFileInfo.Directory.Create();
            }

            if (overwrite || !targetFileInfo.Exists)
            {
                cachedFileInfo.CopyTo(absoluteTargetPath, true);
            }
        }

        #endregion
    }
}