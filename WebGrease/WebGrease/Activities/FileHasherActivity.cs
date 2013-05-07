// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileHasherActivity.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Copies sources to destination, with new file name based on hash of content.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;

    using Common;

    using WebGrease.Extensions;

    /// <summary>Copies sources to destination, with new file name based on hash of content.</summary>
    internal sealed class FileHasherActivity
    {
        /// <summary>The context.</summary>
        private readonly IWebGreaseContext context;

        /// <summary>
        /// Renamed Files Log.
        /// </summary>
        private readonly Dictionary<string, List<string>> renamedFilesLog = new Dictionary<string, List<string>>();

        /// <summary>Initializes a new instance of the <see cref="FileHasherActivity"/> class.</summary>
        /// <param name="context">The web grease context</param>
        internal FileHasherActivity(IWebGreaseContext context)
        {
            this.context = context;
            this.SourceDirectories = new List<string>();
        }

        /// <summary>
        /// Gets the list of directories to copy from.
        /// </summary>
        /// <value>The source directories.</value>
        internal IList<string> SourceDirectories { get; private set; }

        /// <summary>
        /// Gets or sets the Directory to copy to.
        /// </summary>
        /// <value>The destination directory.</value>
        internal string DestinationDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to create an extra level of subdirectories based on hashed file names (e.g. /ab/cd123.css vs. /abcd123.css).
        /// </summary>
        /// <value>
        /// <c>True</c> if [create extra folder level from hashes]; otherwise, <c>false</c>.
        /// </value>
        internal bool CreateExtraDirectoryLevelFromHashes { get; set; }

        /// <summary>
        /// Gets or sets the string prefix to add for output path.
        /// If not blank, this string will be prepended to the paths of the static output location. (e.g. for an image, we may need to add /br before the path).
        /// </summary>
        /// <value>The optional base prefix to add for output path.</value>
        internal string BasePrefixToAddToOutputPath { get; set; }

        /// <summary>Gets or sets the file type.</summary>
        internal FileTypes FileType { get; set; }

        /// <summary>
        /// Gets or sets the BasePrefixToRemoveFromOutputPathInLog.
        /// In the paths in the output log, we'll remove the portion up to and including this stem.
        /// </summary>
        /// <value>The ref static directory stem.</value>
        internal string BasePrefixToRemoveFromOutputPathInLog { get; set; }

        /// <summary>
        /// Gets or sets the BasePrefixToRemoveFromInputPathInLog.
        /// In the paths in the output log, we'll remove the portion up to and including this stem.
        /// </summary>
        /// <value>The ref static directory stem.</value>
        internal string BasePrefixToRemoveFromInputPathInLog { get; set; }

        /// <summary>
        /// Gets or sets the Location and name for the log file to be written to.
        /// </summary>
        /// <value>The log path.</value>
        internal string LogFileName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Preserve Source Directory Structure.
        /// Setting this to true will preserve all subdirectories of SourceDirectories.
        /// </summary>
        /// <value>
        /// <c>True</c> if [preserve source folder structure]; otherwise, <c>false</c>.
        /// </value>
        internal bool ShouldPreserveSourceDirectoryStructure { get; set; }

        /// <summary>
        /// Gets or sets the FileTypeFilter.
        /// Should be a value for DirectoryInfo.GetFiles(), like  '*.css' or '*.js'. Can also be comma separated, like '*.gif,*.jpeg.*.jpg,*.png'
        /// defaults to '*'.
        /// </summary>
        /// <value>The file type filter.</value>
        internal string FileTypeFilter { get; set; }

        /// <summary>Gets or sets the source directory.</summary>
        internal string SourceDirectory { get; set; }

        /// <summary>When overridden in a derived class, executes the task.</summary>
        internal void Execute()
        {
            // Clear out the collection since activities objects may be pooled
            this.renamedFilesLog.Clear();

            this.context.Measure.Start(TimeMeasureNames.FileHasherActivity, this.FileType.ToString());
            try
            {
                if (this.SourceDirectories == null || this.SourceDirectories.Count == 0)
                {
                    // No action for directory not present and no files as input.
                    Trace.TraceInformation("FileHasherActivity - No source directories passed and hence no action taken for the activity.");
                    return;
                }

                // Default the destination directory
                if (string.IsNullOrWhiteSpace(this.DestinationDirectory))
                {
                    this.DestinationDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                }

                // Create an array to filter the file types to retrieve
                var fileTypeFilter = GetFilters(this.FileTypeFilter);

                // Default the prefix to empty
                if (string.IsNullOrWhiteSpace(this.BasePrefixToRemoveFromOutputPathInLog))
                {
                    this.BasePrefixToRemoveFromOutputPathInLog = string.Empty;
                }

                // Ensure BasePrefixToRemoveFromInputPathInLog is not null
                if (string.IsNullOrWhiteSpace(this.BasePrefixToRemoveFromInputPathInLog))
                {
                    this.BasePrefixToRemoveFromInputPathInLog = string.Empty;
                }

                foreach (var sourceDirectory in this.SourceDirectories)
                {
                    if (!Directory.Exists(sourceDirectory))
                    {
                        // No action for directory not present
                        Trace.TraceWarning(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "FileHasherActivity - Could not locate the source directory at {0}",
                                sourceDirectory));
                        continue;
                    }

                    this.Hash(sourceDirectory, this.DestinationDirectory, fileTypeFilter);
                }

                // All done, if we need to save, save the output
                this.Save();
            }
            catch (Exception exception)
            {
                throw new WorkflowException(
                    "FileHasherActivity - Error happened while executing the activity.", exception);
            }
            finally
            {
                this.context.Measure.End(TimeMeasureNames.FileHasherActivity, this.FileType.ToString());
            }
        }

        /// <summary>Hashes the result files.</summary>
        /// <param name="resultFiles">The result files.</param>
        /// <param name="destinationDirectory">The destination.</param>
        /// <returns>The result file after the hash.</returns>
        internal IEnumerable<ResultFile> Hash(IEnumerable<ResultFile> resultFiles, string destinationDirectory = null)
        {
            return resultFiles.Select(rf => this.Hash(rf, destinationDirectory ?? this.DestinationDirectory));
        }

        /// <summary>Hash the result file.</summary>
        /// <param name="resultFile">The source file info.</param>
        /// <param name="destinationDirectory">The destination.</param>
        /// <returns>The result file after the hash.</returns>
        internal ResultFile Hash(ResultFile resultFile, string destinationDirectory = null)
        {
            destinationDirectory = destinationDirectory ?? this.DestinationDirectory;
            var sourceFileInfo = new FileInfo(resultFile.OriginalPath ?? resultFile.Path);

            return resultFile.ResultContentType == ResultContentType.Disk
                       ? this.Hash(sourceFileInfo, destinationDirectory)
                       : this.Hash(
                           sourceFileInfo,
                           this.context.GetContentHash(resultFile.Content),
                           destinationDirectory,
                           destinationFilePath => File.WriteAllText(destinationFilePath, resultFile.Content, resultFile.Encoding));
        }

        /// <summary>Hash the file.</summary>
        /// <param name="sourceFileInfo">The source file info.</param>
        /// <param name="destinationDirectory">The destination.</param>
        /// <returns>The result file after the hash.</returns>
        internal ResultFile Hash(FileInfo sourceFileInfo, string destinationDirectory = null)
        {
            return this.Hash(
                    sourceFileInfo, 
                    this.context.GetFileHash(sourceFileInfo.FullName), 
                    destinationDirectory ?? this.DestinationDirectory, 
                    destinationFilePath => sourceFileInfo.CopyTo(destinationFilePath));
        }

        /// <summary>Saves the log.</summary>
        /// <param name="append">If the save should append or overwrite.</param>
        internal void Save(bool append = true)
        {
            this.WriteLog(this.BasePrefixToRemoveFromInputPathInLog, append);
        }

        /// <summary>Appends to the work log from cache results.</summary>
        /// <param name="cacheResults">The cache results.</param>
        internal void AppendToWorkLog(IEnumerable<CacheResult> cacheResults)
        {
            foreach (var cacheResult in cacheResults)
            {
                var fileBeforeHashing = !string.IsNullOrWhiteSpace(cacheResult.OriginalRelativePath) 
                    ? Path.Combine(this.SourceDirectory, cacheResult.OriginalRelativePath) 
                    : cacheResult.AbsolutePath;

                this.AppendToWorkLog(cacheResult, fileBeforeHashing);
            }
        }

        /// <summary>Appends to the work log from a cache result.</summary>
        /// <param name="cacheResult">The cache result.</param>
        /// <param name="fileBeforeHashing">The file name before it was hashed</param>
        internal void AppendToWorkLog(CacheResult cacheResult, string fileBeforeHashing)
        {
            this.AppendToWorkLog(fileBeforeHashing, Path.Combine(this.context.Configuration.DestinationDirectory, cacheResult.RelativePath));
        }
        
        /// <summary>Gets filters for use with DirectoryInfo.GetFiles().</summary>
        /// <param name="filterType">A '*.css' or camma-separated like '*.gif,*.jpeg'.</param>
        /// <returns>A string array with appropriate file filters (e.g. [*.css] or [*.gif,*.jpeg,...]).</returns>
        private static IEnumerable<string> GetFilters(string filterType)
        {
            return string.IsNullOrWhiteSpace(filterType) ? new[] { "*" } : filterType.Split(Strings.FileFilterSeparator, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>Used for getting the best path section to put in the log file for matching purposes.</summary>
        /// <param name="fullPath">Full path to file.</param>
        /// <param name="stem">The stem and the portion before the stem will be removed.</param>
        /// <returns>Portion of the path after the stem, with back slashes changed to slashes.</returns>
        private static string GetPathAfterStem(string fullPath, string stem)
        {
            // stem should be enough "\bin\Debug\RefStatic" or "\bin\Release\RefStatic"
            var portionFromRefStaticRoot = fullPath.Substring(fullPath.IndexOf(stem, StringComparison.OrdinalIgnoreCase) + stem.Length);

            // fix the separators. e.g. \i\Slot1_Images\image1.gif becomes /i/Slot1_Images/image1.gif to make matching in css later easier
            var oldSep = Path.DirectorySeparatorChar;
            portionFromRefStaticRoot = portionFromRefStaticRoot.Replace(oldSep, Path.AltDirectorySeparatorChar);

            return portionFromRefStaticRoot;
        }

        /// <summary>Copies and hashes the Directory.</summary>
        /// <param name="sourceDirectory">Path to source directory.</param>
        /// <param name="destinationDirectory">Path to destination directory.</param>
        /// <param name="filters">Array of file filters to apply.</param>
        /// <returns>The result files after the hash.</returns>
        private IEnumerable<ResultFile> Hash(string sourceDirectory, string destinationDirectory, IEnumerable<string> filters)
        {
            var results = new List<ResultFile>();

            // Create the directory if does not exist.
            Directory.CreateDirectory(destinationDirectory);

            var sourceDirectoryInfo = new DirectoryInfo(sourceDirectory);

            // Need to do this for only the file type(s) desired
            results.AddRange(
                filters.SelectMany(filter => 
                    sourceDirectoryInfo
                    .EnumerateFiles(filter)
                        .Select(sourceFileInfo => this.Hash(sourceFileInfo, destinationDirectory))));

            // recurse through subdirs, either keeping the source dir structure in the destination or flattening it
            foreach (var subDirectoryInfo in sourceDirectoryInfo.GetDirectories())
            {
                var subDestinationDirectory = 
                    this.ShouldPreserveSourceDirectoryStructure 
                    ? Path.Combine(destinationDirectory, subDirectoryInfo.Name) 
                    : destinationDirectory;

                results.AddRange(
                    this.Hash(subDirectoryInfo.FullName, subDestinationDirectory, filters));
            }

            return results;
        }

        /// <summary>Hash the file.</summary>
        /// <param name="sourceFileInfo">The source file info.</param>
        /// <param name="hash">The MD5 hash</param>
        /// <param name="destinationDirectory">The destination.</param>
        /// <param name="storeAction">The store action.</param>
        /// <returns>The result file after the hash.</returns>
        private ResultFile Hash(FileInfo sourceFileInfo, string hash, string destinationDirectory, Action<string> storeAction)
        {
            var logSourceEntry = sourceFileInfo.FullName;
            var hashedFileName = hash + sourceFileInfo.Extension;
            var destinationFilePath = this.GetDestinationFilePath(destinationDirectory, hashedFileName);

            // Do not overwrite if exists, since filename is md5 hash, filename changes if content changes.
            if (!File.Exists(destinationFilePath))
            {
                storeAction(destinationFilePath);
            }

            // Append to the log
            this.AppendToWorkLog(logSourceEntry, destinationFilePath);

            // Return it as a result file.
            return ResultFile.FromFile(destinationFilePath, this.FileType, sourceFileInfo.FullName, this.SourceDirectory);
        }

        /// <summary>Gets the destination file path for a hashed file name: /12/34567xxxxx.png.</summary>
        /// <param name="destination">The destination path.</param>
        /// <param name="hashedFileName">The hashed file name.</param>
        /// <returns>The destination file path.</returns>
        private string GetDestinationFilePath(string destination, string hashedFileName)
        {
            string destinationFilePath;

            if (this.CreateExtraDirectoryLevelFromHashes)
            {
                // Use the first 2 chars for a directory name, the last chars for the file name
                destinationFilePath = Path.Combine(destination, hashedFileName.Substring(0, 2));

                // This will be the 2 char subdir, and may not exist yet
                if (!Directory.Exists(destinationFilePath))
                {
                    Directory.CreateDirectory(destinationFilePath);
                }

                // Now get the file 
                destinationFilePath = Path.Combine(destinationFilePath, hashedFileName.Remove(0, 2));
            }
            else
            {
                destinationFilePath = Path.Combine(destination, hashedFileName);
            }

            return destinationFilePath;
        }

        /// <summary>Simple logger for generating report of work done.</summary>
        /// <param name="fileBeforeHashing">Path plus file name before hash renaming.</param>
        /// <param name="fileAfterHashing">Path plus file name after hash renaming.</param>
        /// <param name="skipIfExists">skips the add if it already exists.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Need lowercase")]
        private void AppendToWorkLog(string fileBeforeHashing, string fileAfterHashing, bool skipIfExists = false)
        {
            if (!Path.IsPathRooted(fileBeforeHashing))
            {
                throw new BuildWorkflowException("fileBeforeHashing has not an absolute path: {0}".InvariantFormat(fileBeforeHashing));
            }

            if (!Path.IsPathRooted(fileAfterHashing))
            {
                throw new BuildWorkflowException("fileAfterHashing has not an absolute path: {0}".InvariantFormat(fileAfterHashing));
            }

            // Normalize to make sure they match, this is quicker then doing a ignore case equal check.
            fileAfterHashing = fileAfterHashing.ToLowerInvariant();
            fileBeforeHashing = fileBeforeHashing.ToLowerInvariant();

            if (!this.renamedFilesLog.ContainsKey(fileAfterHashing))
            {
                // add a new key
                this.renamedFilesLog.Add(fileAfterHashing, new List<string>());
            }

            var existingRenames = this.renamedFilesLog.Where(rfl => rfl.Value.Contains(fileBeforeHashing) && !rfl.Key.Equals(fileAfterHashing)).ToArray();
            if (existingRenames.Any())
            {
                if (skipIfExists)
                {
                    if (File.Exists(fileAfterHashing))
                    {
                        File.Delete(fileAfterHashing);
                        var directoryName = Path.GetDirectoryName(fileAfterHashing);
                        if (!Directory.EnumerateFiles(directoryName).Any())
                        {
                            Directory.Delete(directoryName);
                        }
                    }

                    return;
                }

                throw new BuildWorkflowException(
                    "The renamed filename already has a rename to a different file: \r\nBeforehashing:{0} \r\nNewAfterHashing:{1} ExistingAfterhashing:{2}"
                        .InvariantFormat(fileBeforeHashing, fileAfterHashing, string.Join(",", existingRenames.Select(e => e.Key))));
            }

            if (!this.renamedFilesLog[fileAfterHashing].Contains(fileBeforeHashing))
            {
                this.renamedFilesLog[fileAfterHashing].Add(fileBeforeHashing);
            }
        }

        /// <summary>Writes a log in an xml file showing which files were copied, with old and new names in it.</summary>
        /// <param name="sourceDirectory">The source directory</param>
        /// <param name="appendToLog">If we append or overwrite</param>
        private void WriteLog(string sourceDirectory, bool appendToLog = true)
        {
            if (string.IsNullOrWhiteSpace(this.LogFileName))
            {
                return;
            }

            if (appendToLog && File.Exists(this.LogFileName))
            {
                this.Load(this.LogFileName);
            }

            var stringBuilder = new StringBuilder();
            var settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true };

            using (var writer = XmlWriter.Create(stringBuilder, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("RenamedFiles");

                // we still want a log file even if nothing was done to it, just no Flie nodes in it
                if (this.renamedFilesLog == null || this.renamedFilesLog.Keys.Count < 1)
                {
                    writer.WriteComment(ResourceStrings.NoFilesProcessed);
                }
                else
                {
                    foreach (var key in this.renamedFilesLog.Keys.OrderBy(f => f))
                    {
                        writer.WriteStartElement("File");
                        writer.WriteStartElement("Output");
                        var outputPath = GetPathAfterStem(key, this.BasePrefixToRemoveFromOutputPathInLog);

                        // if desired, add a base to the path
                        if (!string.IsNullOrWhiteSpace(this.BasePrefixToAddToOutputPath))
                        {
                            outputPath = this.BasePrefixToAddToOutputPath + outputPath;
                        }

                        writer.WriteValue(outputPath);
                        writer.WriteEndElement();
                        foreach (var oldName in this.renamedFilesLog[key].OrderBy(r => r))
                        {
                            writer.WriteStartElement("Input");
                            var pathAfterStem = GetPathAfterStem(oldName, sourceDirectory);
                            writer.WriteValue(pathAfterStem);
                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
            }

            // write the log value out to a file
            FileHelper.WriteFile(this.LogFileName, stringBuilder.ToString(), Encoding.UTF8);
        }

        /// <summary>Load the log from disk.</summary>
        /// <param name="logFileName">The log file name.</param>
        private void Load(string logFileName)
        {
            var doc = XDocument.Load(logFileName);
            var files = doc.Elements("RenamedFiles").Elements("File");
            foreach (var fileElement in files)
            {
                var outputPath = fileElement.Elements("Output").Select(e => (string)e).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    if (!string.IsNullOrWhiteSpace(this.BasePrefixToAddToOutputPath)
                        && outputPath.StartsWith(this.BasePrefixToAddToOutputPath, StringComparison.OrdinalIgnoreCase))
                    {
                        outputPath = outputPath.Substring(this.BasePrefixToAddToOutputPath.Length);
                    }

                    if (!string.IsNullOrWhiteSpace(this.BasePrefixToRemoveFromOutputPathInLog))
                    {
                        outputPath = Path.Combine(this.BasePrefixToRemoveFromOutputPathInLog, outputPath.NormalizeUrl());
                    }

                    if (File.Exists(outputPath))
                    {
                        var inputs = fileElement.Elements("Input").Select(e => (string)e);
                        foreach (var input in inputs)
                        {
                            var inputPath = (!string.IsNullOrWhiteSpace(this.BasePrefixToRemoveFromInputPathInLog))
                                                ? Path.Combine(this.BasePrefixToRemoveFromInputPathInLog, input.NormalizeUrl())
                                                : input;

                            this.AppendToWorkLog(inputPath, outputPath, true);
                        }
                    }
                }
            }
        }
    }
}
