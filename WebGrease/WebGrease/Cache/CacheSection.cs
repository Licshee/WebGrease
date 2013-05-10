// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CacheSection.cs" company="Microsoft">
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
    using System.Text;

    using Microsoft.Ajax.Utilities;

    using Newtonsoft.Json.Linq;

    using WebGrease.Configuration;
    using WebGrease.Css.Extensions;
    using WebGrease.Extensions;

    /// <summary>The cache section.</summary>
    public class CacheSection : ICacheSection
    {
        #region Fields

        /// <summary>The cache results.</summary>
        private readonly List<CacheResult> cacheResults = new List<CacheResult>();

        /// <summary>The child cache sections.</summary>
        private readonly List<CacheSection> childCacheSections = new List<CacheSection>();

        /// <summary>The source dependencies.</summary>
        private readonly IDictionary<string, CacheSourceDependency> sourceDependencies = new Dictionary<string, CacheSourceDependency>();

        /// <summary>The vary by files.</summary>
        private readonly List<CacheVaryByFile> varyByFiles = new List<CacheVaryByFile>();

        /// <summary>The vary by settings.</summary>
        private readonly List<string> varyBySettings = new List<string>();

        /// <summary>The cache category.</summary>
        private string cacheCategory;

        /// <summary>The cached section.</summary>
        private CacheSection cachedSection;

        /// <summary>The context.</summary>
        private IWebGreaseContext context;

        /// <summary>The is from disk.</summary>
        private bool isFromDisk;

        /// <summary>The parent.</summary>
        private CacheSection parent;

        #endregion

        #region Constructors and Destructors

        /// <summary>Prevents a default instance of the <see cref="CacheSection"/> class from being created.</summary>
        private CacheSection()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>Gets the parent.</summary>
        public ICacheSection Parent
        {
            get
            {
                return this.parent;
            }
        }

        #endregion

        #region Properties

        /// <summary>Gets the absolute path.</summary>
        private string AbsolutePath
        {
            get
            {
                return this.context.Cache.GetAbsoluteCacheFilePath(this.cacheCategory, this.FileName);
            }
        }

        /// <summary>Gets the cache section with results.</summary>
        private CacheSection CacheSectionWithResults
        {
            get
            {
                return this.isFromDisk ? this : this.cachedSection;
            }
        }

        /// <summary>Gets the file name.</summary>
        private string FileName
        {
            get
            {
                return this.context.GetValueHash(this.UniqueKey) + ".cache.json";
            }
        }

        /// <summary>Gets the unique key.</summary>
        private string UniqueKey
        {
            get
            {
                return string.Join("|", this.varyByFiles.Select(vbf => vbf.Hash + vbf.Locale + vbf.Theme + Path.GetExtension(vbf.Path)).Concat(this.varyBySettings));
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>Begins the section, create a new section and returns it.</summary>
        /// <param name="context">The context.</param>
        /// <param name="cacheCategory">The cache category.</param>
        /// <param name="action">The action to execute before determinign the unique hash.</param>
        /// <param name="parentCacheSection">The parent cache section.</param>
        /// <returns>The <see cref="CacheSection"/>.</returns>
        public static CacheSection Begin(IWebGreaseContext context, string cacheCategory, Action<ICacheSection> action, ICacheSection parentCacheSection = null)
        {
            var cacheSection = new CacheSection { parent = parentCacheSection as CacheSection, cacheCategory = cacheCategory };

            if (cacheSection.parent != null)
            {
                cacheSection.parent.AddChildCacheSection(cacheSection);
            }

            var cachePath = context.Cache.GetAbsoluteCacheFilePath(cacheCategory, string.Empty);
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }

            cacheSection.context = context;

            action(cacheSection);

            var fileName = new FileInfo(cacheSection.AbsolutePath);
            if (fileName.Exists)
            {
                cacheSection.cachedSection = Load(fileName.FullName, context);
            }

            return cacheSection;
        }

        /// <summary>Adds an end result file from a result file.</summary>
        /// <param name="contentItem">The result file.</param>
        /// <param name="id">The category.</param>
        /// <param name="isEndResult">If the result is an endresult.</param>
        public void AddResult(ContentItem contentItem, string id, bool isEndResult)
        {
            this.context.Measure.Start(SectionIdParts.Cache, SectionIdParts.AddResultFile);
            try
            {
                this.cacheResults.Add(CacheResult.FromContentFile(this.context, this.cacheCategory, isEndResult, id, contentItem));
            }
            finally
            {
                this.context.Measure.End(SectionIdParts.Cache, SectionIdParts.AddResultFile);
            }
        }

        /// <summary>Add a source dependency from a file.</summary>
        /// <param name="file">The file.</param>
        public void AddSourceDependency(string file)
        {
            this.AddSourceDependency(new InputSpec { Path = file });
        }

        /// <summary>Adds a source dependency from a directory.</summary>
        /// <param name="directory">The directory.</param>
        /// <param name="searchPattern">The search pattern.</param>
        /// <param name="searchOption">The search option.</param>
        public void AddSourceDependency(string directory, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            this.AddSourceDependency(new InputSpec { Path = directory, SearchPattern = searchPattern, SearchOption = searchOption });
        }

        /// <summary>Add a source dependency from an input spec.</summary>
        /// <param name="inputSpec">The input spec.</param>
        public void AddSourceDependency(InputSpec inputSpec)
        {
            this.context.Measure.Start(SectionIdParts.Cache, SectionIdParts.AddSourceDependency);
            try
            {
                var key = inputSpec.ToJson(true);
                if (!this.sourceDependencies.ContainsKey(key))
                {
                    this.sourceDependencies.Add(
                        key,
                        CacheSourceDependency.Create(
                            this.context,
                            new InputSpec
                                {
                                    IsOptional = inputSpec.IsOptional,
                                    Path = inputSpec.Path,
                                    SearchOption = inputSpec.SearchOption,
                                    SearchPattern = inputSpec.SearchPattern
                                }));
                }
            }
            finally
            {
                this.context.Measure.End(SectionIdParts.Cache, SectionIdParts.AddSourceDependency);
            }
        }

        /// <summary>If all the cache files are valid and all results could be restored from content.</summary>
        /// <returns>If it can be restored from content.</returns>
        public bool CanBeRestoredFromCache()
        {
            var canBeRestoredFromCache = CanBeRestoredFromCache(this.CacheSectionWithResults);
            if (canBeRestoredFromCache)
            {
                this.CacheSectionWithResults.TouchAllFiles();
            }

            return canBeRestoredFromCache;
        }

        /// <summary>Determiones if all the end results are there and valid, and can therefor be skipped for processing.</summary>
        /// <returns>If it can be skipped.</returns>
        public bool CanBeSkipped()
        {
            var canBeSkipped = CanBeSkipped(this.CacheSectionWithResults);
            if (canBeSkipped)
            {
                this.CacheSectionWithResults.TouchAllFiles();
            }

            return canBeSkipped;
        }

        /// <summary>Ends the section.</summary>
        public void EndSection()
        {
            this.context.Cache.EndSection(this);
        }

        /// <summary>Gets the changed end results recursively.</summary>
        /// <returns>The changed end results.</returns>
        public IEnumerable<CacheResult> GetChangedEndResults()
        {
            var results = this.GetCacheResults(null, true).DistinctBy(r => r.RelativeHashedContentPath);
            return results.Where(
                r =>
                {
                    var absolutePath = Path.Combine(context.Configuration.DestinationDirectory, r.RelativeHashedContentPath);
                    return !File.Exists(absolutePath) || !r.ContentHash.Equals(context.GetFileHash(absolutePath));
                });
        }

        /// <summary>Gets the changed source dependencies recursively.</summary>
        /// <returns>The changed source dependencies.</returns>
        public IEnumerable<CacheSourceDependency> GetChangedSourceDependencies()
        {
            return this.sourceDependencies
                    .Where(csp => csp.Value == null || csp.Value.HasChanged(this.context))
                    .Select(csp => csp.Value)
                    .Concat(this.childCacheSections.SelectMany(css => css.GetChangedSourceDependencies()));
        }

        /// <summary>Gets the changed source dependencies recursively.</summary>
        /// <returns>The changed source dependencies.</returns>
        public IEnumerable<CacheSourceDependency> GetSourceDependencies()
        {
            return this.sourceDependencies.Values
                    .Concat(this.childCacheSections.SelectMany(css => css.GetSourceDependencies()));
        }

        /// <summary>Get the invalid cache results.</summary>
        /// <returns>The invalid cache results.</returns>
        public IEnumerable<CacheResult> GetInvalidCachedResults()
        {
            return this.cacheResults.Where(cr => cr == null || !File.Exists(cr.CachedFilePath));
        }

        /// <summary>Gets the cache results for the category recursively.</summary>
        /// <param name="fileCategory">The category.</param>
        /// <param name="endResultOnly">If it should return end results only.</param>
        /// <returns>The cache results for the category.</returns>
        public IEnumerable<CacheResult> GetCacheResults(string fileCategory = null, bool endResultOnly = false)
        {
            if (this.CacheSectionWithResults == null)
            {
                return NullCacheSection.EmptyCacheResults;
            }

            return this.CacheSectionWithResults.cacheResults
                .Where(cr => (!endResultOnly || cr.EndResult) && (fileCategory == null || cr.FileCategory == fileCategory))
                .Concat(this.CacheSectionWithResults.childCacheSections.SelectMany(css => css.GetCacheResults(fileCategory, endResultOnly)));
        }

        /// <summary>Gets the cached content item.</summary>
        /// <param name="fileCategory">The file category.</param>
        /// <returns>The <see cref="ContentItem"/>.</returns>
        public ContentItem GetCachedContentItem(string fileCategory)
        {
            return ContentItem.FromCacheResult(this.GetCacheResults(fileCategory).First());
        }

        /// <summary>Gets the cached content items.</summary>
        /// <param name="fileCategory">The file category.</param>
        /// <param name="endResultOnly">If it should return end results only.</param>
        /// <returns>The <see cref="ContentItem"/>.</returns>
        public IEnumerable<ContentItem> GetCachedContentItems(string fileCategory, bool endResultOnly = false)
        {
            return this.GetCacheResults(fileCategory, endResultOnly).Select(crf => ContentItem.FromCacheResult(crf));
        }

        /// <summary>Saves the cache section to the cache folder.</summary>
        public void Save()
        {
            var path = new FileInfo(this.AbsolutePath);
            if (path.Directory != null && !path.Directory.Exists)
            {
                path.Directory.Create();
            }

            File.WriteAllText(this.AbsolutePath, this.ToJsonString());
            this.TouchAllFiles();
        }

        /// <summary>Writes a graph report file (.dgml visual studio file).</summary>
        /// <param name="graphReportFilePath">The graph report file path.</param>
        public void WriteDependencyGraph(string graphReportFilePath)
        {
            if (!string.IsNullOrWhiteSpace(graphReportFilePath))
            {
                var dependencyGraph = new CacheDependencyGraph();
                this.AddDepenciesToGraph(dependencyGraph);
                dependencyGraph.Save(graphReportFilePath + ".source.dgml");
            }
        }

        /// <summary>Varys the section by file.</summary>
        /// <param name="contentItem">The result file.</param>
        public void VaryByContentItem(ContentItem contentItem)
        {
            this.varyByFiles.Add(CacheVaryByFile.FromFile(this.context, contentItem));
        }

        /// <summary>Varys the section by settings.</summary>
        /// <param name="settings">The settings.</param>
        /// <param name="nonpublic">Determins if it should non public members of the object as well.</param>
        public void VaryBySettings(object settings, bool nonpublic = false)
        {
            this.varyBySettings.Add(settings.ToJson(nonpublic));
        }

        #endregion

        #region Methods

        /// <summary>The can be restored from cache.</summary>
        /// <param name="cacheSection">The cache section.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private static bool CanBeRestoredFromCache(ICacheSection cacheSection)
        {
            return (cacheSection != null) && !cacheSection.GetInvalidCachedResults().Any() && !cacheSection.GetChangedSourceDependencies().Any();
        }

        /// <summary>The can be skipped.</summary>
        /// <param name="cacheSection">The cache section.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private static bool CanBeSkipped(ICacheSection cacheSection)
        {
            return cacheSection != null && !cacheSection.GetChangedEndResults().Any() && !cacheSection.GetChangedSourceDependencies().Any();
        }

        /// <summary>The from json string.</summary>
        /// <param name="jsonString">The json string.</param>
        /// <param name="context">The context.</param>
        /// <param name="parentCacheSection">The parent cache section.</param>
        /// <returns>The <see cref="CacheSection"/>.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "ap", Justification = "a a")]
        private static CacheSection FromJsonString(string jsonString, IWebGreaseContext context, CacheSection parentCacheSection)
        {
            var cacheSection = new CacheSection { context = context };

            var json = JObject.Parse(jsonString);

            cacheSection.sourceDependencies.AddRange(json["sourceDependencies"].ToString().FromJson<IDictionary<string, CacheSourceDependency>>(true));
            cacheSection.varyByFiles.AddRange(json["varyByFiles"].ToString().FromJson<IEnumerable<CacheVaryByFile>>(true));
            cacheSection.varyBySettings.AddRange(json["varyBySettings"].ToString().FromJson<IEnumerable<string>>(true));
            cacheSection.cacheResults.AddRange(json["cacheResults"].ToString().FromJson<IEnumerable<CacheResult>>(true));
            cacheSection.childCacheSections.AddRange(json["children"].AsEnumerable().Select(f => Load((string)f, context, cacheSection)));
            cacheSection.isFromDisk = true;
            cacheSection.parent = parentCacheSection;
            cacheSection.cacheCategory = (string)json["cacheCategory"];

            return cacheSection;
        }

        /// <summary>The load.</summary>
        /// <param name="fullPath">The full path.</param>
        /// <param name="context">The context.</param>
        /// <param name="parent">The parent.</param>
        /// <returns>The <see cref="CacheSection"/>.</returns>
        private static CacheSection Load(string fullPath, IWebGreaseContext context, CacheSection parent = null)
        {
            if (!File.Exists(fullPath))
            {
                return null;
            }

            return context.Cache.LoadCacheSection(fullPath, () => FromJsonString(File.ReadAllText(fullPath), context, parent));
        }

        /// <summary>The add child cache section.</summary>
        /// <param name="cacheSection">The cache section.</param>
        private void AddChildCacheSection(CacheSection cacheSection)
        {
            this.childCacheSections.Add(cacheSection);
        }

        /// <summary>The add depencies to graph.</summary>
        /// <param name="dependencyGraph">The dependency graph.</param>
        /// <param name="parentNode">The parent node.</param>
        private void AddDepenciesToGraph(CacheDependencyGraph dependencyGraph, string parentNode = null)
        {
            var file = this.varyByFiles.FirstOrDefault();
            if (file != null && !string.IsNullOrWhiteSpace(file.Path))
            {
                parentNode = Path.IsPathRooted(file.Path) ? file.Path.MakeRelativeToDirectory(this.context.Configuration.ApplicationRootDirectory) : file.Path;
            }

            if (parentNode == null)
            {
                parentNode = this.cacheCategory;
            }

            foreach (var cacheSourceDependency in this.sourceDependencies.Values)
            {
                var files =
                    cacheSourceDependency.InputSpec.GetFiles(this.context.Configuration.SourceDirectory)
                                         .Select(f => f.MakeRelativeToDirectory(this.context.Configuration.ApplicationRootDirectory));

                files.ForEach(f => dependencyGraph.AddDependencyLink(parentNode, f));
            }

            this.childCacheSections.ForEach(ccs => ccs.AddDepenciesToGraph(dependencyGraph, parentNode));
        }

        /// <summary>Get a unique json string for the cache section.</summary>
        /// <returns>The json string.</returns>
        private string ToJsonString()
        {
            return
                new
                    {
                        this.sourceDependencies,
                        this.varyByFiles,
                        this.varyBySettings,
                        this.cacheResults,
                        this.cacheCategory,
                        children = this.childCacheSections.Select(ccs => ccs.AbsolutePath)
                    }.ToJson();
        }

        /// <summary>The touch all files.</summary>
        private void TouchAllFiles()
        {
            this.context.Touch(this.AbsolutePath);
            this.cacheResults.ForEach(cr => this.context.Touch(cr.CachedFilePath));

            this.childCacheSections.ForEach(ccs => ccs.TouchAllFiles());
        }

        #endregion
    }
}