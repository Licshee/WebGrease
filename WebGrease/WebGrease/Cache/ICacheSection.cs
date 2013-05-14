// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICacheSection.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System.Collections.Generic;
    using System.IO;

    using WebGrease.Configuration;

    /// <summary>The CacheSection interface.</summary>
    public interface ICacheSection
    {
        /// <summary>Gets the parent.</summary>
        ICacheSection Parent { get; }

        /// <summary>Gets the unique key.</summary>
        string UniqueKey { get; }

        /// <summary>Varys the section by settings.</summary>
        /// <param name="settings">The settings.</param>
        /// <param name="nonpublic">Determins if it should non public members of the object as well.</param>
        void VaryBySettings(object settings, bool nonpublic = false);

        /// <summary>Varys the section by file.</summary>
        /// <param name="contentItem">The result file.</param>
        void VaryByContentItem(ContentItem contentItem);

        /// <summary>Ends the section.</summary>
        void EndSection();

        /// <summary>If all the cache files are valid and all results could be restored from content.</summary>
        /// <returns>If it can be restored from content.</returns>
        bool CanBeRestoredFromCache();

        /// <summary>Adds an end result file from a result file.</summary>
        /// <param name="contentItem">The result file.</param>
        /// <param name="id">The category.</param>
        /// <param name="isEndResult">If the result is an endresult</param>
        void AddResult(ContentItem contentItem, string id, bool isEndResult = false);

        /// <summary>Add a source dependency from a file.</summary>
        /// <param name="file">The file.</param>
        void AddSourceDependency(string file);

        /// <summary>Adds a source dependency from a directory.</summary>
        /// <param name="directory">The directory.</param>
        /// <param name="searchPattern">The search pattern.</param>
        /// <param name="searchOption">The search option.</param>
        void AddSourceDependency(string directory, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly);

        /// <summary>Add a source dependency from an input spec.</summary>
        /// <param name="inputSpec">The input spec.</param>
        void AddSourceDependency(InputSpec inputSpec);

        /// <summary>Stores a graph report file (.dgml visual studio file).</summary>
        void Save();

        /// <summary>Determiones if all the end results are there and valid, and can therefor be skipped for processing.</summary>
        /// <returns>If it can be skipped.</returns>
        bool CanBeSkipped();

        /// <summary>Gets the changed source dependencies recursively.</summary>
        /// <returns>The changed source dependencies.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Not appropriate.")]
        IEnumerable<CacheSourceDependency> GetChangedSourceDependencies();

        /// <summary>Get the invalid cache results.</summary>
        /// <returns>The invalid cache results.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Not appropriate.")]
        IEnumerable<CacheResult> GetInvalidCachedResults();

        /// <summary>Gets the cached content item.</summary>
        /// <param name="fileCategory">The file category.</param>
        /// <returns>The <see cref="ContentItem"/>.</returns>
        ContentItem GetCachedContentItem(string fileCategory);

        /// <summary>Gets the cached content items.</summary>
        /// <param name="fileCategory">The file category.</param>
        /// <param name="endResultOnly">If it should return end results only.</param>
        /// <returns>The <see cref="ContentItem"/>.</returns>
        IEnumerable<ContentItem> GetCachedContentItems(string fileCategory, bool endResultOnly = false);

        /// <summary>Writes a graph report file (.dgml visual studio file).</summary>
        /// <param name="graphReportFilePath">The graph report file path.</param>
        void WriteDependencyGraph(string graphReportFilePath);

        /// <summary>The get cache data.</summary>
        /// <param name="id">The id.</param>
        /// <typeparam name="T">The typeof object</typeparam>
        /// <returns>The <see cref="T"/>.</returns>
        T GetCacheData<T>(string id) where T : new();

        /// <summary>The set cache data.</summary>
        /// <param name="id">The id.</param>
        /// <param name="obj">The data object.</param>
        /// <typeparam name="T">The typeof object</typeparam>
        void SetCacheData<T>(string id, T obj) where T : new();
    }
}