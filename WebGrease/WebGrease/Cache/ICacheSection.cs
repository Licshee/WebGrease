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

        /// <summary>Varys the section by file.</summary>
        /// <param name="absoluteFilePath">The absolute file path.</param>
        void VaryByFile(string absoluteFilePath);

        /// <summary>Varys the section by settings.</summary>
        /// <param name="settings">The settings.</param>
        /// <param name="nonpublic">Determins if it should non public members of the object as well.</param>
        void VaryBySettings(object settings, bool nonpublic = false);

        /// <summary>Ends the section.</summary>
        void EndSection();

        /// <summary>If all the cache files are valid and all results could be restored from content.</summary>
        /// <returns>If it can be restored from content.</returns>
        bool CanBeRestoredFromCache();

        /// <summary>Adds an end result file from a filepath.</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="category">The category.</param>
        void AddEndResultFile(string filePath, string category);

        /// <summary>Adds an end result file from a result file.</summary>
        /// <param name="resultFile">The result file.</param>
        /// <param name="category">The category.</param>
        void AddEndResultFile(ResultFile resultFile, string category);

        /// <summary>Adds a result file.</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="category">The category.</param>
        /// <param name="relativePath">The relative path.</param>
        void AddResultFile(string filePath, string category, string relativePath = null);

        /// <summary>Add result content.</summary>
        /// <param name="content">The content.</param>
        /// <param name="category">The category.</param>
        /// <param name="endResult">If it is an endresult.</param>
        void AddResultContent(string content, string category, bool endResult = false);

        /// <summary>Restore files from cache recursively.</summary>
        /// <param name="category">The category.</param>
        /// <param name="targetPath">The target path.</param>
        /// <param name="overwrite">The overwrite.</param>
        /// <returns>The restored files.</returns>
        IEnumerable<CacheResult> RestoreFiles(string category, string targetPath = null, bool overwrite = true);

        /// <summary>Restores the file from cache recursively.</summary>
        /// <param name="category">The category.</param>
        /// <param name="absolutePath">The absolute path.</param>
        /// <param name="overwrite">If it should overwrit if the file already exists.</param>
        void RestoreFile(string category, string absolutePath, bool overwrite = true);

        /// <summary>Restores / Gets content from cache.</summary>
        /// <param name="category">The category.</param>
        /// <returns>The content from cache.</returns>
        string RestoreContent(string category);

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
        /// <param name="graphReportFilePath">The graph report file path.</param>
        void Store(string graphReportFilePath = null);

        /// <summary>Determiones if all the end results are there and valid, and can therefor be skipped for processing.</summary>
        /// <returns>If it can be skipped.</returns>
        bool CanBeSkipped();

        /// <summary>Gets the changed source dependencies recursively.</summary>
        /// <returns>The changed source dependencies.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Not appropriate.")]
        IEnumerable<CacheSourceDependency> GetChangedSourceDependencies();

        /// <summary>Gets the changed end results recursively.</summary>
        /// <returns>The changed end results.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Not appropriate.")]
        IEnumerable<CacheResult> GetChangedEndResults();

        /// <summary>Get the invalid cache results.</summary>
        /// <returns>The invalid cache results.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Not appropriate.")]
        IEnumerable<CacheResult> GetInvalidCachedResults();

        /// <summary>Gets the cache results for the category recursively.</summary>
        /// <param name="category">The category.</param>
        /// <param name="endResultOnly">If it should return end results only.</param>
        /// <returns>The cache results for the category.</returns>
        IEnumerable<CacheResult> GetResults(string category = null, bool endResultOnly = false);
    }
}