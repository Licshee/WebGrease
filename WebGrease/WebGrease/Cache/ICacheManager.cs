// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICacheManager.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using WebGrease.Configuration;

    /// <summary>The CacheManager interface.</summary>
    public interface ICacheManager
    {
        #region Public Properties

        /// <summary>Gets the current cache section.</summary>
        ICacheSection CurrentCacheSection { get; }

        IDictionary<string, ReadOnlyCacheSection> LoadedCacheSections { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>Begins a new cache section.</summary>
        /// <param name="category">The category.</param>
        /// <param name="contentItem">The result file.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="cacheVarByFileSet">The cache Var By File Set.</param>
        /// <param name="cacheIsSkipable">The cache Is Skipable.</param>
        /// <returns>The <see cref="ICacheSection"/>.</returns>
        ICacheSection BeginSection(string category, ContentItem contentItem = null, object settings = null, IFileSet cacheVarByFileSet = null, bool cacheIsSkipable = false);

        /// <summary>Cleans up all the cache files that we don't need anymore.</summary>
        void CleanUp();

        /// <summary>Ends the cache section.</summary>
        /// <param name="cacheSection">The cache section.</param>
        void EndSection(ICacheSection cacheSection);

        /// <summary>Gets absolute cache file path.</summary>
        /// <param name="category">The category.</param>
        /// <param name="fileName">The relative cache file name.</param>
        /// <returns>The absolute cache file path.</returns>
        string GetAbsoluteCacheFilePath(string category, string fileName);

        /// <summary>Sets the current context.</summary>
        /// <param name="newContext">The current context.</param>
        void SetContext(IWebGreaseContext newContext);

        /// <summary>Stores the content file in cache.</summary>
        /// <param name="cacheCategory">The cache category.</param>
        /// <param name="contentItem">The content file.</param>
        /// <returns>The cache file path.</returns>
        string StoreInCache(string cacheCategory, ContentItem contentItem);

        #endregion
    }
}