// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CacheKeys.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace WebGrease
{
    /// <summary>The cache keys.</summary>
    internal static class CacheKeys
    {
        #region Constants

        /// <summary>The hashed image cache key.</summary>
        internal const string HashedImageCacheKey = "hashedimage";

        /// <summary>The hashed sprite image cache key.</summary>
        internal const string HashedSpriteImageCacheKey = "HashedImageCacheKey";

        /// <summary>The minified css result cache key.</summary>
        internal const string MinifiedCssResultCacheKey = "MinifyCssResultCacheKey";

        /// <summary>The minify js result cache key.</summary>
        internal const string MinifyJsResultCacheKey = "MinifyJsResultCacheKey";

        /// <summary>The sprite log file cache key.</summary>
        internal const string SpriteLogFileCacheKey = "SpriteLogFileCacheKey";

        #endregion
    }
}