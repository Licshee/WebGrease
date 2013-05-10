// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullCacheManager.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System;

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
        /// <param name="contentItem">The result file.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="ICacheSection"/>.</returns>
        public ICacheSection BeginSection(string category, ContentItem contentItem, object settings = null)
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

        /// <summary>Stores the content file in cache.</summary>
        /// <param name="cacheCategory">The cache category.</param>
        /// <param name="contentItem">The content file.</param>
        /// <returns>The cache file path.</returns>
        public string StoreInCache(string cacheCategory, ContentItem contentItem)
        {
            return null;
        }

        #endregion
    }
}