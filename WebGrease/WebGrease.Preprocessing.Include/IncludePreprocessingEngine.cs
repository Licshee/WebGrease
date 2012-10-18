using System;

namespace WebGrease.Preprocessing.Include
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Text.RegularExpressions;

    using WebGrease.Activities;
    using WebGrease.Configuration;

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
        private const string IncludeMatchPattern = @"wgInclude\s*\(\s*(?<quote>[""'])(?<fileOrPath>.*?)\k<quote>(\s*,\s*(?<quote2>[""'])(?<searchPattern>.*?)\k<quote2>)?\s*\)\s*;?";

        private static readonly Regex IncludeRegex = new Regex(IncludeMatchPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// The name of the prepriocesor, used for matching the the engines attribute in the config.
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
        /// <param name="fullFileName">The full filename</param>
        /// <param name="preprocessConfig">The pre processing config</param>
        /// <returns>True if it can process it, otherwise false.</returns>
        public bool CanProcess(string fullFileName, PreprocessingConfig preprocessConfig = null)
        {
            return true;
        }

        /// <summary>
        /// Processed the contents of the file and returns the processed content.
        /// returns null if anything went wrong, and reports any errors through the lof delegates.
        /// </summary>
        /// <param name="fileContent">The content of the file.</param>
        /// <param name="fullFileName">The full filename.</param>
        /// <param name="preprocessConfig">The pre processing configuration</param>
        /// <param name="logInformation">The log information delegate.</param>
        /// <param name="logError">The log error delegate.</param>
        /// <param name="logExtendedError">The log extended error delegate.</param>
        /// <returns>The processed contents or null of an error occurred.</returns>
        public string Process(string fileContent, string fullFileName, PreprocessingConfig preprocessConfig, Action<string> logInformation = null, LogError logError = null, LogExtendedError logExtendedError = null)
        {
            var fi = new FileInfo(fullFileName);
            var workingFolder = fi.DirectoryName;
            if (!string.IsNullOrWhiteSpace(fileContent))
            {
                fileContent = IncludeRegex.Replace(fileContent, (match) => ReplaceInputs(match, workingFolder));
            }

            return fileContent;
        }

        /// <summary>
        /// The method called from the regex replace to replavce the matched wgInclude() statements.
        /// </summary>
        /// <param name="match">The regex match</param>
        /// <param name="workingFolder">The working folder from which to determine relative path's in the include.</param>
        /// <returns>The contents of the file to replace, with a /* WGINCLUDE [fullFilePath] */ header on top.</returns>
        private static string ReplaceInputs(Match match, string workingFolder)
        {
            var fileOrPath = Path.Combine(workingFolder, match.Groups["fileOrPath"].Value.Trim());
            var filesToInclude = new List<string>();
            if (Directory.Exists(fileOrPath))
            {
                var searchPattern = match.Groups["searchPattern"].Value.Trim();
                filesToInclude.AddRange(
                    (!String.IsNullOrWhiteSpace(searchPattern))
                    ? Directory.GetFiles(fileOrPath, searchPattern)
                    : Directory.GetFiles(fileOrPath));
            }
            else if (File.Exists(fileOrPath))
            {
                filesToInclude.Add(fileOrPath);
            }

            var result = string.Empty;
            foreach (var file in filesToInclude)
            {
                result += "/* WGINCLUDE: {0} */\r\n".InvariantFormat(file);
                result += File.ReadAllText(file) +"\r\n";
            }
            return result;
        }
    }
}
