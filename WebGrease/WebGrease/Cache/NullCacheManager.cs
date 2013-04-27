namespace WebGrease
{
    using System;

    public class NullCacheManager : ICacheManager
    {
        private readonly ICacheSection emptyCacheSection = new NullCacheSection();

        public ICacheSection BeginSection(string category, object settings)
        {
            return emptyCacheSection;
        }

        public ICacheSection BeginSection(string category, string filePath, object settings)
        {
            return emptyCacheSection;
        }

        public string StoreInCache(string category, string absolutePath)
        {
            return null;
        }

        public string GetAbsoluteCacheFilePath(string category, string fileName)
        {
            return null;
        }

        public void EndSection(CacheSection cacheSection)
        {
        }

        public ICacheSection CurrentCacheSection
        {
            get
            {
                return emptyCacheSection;
            }
        }
    }
}