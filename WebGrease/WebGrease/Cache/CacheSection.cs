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
                return
                    this.context.GetContentHash(this.UniqueKey) + ".cache.json";
            }
        }

        /// <summary>Gets the unique key.</summary>
        private string UniqueKey
        {
            get
            {
                return string.Join("|", this.varyByFiles.Select(vbf => vbf.Hash + Path.GetExtension(vbf.OriginalAbsoluteFilePath)).Concat(this.varyBySettings));
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

        /// <summary>Adds an end result file from a filepath.</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="category">The category.</param>
        public void AddEndResultFile(string filePath, string category)
        {
            this.context.Measure.Start(TimeMeasureNames.Cache, TimeMeasureNames.AddResultFile);
            try
            {
                var applicationRootRelativePath = filePath.MakeRelativeToDirectory(this.context.Configuration.DestinationDirectory);
                this.cacheResults.Add(CacheResult.FromResultFile(this.context, this.cacheCategory, true, category, filePath, applicationRootRelativePath));
            }
            finally
            {
                this.context.Measure.End(TimeMeasureNames.Cache, TimeMeasureNames.AddResultFile);
            }
        }

        /// <summary>Adds an end result file from a result file.</summary>
        /// <param name="resultFile">The result file.</param>
        /// <param name="category">The category.</param>
        public void AddEndResultFile(ResultFile resultFile, string category)
        {
            this.context.Measure.Start(TimeMeasureNames.Cache, TimeMeasureNames.AddResultFile);
            try
            {
                var applicationRootRelativePath = resultFile.Path.MakeRelativeToDirectory(this.context.Configuration.DestinationDirectory);
                this.cacheResults.Add(CacheResult.FromResultFile(this.context, this.cacheCategory, true, category, resultFile, applicationRootRelativePath));
            }
            finally
            {
                this.context.Measure.End(TimeMeasureNames.Cache, TimeMeasureNames.AddResultFile);
            }
        }

        /// <summary>Add result content.</summary>
        /// <param name="content">The content.</param>
        /// <param name="category">The category.</param>
        /// <param name="endResult">If it is an endresult.</param>
        public void AddResultContent(string content, string category, bool endResult = false)
        {
            this.context.Measure.Start(TimeMeasureNames.Cache, TimeMeasureNames.AddResultContent);
            try
            {
                this.cacheResults.Add(CacheResult.FromContent(this.context, this.cacheCategory, endResult, category, content));
            }
            finally
            {
                this.context.Measure.End(TimeMeasureNames.Cache, TimeMeasureNames.AddResultContent);
            }
        }

        /// <summary>Adds a result file.</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="category">The category.</param>
        /// <param name="relativePath">The relative path.</param>
        public void AddResultFile(string filePath, string category, string relativePath = null)
        {
            this.context.Measure.Start(TimeMeasureNames.Cache, TimeMeasureNames.AddResultFile);
            try
            {
                var applicationRootRelativePath = this.context.MakeRelative(
                    filePath, (relativePath ?? this.context.Configuration.ApplicationRootDirectory).EnsureEndSeperatorChar());
                this.cacheResults.Add(CacheResult.FromResultFile(this.context, this.cacheCategory, false, category, filePath, applicationRootRelativePath));
            }
            finally
            {
                this.context.Measure.End(TimeMeasureNames.Cache, TimeMeasureNames.AddResultFile);
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
            this.context.Measure.Start(TimeMeasureNames.Cache, TimeMeasureNames.AddSourceDependency);
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
                this.context.Measure.End(TimeMeasureNames.Cache, TimeMeasureNames.AddSourceDependency);
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
            var results = this.cacheResults.Where(cr => cr.EndResult);
            return results.Where(
                r =>
                    {
                        var absolutePath = Path.Combine(context.Configuration.DestinationDirectory, r.RelativePath);
                        return !File.Exists(absolutePath) || !r.ContentHash.Equals(context.GetFileHash(absolutePath));
                    }).Concat(this.childCacheSections.SelectMany(css => css.GetChangedEndResults()));
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

        /// <summary>Get the invalid cache results.</summary>
        /// <returns>The invalid cache results.</returns>
        public IEnumerable<CacheResult> GetInvalidCachedResults()
        {
            return this.cacheResults.Where(cr => cr == null || !File.Exists(cr.CachedFilePath));
        }

        /// <summary>Gets the cache results for the category recursively.</summary>
        /// <param name="category">The category.</param>
        /// <param name="endResultOnly">If it should return end results only.</param>
        /// <returns>The cache results for the category.</returns>
        public IEnumerable<CacheResult> GetResults(string category = null, bool endResultOnly = false)
        {
            if (this.CacheSectionWithResults == null)
            {
                return NullCacheSection.EmptyCacheResults;
            }

            return this.CacheSectionWithResults.cacheResults
                .Where(cr => (!endResultOnly || cr.EndResult) && (category == null || cr.Category == category))
                .Concat(this.CacheSectionWithResults.childCacheSections.SelectMany(css => css.GetResults(category, endResultOnly)));
        }

        /// <summary>Restores / Gets content from cache.</summary>
        /// <param name="category">The category.</param>
        /// <returns>The content from cache.</returns>
        public string RestoreContent(string category)
        {
            var results = this.GetCachedResultFiles(category);
            if (results.Count() > 1)
            {
                throw new BuildWorkflowException("There were more then one files in the cache that matched that result.");
            }

            return results.Select(r => r.RestoreContent()).FirstOrDefault();
        }

        /// <summary>Restores the file from cache recursively.</summary>
        /// <param name="category">The category.</param>
        /// <param name="absolutePath">The absolute path.</param>
        /// <param name="overwrite">If it should overwrit if the file already exists.</param>
        public void RestoreFile(string category, string absolutePath, bool overwrite = false)
        {
            var results = this.GetCachedResultFiles(category);
            if (results.Count() > 1)
            {
                throw new BuildWorkflowException("There were more then one files in the cache that matched that result.");
            }

            foreach (var cacheResult in results)
            {
                cacheResult.RestoreFile(absolutePath, overwrite);
            }

            // Execute on all children
            this.CacheSectionWithResults.childCacheSections.ForEach(css => css.RestoreFile(category, absolutePath, overwrite));
        }

        /// <summary>Restore files from cache recursively.</summary>
        /// <param name="category">The category.</param>
        /// <param name="targetPath">The target path.</param>
        /// <param name="overwrite">The overwrite.</param>
        /// <returns>The restored files.</returns>
        public IEnumerable<CacheResult> RestoreFiles(string category, string targetPath = null, bool overwrite = true)
        {
            var results = this.GetCachedResultFiles(category);
            foreach (var result in results)
            {
                result.RestoreFile(
                    Path.Combine((targetPath ?? this.context.Configuration.ApplicationRootDirectory).EnsureEndSeperatorChar(), result.RelativePath), overwrite);
            }

            // Execute on all children
            return results.Concat(this.CacheSectionWithResults.childCacheSections.SelectMany(css => css.RestoreFiles(category, targetPath, overwrite))).ToArray();
        }

        /// <summary>Stores a graph report file (.dgml visual studio file).</summary>
        /// <param name="graphReportFilePath">The graph report file path.</param>
        public void Store(string graphReportFilePath)
        {
            var path = new FileInfo(this.AbsolutePath);
            if (path.Directory != null && !path.Directory.Exists)
            {
                path.Directory.Create();
            }

            File.WriteAllText(this.AbsolutePath, this.ToJsonString());
            this.TouchAllFiles();

            if (!string.IsNullOrWhiteSpace(graphReportFilePath) && this.context.Configuration.CacheOutputDependencies)
            {
                var dependencyGraph = new CacheDependencyGraph();
                this.AddDepenciesToGraph(dependencyGraph);
                dependencyGraph.Save(graphReportFilePath + ".source.dgml");
            }
        }

        /// <summary>Varys the section by file.</summary>
        /// <param name="absoluteFilePath">The absolute file path.</param>
        public void VaryByFile(string absoluteFilePath)
        {
            this.varyByFiles.Add(CacheVaryByFile.FromFile(this.context, absoluteFilePath));
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
            if (file != null && !string.IsNullOrWhiteSpace(file.OriginalAbsoluteFilePath))
            {
                parentNode = file.OriginalAbsoluteFilePath.MakeRelativeToDirectory(this.context.Configuration.ApplicationRootDirectory);
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

        /// <summary>Gets the cached result files for a category.</summary>
        /// <param name="category">The category.</param>
        /// <returns>The cached result files.</returns>
        private IEnumerable<CacheResult> GetCachedResultFiles(string category)
        {
            if (this.CacheSectionWithResults == null)
            {
                throw new WorkflowException("Cached section is null");
            }

            return this.CacheSectionWithResults.cacheResults.Where(cr => category.Equals(cr.Category));
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