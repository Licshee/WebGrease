// ----------------------------------------------------------------------------------------------------
// <copyright file="IncludePreprocessingEngine.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// <summary>
//   This preprocessing engine, if enabled through config (<Settings><Preprocessing Engine="include" /></Settings>)
//   Will read the file's content and replace any wgInclude("[fileOrPath]","?[searchPattern]") with the contents of what is in the filePath variable. ([searchPattern] is Optional)
//   If the [fileOrPath] variable is a file it will include the file and replace the wgInclude statement with the contents of the file.
//   If it is a path it will either use the optional [searchPattern] or take all files in the folder.
//   it does this non-recursively, only 1 level is include.
//   Files included by wgInclude are not processed for more wgInclude's, it only works for files directly called by WebGrease.
//   it will add /* WGINCLUDE: {filename} */ in the output above the content's of the file.
//   If the file or directory does not exist it will just silently remove the wgInclude.
// </summary>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease.Preprocessing.Include
{
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Text.RegularExpressions;

    using WebGrease.Configuration;
    using WebGrease.Extensions;

    /// <summary>
    /// This preprocessing engine, if enabled through config (<Settings><Preprocessing Engine="include"/></Settings>)
    /// Will read the file's content and replace any wgInclude("[fileOrPath]","?[searchPattern]") with the contents of what is in the filePath variable. ([searchPattern] is Optional)
    /// If the [fileOrPath] variable is a file it will include the file and replace the wgInclude statement with the contents of the file.
    /// If it is a path it will either use the optional [searchPattern] or take all files in the folder.
    /// it does this non-recursively, only 1 level is include.
    /// Files included by wgInclude are not processed for more wgInclude's, it only works for files directly called by WebGrease.
    /// it will add /* WGINCLUDE: {filename} */ in the output above the content's of the file.
    /// If the file or directory does not exist it will just silently remove the wgInclude.
    /// </summary>
    [Export(typeof(IPreprocessingEngine))]
    public class IncludePreprocessingEngine : IPreprocessingEngine
    {
        /// <summary>The include match regex pattern.</summary>
        private const string IncludeMatchPattern = @"wgInclude\s*\(\s*(?<quote>[""'])(?<fileOrPath>.*?)\k<quote>(\s*,\s*(?<quote2>[""'])(?<searchPattern>.*?)\k<quote2>)?\s*\)\s*;?";

        /// <summary>The include regex.</summary>
        private static readonly Regex IncludeRegex = new Regex(IncludeMatchPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>The context.</summary>
        private IWebGreaseContext context;

        /// <summary>
        /// Gets the name of the prepriocesor, used for matching the the engines attribute in the config.
        /// The includeengine uses : "include"
        /// </summary>
        public string Name
        {
            get
            {
                return "include";
            }
        }

        /// <summary>
        /// Determines if the processor can parse the filetype, always true for this engine.
        /// </summary>
        /// <param name="contentItem">The full filename</param>
        /// <param name="preprocessConfig">The pre processing config</param>
        /// <returns>True if it can process it, otherwise false.</returns>
        public bool CanProcess(ContentItem contentItem, PreprocessingConfig preprocessConfig = null)
        {
            return true;
        }

        /// <summary>The initialize.</summary>
        /// <param name="webGreaseContext">The context.</param>
        public void SetContext(IWebGreaseContext webGreaseContext)
        {
            this.context = webGreaseContext;
        }

        /// <summary>
        /// Processed the contents of the file and returns the processed content.
        /// returns null if anything went wrong, and reports any errors through the lot delegates.
        /// </summary>
        /// <param name="contentItem">The content of the file.</param>
        /// <param name="preprocessingConfig">The pre processing configuration</param>
        /// <returns>The processed contents or null of an error occurred.</returns>
        public ContentItem Process(ContentItem contentItem, PreprocessingConfig preprocessingConfig)
        {
            this.context.Measure.Start(SectionIdParts.Preprocessing, SectionIdParts.Process, "WgInclude");
            var wgincludeCacheImportsSection = this.context.Cache.BeginSection("wginclude", contentItem, preprocessingConfig);
            try
            {
                var workingFolder = this.context.GetWorkingSourceDirectory(contentItem.RelativeContentPath);
                var content = contentItem.Content;
                if (string.IsNullOrWhiteSpace(content))
                {
                    return contentItem;
                }

                content = IncludeRegex.Replace(content, match => ReplaceInputs(match, workingFolder, wgincludeCacheImportsSection));
                wgincludeCacheImportsSection.Save();

                return ContentItem.FromContent(content, contentItem);
            }
            finally
            {
                wgincludeCacheImportsSection.EndSection();
                this.context.Measure.End(SectionIdParts.Preprocessing, SectionIdParts.Process, "WgInclude");
            }
        }

        /// <summary>The method called from the regex replace to replace the matched wgInclude() statements.</summary>
        /// <param name="match">The regex match</param>
        /// <param name="workingFolder">The working folder from which to determine relative path's in the include.</param>
        /// <param name="cacheSection">The cache Section.</param>
        /// <returns>The contents of the file to replace, with a /* WGINCLUDE [fullFilePath] */ header on top.</returns>
        private static string ReplaceInputs(Match match, string workingFolder, ICacheSection cacheSection)
        {
            var fileOrPath = Path.Combine(workingFolder, match.Groups["fileOrPath"].Value.Trim());
            var inputSpec = new InputSpec { IsOptional = true, Path = fileOrPath };
            if (Directory.Exists(fileOrPath))
            {
                inputSpec.SearchPattern = match.Groups["searchPattern"].Value.Trim();
            }

            cacheSection.AddSourceDependency(inputSpec);

            var result = string.Empty;
            foreach (var file in inputSpec.GetFiles())
            {
                result += "/* WGINCLUDE: {0} */\r\n".InvariantFormat(file);
                result += File.ReadAllText(file) + "\r\n";
            }

            return result;
        }
    }
}
