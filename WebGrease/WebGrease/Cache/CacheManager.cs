// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CacheManager.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;

    using WebGrease.Configuration;
    using WebGrease.Css.Extensions;
    using WebGrease.Extensions;

    /// <summary>The cache manager.</summary>
    public class CacheManager : ICacheManager
    {
        #region Static Fields

        /// <summary>The null cache section.</summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "For easy access and reuse.")]
        public static readonly ICacheSection NullCacheSection = new NullCacheSection();

        #endregion

        #region Fields

        /// <summary>The cache root path.</summary>
        private readonly string cacheRootPath;

        /// <summary>The cached cache sections.</summary>
        private readonly IDictionary<string, ICacheSection> cachedCacheSections = new Dictionary<string, ICacheSection>();

        /// <summary>The context.</summary>
        private IWebGreaseContext context;

        /// <summary>The current cache section.</summary>
        private ICacheSection currentCacheSection;

        #endregion

        #region Constructors and Destructors

        /// <summary>Initializes a new instance of the <see cref="CacheManager"/> class.</summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logManager">The log manager.</param>
        public CacheManager(WebGreaseConfiguration configuration, LogManager logManager)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (logManager == null)
            {
                throw new ArgumentNullException("logManager");
            }

            var cacheRoot = configuration.CacheRootPath.AsNullIfWhiteSpace() ?? "_webgrease.cache";

            if (!Path.IsPathRooted(cacheRoot))
            {
                cacheRoot = Path.Combine(configuration.SourceDirectory, cacheRoot);
            }

            this.cacheRootPath = Path.Combine(cacheRoot, configuration.CacheUniqueKey ?? string.Empty, configuration.ConfigType) ?? string.Empty;

            if (!Directory.Exists(this.cacheRootPath))
            {
                Directory.CreateDirectory(this.cacheRootPath);
            }

            logManager.Information("Cache enabled using cache path: {0}".InvariantFormat(this.cacheRootPath));
        }

        #endregion

        #region Public Properties

        /// <summary>Gets the current cache section.</summary>
        public ICacheSection CurrentCacheSection
        {
            get
            {
                return this.currentCacheSection;
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
            return this.currentCacheSection = CacheSection.Begin(this.context, category, cs => cs.VaryBySettings(settings), this.CurrentCacheSection);
        }

        /// <summary>Begins a new cache section.</summary>
        /// <param name="category">The category.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="ICacheSection"/>.</returns>
        public ICacheSection BeginSection(string category, FileInfo filePath, object settings = null)
        {
            return this.currentCacheSection = CacheSection.Begin(
                this.context, 
                category, 
                cs =>
                    {
                        cs.VaryByFile(filePath.FullName);
                        cs.VaryBySettings(settings);
                    }, 
                this.CurrentCacheSection);
        }

        /// <summary>Cleans up all the cache files that we don't need anymore.</summary>
        public void CleanUp()
        {
            var startTime = this.context.SessionStartTime;
            if (this.context.Configuration.CacheTimeout.TotalSeconds > 0)
            {
                var expireTime = startTime - this.context.Configuration.CacheTimeout;
                var allFiles = Directory.GetFiles(this.cacheRootPath, "*.*", SearchOption.AllDirectories);
                allFiles.Where(f => File.GetLastWriteTimeUtc(f) < expireTime).ForEach(File.Delete);
            }
        }

        /// <summary>Ends the cache section.</summary>
        /// <param name="cacheSection">The cache section.</param>
        public void EndSection(ICacheSection cacheSection)
        {
            if (this.CurrentCacheSection != cacheSection)
            {
                throw new BuildWorkflowException("Something unexpected went wrong with the caching logic.");
            }

            this.currentCacheSection = cacheSection.Parent;
        }

        /// <summary>Gets absolute cache file path.</summary>
        /// <param name="category">The category.</param>
        /// <param name="fileName">The relative cache file name.</param>
        /// <returns>The absolute cache file path.</returns>
        public string GetAbsoluteCacheFilePath(string category, string fileName)
        {
            return Path.Combine(this.cacheRootPath, category, fileName);
        }

        /// <summary>Loads a cache section from disk, uses per session in memory cache as well.</summary>
        /// <param name="fullPath">The full path.</param>
        /// <param name="loadAction">The load action.</param>
        /// <typeparam name="T">The Type of ICacheSection</typeparam>
        /// <returns>The cache section.</returns>
        public T LoadCacheSection<T>(string fullPath, Func<T> loadAction) where T : class, ICacheSection
        {
            var key = new FileInfo(fullPath).FullName.ToUpperInvariant();
            if (!this.cachedCacheSections.ContainsKey(key))
            {
                this.cachedCacheSections.Add(key, loadAction());
            }

            var cacheSection = this.cachedCacheSections[key] as T;
            return cacheSection;
        }

        /// <summary>Sets the current context.</summary>
        /// <param name="newContext">The current context.</param>
        public void SetContext(IWebGreaseContext newContext)
        {
            this.context = newContext;
        }

        /// <summary>Stores the content in cache.</summary>
        /// <param name="category">The category.</param>
        /// <param name="content">The content.</param>
        /// <returns>The cache file path.</returns>
        public string StoreContentInCache(string category, string content)
        {
            var uniqueId = this.context.GetContentHash(content);
            var absoluteCacheFilePath = this.GetAbsoluteCacheFilePath(category, uniqueId + ".txt");

            var targetFi = new FileInfo(absoluteCacheFilePath);
            if (!targetFi.Exists)
            {
                if (targetFi.Directory != null && !targetFi.Directory.Exists)
                {
                    targetFi.Directory.Create();
                }

                File.WriteAllText(absoluteCacheFilePath, content);
            }

            return absoluteCacheFilePath;
        }

        /// <summary>Stores the file in cache.</summary>
        /// <param name="category">The category.</param>
        /// <param name="absolutePath">The absolute path.</param>
        /// <returns>The stored cache file path.</returns>
        public string StoreFileInCache(string category, string absolutePath)
        {
            var uniqueId = this.context.GetFileHash(absolutePath);
            var sourceFi = new FileInfo(absolutePath);
            if (!sourceFi.Exists)
            {
                throw new FileNotFoundException("Could not find the result file to store in the cache", absolutePath);
            }

            var isSourcePath = absolutePath.StartsWith(this.context.Configuration.SourceDirectory, StringComparison.OrdinalIgnoreCase);
            if (isSourcePath)
            {
                return absolutePath;
            }

            var absoluteCacheFilePath = this.GetAbsoluteCacheFilePath(category, uniqueId + Path.GetExtension(absolutePath));
            var targetFi = new FileInfo(absoluteCacheFilePath);
            if (targetFi.Exists)
            {
                return absoluteCacheFilePath;
            }

            if (targetFi.Directory != null && !targetFi.Directory.Exists)
            {
                targetFi.Directory.Create();
            }

            sourceFi.CopyTo(targetFi.FullName, true);

            return absoluteCacheFilePath;
        }

        #endregion
    }
}