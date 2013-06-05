// ----------------------------------------------------------------------------------------------------
// <copyright file="IWebGreaseContext.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// <summary>
//   The interface for the web grease context, can be used separately to mock and/or replace the context in tests.
//   See WebGreaseContext for more info on the functionality provides by implementations.
// </summary>
// ----------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System;
    using System.Collections.Generic;

    using WebGrease.Activities;
    using WebGrease.Configuration;
    using WebGrease.Preprocessing;

    /// <summary>
    /// The interface for the web grease context, can be used separately to mock and/or replace the context in tests.
    /// See WebGreaseContext for more info on the functionality provides by implementations.
    /// </summary>
    public interface IWebGreaseContext
    {
        #region Public Properties

        /// <summary>Gets the cache manager.</summary>
        ICacheManager Cache { get; }

        /// <summary>Gets the configuration.</summary>
        WebGreaseConfiguration Configuration { get; }

        /// <summary>Gets the log.</summary>
        LogManager Log { get; }

        /// <summary>Gets the time measure object.</summary>
        ITimeMeasure Measure { get; }

        /// <summary>Gets the preprocessing.</summary>
        PreprocessingManager Preprocessing { get; }

        /// <summary>Gets the session start time.</summary>
        DateTimeOffset SessionStartTime { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>The clean cache.</summary>
        /// <param name="logManager">The log manager</param>
        void CleanCache(LogManager logManager = null);

        /// <summary>The clean destination.</summary>
        void CleanDestination();

        /// <summary>Gets the available files.</summary>
        /// <param name="rootDirectory">The root directory.</param>
        /// <param name="directories">The directories.</param>
        /// <param name="extensions">The extensions.</param>
        /// <param name="fileType">The file type.</param>
        /// <returns>The available files</returns>
        IDictionary<string, string> GetAvailableFiles(string rootDirectory, IEnumerable<string> directories, IEnumerable<string> extensions, FileTypes fileType);

        /// <summary>Gets the unique hash for the provided string value.</summary>
        /// <param name="value">The string value.</param>
        /// <returns>The hash for the string value..</returns>
        string GetValueHash(string value);

        /// <summary>The get content hash.</summary>
        /// <param name="contentItem">The content.</param>
        /// <returns>The <see cref="string"/>.</returns>
        string GetContentItemHash(ContentItem contentItem);

        /// <summary>The get file hash.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The <see cref="string"/>.</returns>
        string GetFileHash(string filePath);

        /// <summary>The make relative.</summary>
        /// <param name="absolutePath">The absolute path.</param>
        /// <returns>The <see cref="string"/>.</returns>
        string MakeRelativeToApplicationRoot(string absolutePath);

        /// <summary>Make a path absolute to source directory.</summary>
        /// <param name="relativePath">The relative path.</param>
        /// <returns>The absolute path.</returns>
        string GetWorkingSourceDirectory(string relativePath);

        /// <summary>The touch.</summary>
        /// <param name="filePath">The file path.</param>
        void Touch(string filePath);

        #endregion

        /// <summary>The section.</summary>
        /// <param name="idParts">The id parts.</param>
        /// <returns>The <see cref="IWebGreaseSection"/>.</returns>
        IWebGreaseSection SectionedAction(params string[] idParts);

        /// <summary>The section.</summary>
        /// <param name="idParts">The id parts.</param>
        /// <returns>The <see cref="IWebGreaseSection"/>.</returns>
        IWebGreaseSection SectionedActionGroup(params string[] idParts);

        /// <summary>Returns if the code should temporary ignore and do no heavy processing for this execution run for this fileset and/or content item.</summary>
        /// <param name="fileSet">The file set.</param>
        /// <param name="contentItem">The content item.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        bool TemporaryIgnore(IFileSet fileSet, ContentItem contentItem);

        /// <summary>Returns if the code should temporary ignore and do no heavy processing for this execution run for this content pivot.</summary>
        /// <param name="contentPivot">The content Pivot.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        bool TemporaryIgnore(ContentPivot contentPivot);

        /// <summary>Ensures the file exists so it can be reported with an error.</summary>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="sourceContentItem">The input file.</param>
        /// <returns>The error file path.</returns>
        string EnsureErrorFileOnDisk(string sourceFile, ContentItem sourceContentItem);
    }
}