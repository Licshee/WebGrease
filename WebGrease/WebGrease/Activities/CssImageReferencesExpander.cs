// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CssImageReferencesExpander.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Manages the hash naming of file outputs
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Activities
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>Manages the hash naming of file outputs</summary>
    internal static class CssImageReferencesExpander
    {
        /// <summary>
        /// Error message for reference not found.
        /// </summary>
        private const string NoImageFoundErrorMessage = "The css file contains an invalid image reference. There is no image found for '{0}'";

        /// <summary>
        /// Initializes static members of the CssImageReferencesExpander class 
        /// </summary>
        static CssImageReferencesExpander()
        {
            HashRegex = new Regex(@"hash(?:\((?<1>[^\)]*))\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            UrlRegex = new Regex(@"url\((?<quote>[""']?)\s*([-\\:/.\w]+\.[\w]+)\s*\k<quote>\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        /// <summary>
        /// Gets the precompiled regular expression to match the hash function for images
        /// </summary>
        internal static Regex HashRegex { get; private set; }

        /// <summary>
        /// Gets the precompiled regular expression to match the url reference for images
        /// </summary>
        internal static Regex UrlRegex { get; private set; }

        /// <summary>
        /// Gets the hash xpath to query from the xml file
        /// </summary>
        internal static string HashXPath { get; private set; }

        /// <summary>Updates the file content with the new</summary>
        /// <param name="renamedFilesLogs">The renamed file log.</param>
        /// <param name="cssFileContent">The css file contents</param>
        /// <returns>The updated css file content</returns>
        public static string UpdateForHashReferences(RenamedFilesLogs renamedFilesLogs, string cssFileContent)
        {
            if (renamedFilesLogs == null || cssFileContent == null)
            {
                return cssFileContent;
            }

            return UpdateFileContentsWithHashMatch(renamedFilesLogs, cssFileContent);
        }

        /// <summary>Updates the fileContent contents after the hash matches</summary>
        /// <param name="renamedFilesLogs">The renamed file log.</param>
        /// <param name="fileContent">The file contents</param>
        /// <returns>The updated css file content</returns>
        private static string UpdateFileContentsWithHashMatch(RenamedFilesLogs renamedFilesLogs, string fileContent)
        {
            // Pass 1 - Regex on the url in css
            var urlExpansion = UrlRegex.Replace(fileContent, match =>
                                                                 {
                                                                     var originalImagePath = match.Groups[1].ToString();
                                                                     var hashedName = renamedFilesLogs.FindHashPath(originalImagePath);
                                                                     return !string.IsNullOrWhiteSpace(hashedName) ? match.Value.Replace(originalImagePath, hashedName) : match.Value;
                                                                 });

            // Pass 2 - Legacy Hash Function
            var hashExpansion = HashRegex.Replace(urlExpansion, match =>
                                                                    {
                                                                        var originalImagePath = match.Groups[1].ToString();
                                                                        var hashedName = renamedFilesLogs.FindHashPath(originalImagePath);
                                                                        if (!string.IsNullOrWhiteSpace(hashedName))
                                                                        {
                                                                            return hashedName;
                                                                        }

                                                                        throw new WorkflowException(string.Format(CultureInfo.CurrentUICulture, NoImageFoundErrorMessage, originalImagePath));
                                                                    });

            return hashExpansion;
        }
    }
}
