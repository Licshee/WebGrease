// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullCacheManager.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System;
    using System.IO;

    /// <summary>The null cache manager.</summary>
    public class NullCacheManager : ICacheManager
    {
        #region Static Fields

        /// <summary>The empty cache section.</summary>
        internal static readonly ICacheSection EmptyCacheSection = new NullCacheSection();

        #endregion

        #region Public Properties

        /// <summary>Gets the current cache section.</summary>
        public ICacheSection CurrentCacheSection
        {
            get
            {
                return EmptyCacheSection;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>Begins a new cache section.</summary>
        /// <param name="category">The category.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="ICacheSection"/>.</returns>
        public ICacheSection BeginSection(string category, object settings)
        {
            return EmptyCacheSection;
        }

        /// <summary>Begins a new cache section.</summary>
        /// <param name="category">The category.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="ICacheSection"/>.</returns>
        public ICacheSection BeginSection(string category, FileInfo filePath, object settings = null)
        {
            return EmptyCacheSection;
        }

        /// <summary>Cleans up all the cache files that we don't need anymore.</summary>
        public void CleanUp()
        {
        }

        /// <summary>Ends the cache section.</summary>
        /// <param name="cacheSection">The cache section.</param>
        public void EndSection(ICacheSection cacheSection)
        {
        }

        /// <summary>Gets absolute cache file path.</summary>
        /// <param name="category">The category.</param>
        /// <param name="fileName">The relative cache file name.</param>
        /// <returns>The absolute cache file path.</returns>
        public string GetAbsoluteCacheFilePath(string category, string fileName)
        {
            return null;
        }

        /// <summary>Loads a cache section from disk, uses per session in memory cache as well.</summary>
        /// <param name="fullPath">The full path.</param>
        /// <param name="loadAction">The load action.</param>
        /// <typeparam name="T">The Type of ICacheSection</typeparam>
        /// <returns>The cache section.</returns>
        public T LoadCacheSection<T>(string fullPath, Func<T> loadAction) where T : class, ICacheSection
        {
            return EmptyCacheSection as T;
        }

        /// <summary>Sets the current context.</summary>
        /// <param name="newContext">The current context.</param>
        public void SetContext(IWebGreaseContext newContext)
        {
        }

        /// <summary>Stores the content in cache.</summary>
        /// <param name="category">The category.</param>
        /// <param name="content">The content.</param>
        /// <returns>The stored cache file path.</returns>
        public string StoreContentInCache(string category, string content)
        {
            return null;
        }

        /// <summary>Stores the file in cache.</summary>
        /// <param name="category">The category.</param>
        /// <param name="absolutePath">The absolute path.</param>
        /// <returns>The stored cache file path.</returns>
        public string StoreFileInCache(string category, string absolutePath)
        {
            return null;
        }

        #endregion
    }
}