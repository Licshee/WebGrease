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
        DateTime SessionStartTime { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>The clean cache.</summary>
        void CleanCache();

        /// <summary>The clean destination.</summary>
        void CleanDestination();

        /// <summary>The clean tools temp.</summary>
        void CleanToolsTemp();

        /// <summary>Gets the available files.</summary>
        /// <param name="rootDirectory">The root directory.</param>
        /// <param name="directories">The directories.</param>
        /// <param name="extensions">The extensions.</param>
        /// <param name="fileType">The file type.</param>
        /// <returns>The available files</returns>
        IEnumerable<ResultFile> GetAvailableFiles(string rootDirectory, IList<string> directories, IList<string> extensions, FileTypes fileType);

        /// <summary>The get content hash.</summary>
        /// <param name="content">The content.</param>
        /// <returns>The <see cref="string"/>.</returns>
        string GetContentHash(string content);

        /// <summary>The get file hash.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The <see cref="string"/>.</returns>
        string GetFileHash(string filePath);

        /// <summary>The make relative.</summary>
        /// <param name="absolutePath">The absolute path.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <returns>The <see cref="string"/>.</returns>
        string MakeRelative(string absolutePath, string relativePath = null);

        /// <summary>The touch.</summary>
        /// <param name="filePath">The file path.</param>
        void Touch(string filePath);

        #endregion
    }
}