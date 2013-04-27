namespace WebGrease
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using WebGrease.Configuration;
    using WebGrease.Extensions;

    public class CacheSection : ICacheSection
    {
        private readonly List<string> varyBySettings = new List<string>();

        private readonly List<CacheVaryByFile> varyByFiles = new List<CacheVaryByFile>();

        private readonly List<CacheSection> childCacheSections = new List<CacheSection>();

        private readonly List<CacheResult> cacheResults = new List<CacheResult>();

        private readonly IDictionary<string, CacheSourceDependency> sourceDependencies = new Dictionary<string, CacheSourceDependency>();

        private CacheSection parent;

        private bool isFromDisk;

        private IWebGreaseContext context;

        private CacheSection cachedSection;

        private string cacheCategory;

        private CacheSection() { }

        public static CacheSection Begin(IWebGreaseContext context, string cacheCategory, Action<CacheSection> action, ICacheSection parentCacheSection = null)
        {
            var cacheSection = new CacheSection
                                   {
                                       parent = parentCacheSection as CacheSection,
                                       cacheCategory = cacheCategory
                                   };

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

        private static CacheSection Load(string fullPath, IWebGreaseContext context, CacheSection parent = null)
        {
            if (!File.Exists(fullPath))
            {
                return null;
            }

            var jsonString = File.ReadAllText(fullPath);
            return FromJsonString(jsonString, context, parent);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "ap", Justification = "a a")]
        private static CacheSection FromJsonString(string jsonString, IWebGreaseContext context, CacheSection parentCacheSection)
        {
            var cacheSection = new CacheSection { context = context };

            var json = JObject.Parse(jsonString);

            cacheSection.sourceDependencies.AddRange(json["sourceDependencies"].ToString().FromJson<IDictionary<string, CacheSourceDependency>>(true));
            cacheSection.varyByFiles.AddRange((json["varyByFiles"].ToString().FromJson<IEnumerable<CacheVaryByFile>>(true)));
            cacheSection.varyBySettings.AddRange((json["varyBySettings"].ToString().FromJson<IEnumerable<string>>(true)));
            cacheSection.cacheResults.AddRange((json["cacheResults"].ToString().FromJson<IEnumerable<CacheResult>>(true)));
            cacheSection.childCacheSections.AddRange(json["children"].AsEnumerable().Select(f => Load((string)f, context, cacheSection)));
            cacheSection.isFromDisk = true;
            cacheSection.parent = parentCacheSection;
            cacheSection.cacheCategory = (string)json["cacheCategory"];

            return cacheSection;
        }

        private string ToJsonString()
        {
            return new
            {
                this.sourceDependencies,
                this.varyByFiles,
                this.varyBySettings,
                this.cacheResults,
                this.cacheCategory,
                children = childCacheSections.Select(ccs=>ccs.AbsolutePath)
            }.ToJson();
        }

        private string AbsolutePath
        {
            get
            {
                return this.context.Cache.GetAbsoluteCacheFilePath(this.cacheCategory, this.FileName);
            }
        }

        private string FileName
        {
            get
            {
                return this.context.GetContentHash(
                        string.Join("|", varyByFiles
                        .Select(vbf => vbf.Hash + Path.GetExtension(vbf.OriginalAbsoluteFilePath))
                        .Concat(varyBySettings)))
                        + ".cache.json";
            }
        }

        public ICacheSection Parent
        {
            get
            {
                return this.parent;
            }
        }

        public bool SourceDependenciesHaveChanged()
        {
            if (CacheSectionWithResults == null)
            {
                return true;
            }

            var cacheSourceDependencies = CacheSectionWithResults.GetSourceDependencies().ToArray();
            return !cacheSourceDependencies.Any() 
                || cacheSourceDependencies.Any(sd => sd.HasChanged(context));
        }

        private IEnumerable<CacheSourceDependency> GetSourceDependencies()
        {
            return sourceDependencies.Values.Concat(childCacheSections.SelectMany(cccs => cccs.GetSourceDependencies()));

        }

        public void VaryByFile(string absoluteFilePath)
        {
            this.varyByFiles.Add(CacheVaryByFile.FromFile(this.context, absoluteFilePath));
        }

        public void VaryBySettings(object settings, bool nonpublic = false)
        {
            this.varyBySettings.Add(settings.ToJson(nonpublic));
        }

        public void EndSection()
        {
            if (!this.IsValid())
            {
                this.Store();
            }
            this.context.Cache.EndSection(this);
        }

        public bool IsValid()
        {
            return IsValid(this.cachedSection) && this.childCacheSections.All(ccs => ccs.IsValid());
        }

        public void RestoreFiles(string category, string targetPath = null, bool overwrite = false)
        {
            var results = this.GetCachedResultFiles(category);
            foreach (var result in results)
            {
                result.Restore(Path.Combine((targetPath ?? this.context.Configuration.ApplicationRootDirectory).EnsureEndSeperatorChar(), result.SolutionRelativePath), overwrite);
            }

            // Execute on all children
            this.CacheSectionWithResults.childCacheSections.ForEach(css =>
                css.RestoreFiles(category, targetPath, overwrite));
        }

        public void RestoreFile(string category, string absolutePath, bool overwrite = false)
        {
            var results = this.GetCachedResultFiles(category);
            if (results.Count() > 1)
            {
                throw new BuildWorkflowException("There were more then one files in the cache that matched that result.");
            }

            foreach (var cacheResult in results)
            {
                cacheResult.Restore(absolutePath, overwrite);
            }

            // Execute on all children
            this.CacheSectionWithResults.childCacheSections.ForEach(css =>
                css.RestoreFile(category, absolutePath, overwrite));
        }


        public void AddSourceDependency(string file)
        {
            AddSourceDependency(new InputSpec { Path = file });
        }

        public void AddSourceDependency(string directory, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            this.AddSourceDependency(new InputSpec
            {
                Path = directory,
                SearchPattern = searchPattern,
                SearchOption = searchOption
            });
        }

        public void AddSourceDependency(InputSpec inputSpec)
        {
            var key = inputSpec.ToJson(true);
            if (!sourceDependencies.ContainsKey(key))
            {
                sourceDependencies.Add(key, CacheSourceDependency.Create(this.context, inputSpec));
            }
        }

        private void Store()
        {
            var path = new FileInfo(this.AbsolutePath);
            if (path.Directory != null && !path.Directory.Exists)
            {
                path.Directory.Create();
            }

            File.WriteAllText(this.AbsolutePath, this.ToJsonString());
        }

        private static bool IsValid(CacheSection cacheSection)
        {
            // There was no cache section
            if (cacheSection == null)
            {
                return false;
            }

            // Any of the cache target files no longer exists.
            if (!cacheSection.cacheResults.All(cr => File.Exists(cr.CachedFilePath)))
            {
                return false;
            }

            return true;
        }

        private IEnumerable<CacheResult> GetCachedResultFiles(string category)
        {
            if (this.CacheSectionWithResults == null)
            {
                throw new WorkflowException("Cached section is null");
            }

            return this.CacheSectionWithResults.cacheResults.Where(cr => category.Equals(cr.Category));
        }

        private CacheSection CacheSectionWithResults
        {
            get
            {
                return this.isFromDisk ? this : this.cachedSection;
            }
        }

        public void AddResultFile(string filePath, string fileCategory, string relativePath = null, string id = null)
        {
            var solutionRelativePath = this.context.MakeRelative(filePath, (relativePath ?? this.context.Configuration.ApplicationRootDirectory).EnsureEndSeperatorChar());
            this.cacheResults.Add(CacheResult.FromResultFile(this.context, this.cacheCategory, id.AsNullIfWhiteSpace() ?? solutionRelativePath, fileCategory, filePath, solutionRelativePath));
        }

        private void AddChildCacheSection(CacheSection cacheSection)
        {
            this.childCacheSections.Add(cacheSection);
        }
    }
}