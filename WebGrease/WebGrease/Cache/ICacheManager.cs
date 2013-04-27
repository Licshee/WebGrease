namespace WebGrease
{
    using System;

    public interface ICacheManager
    {
        ICacheSection BeginSection(string category, object settings);

        ICacheSection BeginSection(string category, string filePath, object settings);

        string StoreInCache(string category, string absolutePath);

        string GetAbsoluteCacheFilePath(string category, string fileName);

        void EndSection(CacheSection cacheSection);

        ICacheSection CurrentCacheSection { get; }
    }
}