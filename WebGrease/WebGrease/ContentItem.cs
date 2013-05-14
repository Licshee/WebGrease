// ----------------------------------------------------------------------------------------------------
// <copyright file="ContentItem.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System.IO;

    /// <summary>The content item.</summary>
    public class ContentItem
    {
        /// <summary>The content hash.</summary>
        private string contentHash;

        #region Constructors and Destructors

        /// <summary>Prevents a default instance of the <see cref="ContentItem"/> class from being created.</summary>
        private ContentItem()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>Gets the origin relative source path.</summary>
        public string RelativeContentPath { get; private set; }

        /// <summary>Gets the theme.</summary>
        public string Theme { get; private set; }

        /// <summary>Gets the locale.</summary>
        public string Locale { get; private set; }

        /// <summary>Gets the alternate relative path.</summary>
        public string RelativeHashedContentPath { get; private set; }

        /// <summary>Gets the content of the file.</summary>
        /// <value>The file content.</value>
        public string Content
        {
            get
            {
                return this.ContentItemType == ContentItemType.Path
                    ? File.ReadAllText(this.AbsoluteContentPath)
                    : this.ContentValue;
            }
        }

        /// <summary>Gets the content.</summary>
        private string ContentValue { get; set; }

        /// <summary>Gets the absolute destination path.</summary>
        private string AbsoluteContentPath { get; set; }

        /// <summary>Gets the content type.</summary>
        private ContentItemType ContentItemType { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>The from content.</summary>
        /// <param name="content">The content.</param>
        /// <param name="relativeContentPath">The original Relative Path.</param>
        /// <param name="relativeHashedContentPath">The alternate Relative Path.</param>
        /// <param name="locale">The locale.</param>
        /// <param name="theme">The theme.</param>
        /// <returns>The <see cref="ContentItem"/>.</returns>
        public static ContentItem FromContent(string content, string relativeContentPath, string relativeHashedContentPath = null, string locale = null, string theme = null)
        {
            return new ContentItem
                         {
                             ContentItemType = ContentItemType.Value,
                             ContentValue = content,
                             Locale = locale,
                             Theme = theme,
                             RelativeContentPath = relativeContentPath,
                             RelativeHashedContentPath = relativeHashedContentPath,
                         };
        }

        /// <summary>Creates a content item from a cache result.</summary>
        /// <param name="cacheResult">The cache result.</param>
        /// <param name="locale">The locale.</param>
        /// <param name="theme">The theme.</param>
        /// <returns>The <see cref="ContentItem"/>.</returns>
        public static ContentItem FromCacheResult(CacheResult cacheResult, string locale = null, string theme = null)
        {
            return new ContentItem
                       {
                           ContentItemType = ContentItemType.Path,
                           AbsoluteContentPath = cacheResult.CachedFilePath,
                           RelativeContentPath = cacheResult.RelativeContentPath,
                           RelativeHashedContentPath = cacheResult.RelativeHashedContentPath,
                           Locale = locale,
                           Theme = theme,
                       };
        }

        /// <summary>Creates a content item from a file.</summary>
        /// <param name="absoluteContentPath">The path.</param>
        /// <param name="relativeContentPath">The original Relative Path.</param>
        /// <param name="relativeHashedContentPath">The alternate Relative Path.</param>
        /// <param name="locale">The locale.</param>
        /// <param name="theme">The theme.</param>
        /// <returns>The <see cref="ContentItem"/>.</returns>
        public static ContentItem FromFile(string absoluteContentPath, string relativeContentPath = null, string relativeHashedContentPath = null, string locale = null, string theme = null)
        {
            return new ContentItem
                         {
                             ContentItemType = ContentItemType.Path,
                             AbsoluteContentPath = absoluteContentPath,
                             RelativeContentPath = relativeContentPath ?? absoluteContentPath,
                             RelativeHashedContentPath = relativeHashedContentPath,
                             Locale = locale,
                             Theme = theme,
                         };
        }

        /// <summary>Creates a content item from another content item and changes the relativeHashedPath.</summary>
        /// <param name="contentItem">The content item.</param>
        /// <param name="relativeContentPath">The relative Content Path.</param>
        /// <param name="relativeHashedContentPath">The alternate Relative Path.</param>
        /// <returns>The <see cref="ContentItem"/>.</returns>
        public static ContentItem FromContentItem(ContentItem contentItem, string relativeContentPath = null, string relativeHashedContentPath = null)
        {
            return new ContentItem
                       {
                           RelativeHashedContentPath = relativeHashedContentPath ?? contentItem.RelativeHashedContentPath,
                           RelativeContentPath = relativeContentPath ?? contentItem.RelativeContentPath,

                           AbsoluteContentPath = contentItem.AbsoluteContentPath,
                           ContentItemType = contentItem.ContentItemType,
                           ContentValue = contentItem.ContentValue,
                           Locale = contentItem.Locale,
                           Theme = contentItem.Theme,
                       };
        }

        /// <summary>The from content.</summary>
        /// <param name="content">The content.</param>
        /// <returns>The <see cref="ContentItem"/>.</returns>
        public static ContentItem FromContent(string content)
        {
            return FromContent(content, null, null, null, null);
        }

        /// <summary>Creates a content item from a string value and a previous contentItem.</summary>
        /// <summary>The from content.</summary>
        /// <param name="content">The content.</param>
        /// <param name="contentItem">The content item.</param>
        /// <param name="locale">The locale.</param>
        /// <param name="theme">The theme.</param>
        /// <returns>The <see cref="ContentItem"/>.</returns>
        public static ContentItem FromContent(string content, ContentItem contentItem, string locale = null, string theme = null)
        {
            return FromContent(
                content,
                contentItem.RelativeContentPath,
                contentItem.RelativeHashedContentPath,
                locale ?? contentItem.Locale,
                theme ?? contentItem.Theme);
        }

        #endregion

        /// <summary>The get content hash.</summary>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="string"/>.</returns>
        internal string GetContentHash(IWebGreaseContext context)
        {
            return this.contentHash ??
                (this.contentHash = ContentItemType == ContentItemType.Value
                    ? context.GetValueHash(this.Content)
                    : context.GetFileHash(this.AbsoluteContentPath));
        }

        /// <summary>The save to alternate.</summary>
        /// <param name="destinationDirectory">The destination directory.</param>
        /// <param name="overwrite">The overwrite.</param>
        internal void WriteToHashedPath(string destinationDirectory, bool overwrite = false)
        {
            this.WriteTo(Path.Combine(destinationDirectory, this.RelativeHashedContentPath), overwrite);
        }

        /// <summary>The save to alternate.</summary>
        /// <param name="destinationDirectory">The destination directory.</param>
        /// <param name="overwrite">The overwrite.</param>
        internal void WriteToContentPath(string destinationDirectory, bool overwrite = false)
        {
            this.WriteTo(Path.Combine(destinationDirectory, this.RelativeContentPath), overwrite);
        }

        /// <summary>Save to disk.</summary>
        /// <param name="fullPath">The full path</param>
        /// <param name="overwrite">The overwrite.</param>
        internal void WriteTo(string fullPath, bool overwrite = false)
        {
            var absolutePath = new FileInfo(fullPath);
            if (!absolutePath.Exists || overwrite)
            {
                if (absolutePath.Directory != null && !absolutePath.Directory.Exists)
                {
                    absolutePath.Directory.Create();
                }

                if (this.ContentItemType == ContentItemType.Path)
                {
                    File.Copy(this.AbsoluteContentPath, absolutePath.FullName, overwrite);
                }
                else
                {
                    File.WriteAllText(absolutePath.FullName, this.Content);
                }
            }
        }
    }
}