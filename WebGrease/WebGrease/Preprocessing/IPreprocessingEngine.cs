// ----------------------------------------------------------------------------------------------------
// <copyright file="IPreprocessingEngine.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease.Preprocessing
{
    using System;

    using WebGrease.Configuration;

    /// <summary>
    /// The IPreprocessingEngine describes a preprocessing plugin for webgrease.
    /// When implemented and [Export} applied, a class will be loaded through MEF and called when processing files in WebGrease.
    /// </summary>
    public interface IPreprocessingEngine
    {
        #region Public Properties

        /// <summary>The name of this preprocessor (Name has to be set in a configuration for the preprocessor to be used)</summary>
        string Name { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// This method will be called to check if the processor believes it can handle the file based on the filename.
        /// </summary>
        /// <param name="fullFileName">The full path to the file.</param>
        /// <param name="preprocessConfig">The configuration</param>
        /// <returns>If it thinks it can process it.</returns>
        bool CanProcess(string fullFileName, PreprocessingConfig preprocessConfig = null);

        /// <summary>
        /// The main method for Preprocessing, this is where the preprocessor gets passed the full content, parses it and returns the parsed content.
        /// </summary>
        /// <param name="fileContent">Content of the file to parse.</param>
        /// <param name="fullFileName">The full filename</param>
        /// <param name="preprocessConfig">The configuration.</param>
        /// <returns>The processed content.</returns>
        string Process(string fileContent, string fullFileName, PreprocessingConfig preprocessConfig);

        #endregion

        /// <summary>The initialize.</summary>
        /// <param name="webGreaseContext">The context.</param>
        void Initialize(IWebGreaseContext webGreaseContext);
    }
}