namespace WebGrease
{
    using System.IO;

    using WebGrease.Extensions;

    public class CacheManager : ICacheManager
    {
        private readonly string cacheRootPath;

        private readonly IWebGreaseContext context;

        private ICacheSection currentCacheSection;

        public CacheManager(IWebGreaseContext context)
        {
            this.context = context;
            var cacheRoot =
                context.Configuration.CacheRootPath.AsNullIfWhiteSpace()
                ?? context.Configuration.ToolsTempDirectory.AsNullIfWhiteSpace()
                ?? Path.GetTempPath();

            this.cacheRootPath = Path.Combine(
                cacheRoot,
                context.Configuration.CacheUniqueKey.AsNullIfWhiteSpace() ?? "_WebGrease.Cache",
                context.Configuration.ConfigType ?? "Unknown");

            if (!Directory.Exists(cacheRootPath))
            {
                Directory.CreateDirectory(cacheRootPath);
            }
        }

        public ICacheSection CurrentCacheSection
        {
            get
            {
                return this.currentCacheSection;
            }
        }

        public ICacheSection BeginSection(string category, object settings)
        {
            return (this.currentCacheSection = CacheSection.Begin(context, category, (cs => cs.VaryBySettings(settings)), this.CurrentCacheSection));
        }

        public ICacheSection BeginSection(string category, string filePath, object settings)
        {
            return (this.currentCacheSection = CacheSection.Begin(context, category, (cs =>
                {
                    cs.VaryByFile(filePath);
                    cs.VaryBySettings(settings);
                }), this.CurrentCacheSection));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "WebGrease", Justification = "")]
        public string StoreInCache(string category, string absolutePath)
        {
            var uniqueId = this.context.GetFileHash(absolutePath);
            var sourceFi = new FileInfo(absolutePath);
            if (!sourceFi.Exists)
            {
                throw new FileNotFoundException("Could not find the result file to store in the WebGrease cache", absolutePath);
            }

            var absoluteCacheFilePath = this.GetAbsoluteCacheFilePath(category, uniqueId + Path.GetExtension(absolutePath));

            var targetFi = new FileInfo(absoluteCacheFilePath);
            if (targetFi.Exists)
            {
                var sourceMd5 = context.GetFileHash(sourceFi.FullName);
                var targetMd5 = context.GetFileHash(targetFi.FullName);
                if (sourceMd5.Equals(targetMd5))
                {
                    return absoluteCacheFilePath;
                }
            }
            else if (targetFi.Directory != null && !targetFi.Directory.Exists)
            {
                targetFi.Directory.Create();
            }

            sourceFi.CopyTo(targetFi.FullName, true);

            return absoluteCacheFilePath;
        }

        public string GetAbsoluteCacheFilePath(string category, string fileName)
        {
            return Path.Combine(this.cacheRootPath, category, fileName);
        }

        public void EndSection(CacheSection cacheSection)
        {
            if (this.CurrentCacheSection != cacheSection)
            {
                throw new BuildWorkflowException("Something unexpected went wrong with the caching logic.");
            }

            this.currentCacheSection = cacheSection.Parent;
        }
    }
}