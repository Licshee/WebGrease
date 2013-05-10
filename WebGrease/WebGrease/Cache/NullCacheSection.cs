// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullCacheSection.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    using WebGrease.Configuration;

    /// <summary>The null cache section.</summary>
    public class NullCacheSection : ICacheSection
    {
        #region Static Fields

        /// <summary>The empty cache results.</summary>
        internal static readonly IEnumerable<CacheResult> EmptyCacheResults = new CacheResult[] { };

        /// <summary>The empty cache results.</summary>
        private static readonly IEnumerable<ContentItem> EmptyContentItems = new ContentItem[] { };

        /// <summary>The empty source dependencies.</summary>
        private static readonly IEnumerable<CacheSourceDependency> EmptySourceDependencies = new CacheSourceDependency[] { };

        #endregion

        #region Public Properties

        /// <summary>Gets the parent.</summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Interface for Null object")]
        public ICacheSection Parent
        {
            get
            {
                return NullCacheManager.EmptyCacheSection;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>Adds an end result file from a result file.</summary>
        /// <param name="contentItem">The result file.</param>
        /// <param name="id">The category.</param>
        /// <param name="isEndResult">If it is an endresult.</param>
        public void AddResult(ContentItem contentItem, string id, bool isEndResult)
        {
        }

        /// <summary>Add a source dependency from a file.</summary>
        /// <param name="file">The file.</param>
        public void AddSourceDependency(string file)
        {
        }

        /// <summary>Adds a source dependency from a directory.</summary>
        /// <param name="directory">The directory.</param>
        /// <param name="searchPattern">The search pattern.</param>
        /// <param name="searchOption">The search option.</param>
        public void AddSourceDependency(string directory, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
        }

        /// <summary>Add a source dependency from an input spec.</summary>
        /// <param name="inputSpec">The input spec.</param>
        public void AddSourceDependency(InputSpec inputSpec)
        {
        }

        /// <summary>If all the cache files are valid and all results could be restored from content.</summary>
        /// <returns>If it can be restored from content.</returns>
        public bool CanBeRestoredFromCache()
        {
            return false;
        }

        /// <summary>Determiones if all the end results are there and valid, and can therefor be skipped for processing.</summary>
        /// <returns>If it can be skipped.</returns>
        public bool CanBeSkipped()
        {
            return false;
        }

        /// <summary>Ends the section.</summary>
        public void EndSection()
        {
        }

        /// <summary>Gets the changed end results recursively.</summary>
        /// <returns>The changed end results.</returns>
        public IEnumerable<CacheResult> GetChangedEndResults()
        {
            return EmptyCacheResults;
        }

        /// <summary>Gets the changed source dependencies recursively.</summary>
        /// <returns>The changed source dependencies.</returns>
        public IEnumerable<CacheSourceDependency> GetChangedSourceDependencies()
        {
            return EmptySourceDependencies;
        }

        /// <summary>Get the invalid cache results.</summary>
        /// <returns>The invalid cache results.</returns>
        public IEnumerable<CacheResult> GetInvalidCachedResults()
        {
            return EmptyCacheResults;
        }

        /// <summary>Gets the cached content item.</summary>
        /// <param name="fileCategory">The file category.</param>
        /// <returns>The <see cref="ContentItem"/>.</returns>
        public ContentItem GetCachedContentItem(string fileCategory)
        {
            return null;
        }

        /// <summary>Gets the cached content items.</summary>
        /// <param name="fileCategory">The file category.</param>
        /// <param name="endResultOnly">If it should return end results only.</param>
        /// <returns>The <see cref="ContentItem"/>.</returns>
        public IEnumerable<ContentItem> GetCachedContentItems(string fileCategory, bool endResultOnly = false)
        {
            return EmptyContentItems;
        }

        /// <summary>Writes a graph report file (.dgml visual studio file).</summary>
        /// <param name="graphReportFilePath">The graph report file path.</param>
        public void WriteDependencyGraph(string graphReportFilePath)
        {
        }

        /// <summary>Gets the source dependencies recursively.</summary>
        /// <returns>The source dependencies.</returns>
        public IEnumerable<CacheSourceDependency> GetSourceDependencies()
        {
            return EmptySourceDependencies;
        }

        /// <summary>Varys the section by file.</summary>
        /// <param name="contentItem">The result file.</param>
        public void VaryByContentItem(ContentItem contentItem)
        {
        }

        /// <summary>Stores a graph report file (.dgml visual studio file).</summary>
        public void Save()
        {
        }

        /// <summary>Varys the section by settings.</summary>
        /// <param name="settings">The settings.</param>
        /// <param name="nonpublic">Determins if it should non public members of the object as well.</param>
        public void VaryBySettings(object settings, bool nonpublic = false)
        {
        }

        #endregion
    }
}