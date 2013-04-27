namespace WebGrease
{
    using System.IO;

    public class CacheResult
    {
        private CacheResult()
        {

        }

        public string SolutionRelativePath { get; private set; }

        public string OriginalPath { get; private set; }

        public string CachedFilePath { get; private set; }

        public string Category { get; private set; }

        public string Id { get; private set; }

        public bool Restore(string absoluteTargetPath, bool overwrite)
        {
            var cachedFileInfo = new FileInfo(this.CachedFilePath);
            if (!cachedFileInfo.Exists)
            {
                return false;
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

            return true;
        }

        public static CacheResult FromResultFile(IWebGreaseContext context, string cacheCategory, string id, string fileCategory, string absolutePath, string solutionRelativePath)
        {
            return new CacheResult
                       {
                           Id = id,
                           CachedFilePath = context.Cache.StoreInCache(cacheCategory, absolutePath),
                           Category = fileCategory,
                           OriginalPath = absolutePath,
                           SolutionRelativePath = solutionRelativePath,
                       };

        }
    }
}