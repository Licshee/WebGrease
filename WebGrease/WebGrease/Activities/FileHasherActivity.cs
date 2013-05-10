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
        internal string DestinationDirectory { private get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to create an extra level of subdirectories based on hashed file names (e.g. /ab/cd123.css vs. /abcd123.css).
        /// </summary>
        /// <value>
        /// <c>True</c> if [create extra folder level from hashes]; otherwise, <c>false</c>.
        /// </value>
        internal bool CreateExtraDirectoryLevelFromHashes { private get; set; }

        /// <summary>
        /// Gets or sets the string prefix to add for output path.
        /// If not blank, this string will be prepended to the paths of the static output location. (e.g. for an image, we may need to add /br before the path).
        /// </summary>
        /// <value>The optional base prefix to add for output path.</value>
        internal string BasePrefixToAddToOutputPath { get; set; }

        /// <summary>Gets or sets the file type.</summary>
        internal FileTypes FileType { private get; set; }

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
        internal bool ShouldPreserveSourceDirectoryStructure { private get; set; }

        /// <summary>
        /// Gets or sets the FileTypeFilter.
        /// Should be a value for DirectoryInfo.GetFiles(), like  '*.css' or '*.js'. Can also be comma separated, like '*.gif,*.jpeg.*.jpg,*.png'
        /// defaults to '*'.
        /// </summary>
        /// <value>The file type filter.</value>
        internal string FileTypeFilter { private get; set; }

        /// <summary>When overridden in a derived class, executes the task.</summary>
        internal void Execute()
        {
            // Clear out the collection since activities objects may be pooled
            this.renamedFilesLog.Clear();

            this.context.Measure.Start(SectionIdParts.FileHasherActivity, this.FileType.ToString());
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
                this.context.Measure.End(SectionIdParts.FileHasherActivity, this.FileType.ToString());
            }
        }

        /// <summary>Hashes the result files.</summary>
        /// <param name="contentItems">The result files.</param>
        /// <returns>The result file after the hash.</returns>
        internal IEnumerable<ContentItem> Hash(IEnumerable<ContentItem> contentItems)
        {
            return contentItems.Select(this.Hash);
        }

        /// <summary>Hash the file.</summary>
        /// <param name="contentItem">The content item.</param>
        /// <returns>The result file after the hash.</returns>
        internal ContentItem Hash(ContentItem contentItem)
        {
            var originRelativePath = contentItem.RelativeContentPath;
            var hashedFileName = contentItem.GetContentHash(this.context) + Path.GetExtension(originRelativePath);
            var destinationFilePath = this.GetDestinationFilePath(this.DestinationDirectory, hashedFileName, contentItem.RelativeContentPath);

            var hashedDestinationFolder = this.context.Configuration.DestinationDirectory ?? this.DestinationDirectory;

            var relativeHashedPath = destinationFilePath;
            if (!string.IsNullOrWhiteSpace(hashedDestinationFolder) && Path.IsPathRooted(relativeHashedPath))
            {
                relativeHashedPath = relativeHashedPath.MakeRelativeToDirectory(hashedDestinationFolder);
            }

            contentItem = ContentItem.FromContentItem(contentItem, null, relativeHashedPath);

            // Do not overwrite if exists, since filename is md5 hash, filename changes if content changes.
            contentItem.WriteToHashedPath(hashedDestinationFolder);

            // Append to the log
            this.AppendToWorkLog(contentItem);

            // Return it as a result file.
            return contentItem;
        }

        /// <summary>Saves the log.</summary>
        /// <param name="append">If the save should append or overwrite.</param>
        internal void Save(bool append = true)
        {
            this.WriteLog(append);
        }

        /// <summary>Appends to the work log from cache results.</summary>
        /// <param name="cacheResults">The cache results.</param>
        internal void AppendToWorkLog(IEnumerable<ContentItem> cacheResults)
        {
            foreach (var cacheResult in cacheResults)
            {
                this.AppendToWorkLog(cacheResult);
            }
        }

        /// <summary>Appends to the work log from a cache result.</summary>
        /// <param name="cacheResult">The cache result.</param>
        internal void AppendToWorkLog(ContentItem cacheResult)
        {
            this.AppendToWorkLog(cacheResult.RelativeContentPath, cacheResult.RelativeHashedContentPath);
        }

        /// <summary>Gets filters for use with DirectoryInfo.GetFiles().</summary>
        /// <param name="filterType">A '*.css' or camma-separated like '*.gif,*.jpeg'.</param>
        /// <returns>A string array with appropriate file filters (e.g. [*.css] or [*.gif,*.jpeg,...]).</returns>
        private static IEnumerable<string> GetFilters(string filterType)
        {
            return string.IsNullOrWhiteSpace(filterType) ? new[] { "*" } : filterType.Split(Strings.FileFilterSeparator, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>The get url path.</summary>
        /// <param name="key">The key.</param>
        /// <returns>The <see cref="string"/>.</returns>
        private static string GetUrlPath(string key)
        {
            return key.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        /// <summary>Copies and hashes the Directory.</summary>
        /// <param name="sourceDirectory">Path to source directory.</param>
        /// <param name="destinationDirectory">Path to destination directory.</param>
        /// <param name="filters">Array of file filters to apply.</param>
        /// <param name="rootSourceDirectory">The root source directory</param>
        /// <returns>The result files after the hash.</returns>
        private IEnumerable<ContentItem> Hash(string sourceDirectory, string destinationDirectory, IEnumerable<string> filters, string rootSourceDirectory = null)
        {
            var results = new List<ContentItem>();

            // Create the directory if does not exist.
            Directory.CreateDirectory(destinationDirectory);

            var sourceDirectoryInfo = new DirectoryInfo(sourceDirectory);

            rootSourceDirectory = rootSourceDirectory ?? sourceDirectoryInfo.FullName;

            // Need to do this for only the file type(s) desired
            results.AddRange(
                filters.SelectMany(filter =>
                    sourceDirectoryInfo
                    .EnumerateFiles(filter, SearchOption.TopDirectoryOnly)
                        .Select(sourceFileInfo =>
                            this.Hash(ContentItem.FromFile(sourceFileInfo.FullName, sourceFileInfo.FullName.MakeRelativeToDirectory(rootSourceDirectory))))));

            // recurse through subdirs, either keeping the source dir structure in the destination or flattening it
            foreach (var subDirectoryInfo in sourceDirectoryInfo.GetDirectories())
            {
                var subDestinationDirectory =
                    this.ShouldPreserveSourceDirectoryStructure
                    ? Path.Combine(destinationDirectory, subDirectoryInfo.Name)
                    : destinationDirectory;

                results.AddRange(
                    this.Hash(subDirectoryInfo.FullName, subDestinationDirectory, filters, rootSourceDirectory));
            }

            return results;
        }

        /// <summary>Gets the destination file path for a hashed file name: /12/34567xxxxx.png.</summary>
        /// <param name="destination">The destination path.</param>
        /// <param name="hashedFileName">The hashed file name.</param>
        /// <param name="originRelativePath"></param>
        /// <returns>The destination file path.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Hash md5 lowercase")]
        private string GetDestinationFilePath(string destination, string hashedFileName, string originRelativePath)
        {
            string destinationFilePath;

            if (this.CreateExtraDirectoryLevelFromHashes)
            {
                // Use the first 2 chars for a directory name, the last chars for the file name
                destinationFilePath = Path.Combine(destination, hashedFileName.Substring(0, 2)).ToLowerInvariant();

                // This will be the 2 char subdir, and may not exist yet
                if (!Directory.Exists(destinationFilePath))
                {
                    Directory.CreateDirectory(destinationFilePath);
                }

                // Now get the file 
                destinationFilePath = Path.Combine(destinationFilePath, hashedFileName.Remove(0, 2));
            }
            else if (this.ShouldPreserveSourceDirectoryStructure)
            {
                destinationFilePath = Path.Combine(destination, Path.GetDirectoryName(originRelativePath), hashedFileName);
            }
            else
            {
                destinationFilePath = Path.Combine(destination, hashedFileName);
            }

            return destinationFilePath.ToLowerInvariant();
        }

        /// <summary>Simple logger for generating report of work done.</summary>
        /// <param name="fileBeforeHashing">Path plus file name before hash renaming.</param>
        /// <param name="fileAfterHashing">Path plus file name after hash renaming.</param>
        /// <param name="skipIfExists">skips the add if it already exists.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Need lowercase")]
        private void AppendToWorkLog(string fileBeforeHashing, string fileAfterHashing, bool skipIfExists = false)
        {
            fileAfterHashing = Path.Combine(this.context.Configuration.DestinationDirectory ?? this.DestinationDirectory, fileAfterHashing);
            fileBeforeHashing = this.NormalizeFileForWorkLog(fileBeforeHashing, this.BasePrefixToRemoveFromInputPathInLog);
            fileAfterHashing = this.NormalizeFileForWorkLog(fileAfterHashing, this.BasePrefixToRemoveFromOutputPathInLog);

            if (Path.IsPathRooted(fileBeforeHashing))
            {
                fileBeforeHashing = fileBeforeHashing.MakeRelativeToDirectory(this.BasePrefixToRemoveFromInputPathInLog);
            }

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

        /// <summary>The normalize file for work log.</summary>
        /// <param name="file">The file after hashing.</param>
        /// <param name="preFixToRemoveFromWorkLog">The pre fix to remove from work log.</param>
        /// <returns>The <see cref="string"/>.</returns>
        private string NormalizeFileForWorkLog(string file, string preFixToRemoveFromWorkLog)
        {
            if (Path.IsPathRooted(file))
            {
                file = file.MakeRelativeToDirectory(preFixToRemoveFromWorkLog);
            }
            else if (!string.IsNullOrWhiteSpace(preFixToRemoveFromWorkLog))
            {
                var relativeRemoveFromOutputPath = preFixToRemoveFromWorkLog.MakeRelativeToDirectory(this.DestinationDirectory);
                if (!string.IsNullOrWhiteSpace(relativeRemoveFromOutputPath))
                {
                    if (file.StartsWith(relativeRemoveFromOutputPath, StringComparison.OrdinalIgnoreCase))
                    {
                        file = file.Substring(relativeRemoveFromOutputPath.Length);
                    }
                }
            }

            return file.NormalizeUrl();
        }

        /// <summary>Writes a log in an xml file showing which files were copied, with old and new names in it.</summary>
        /// <param name="appendToLog">If we append or overwrite</param>
        private void WriteLog(bool appendToLog = true)
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
                        var outputPath = GetUrlPath(key);

                        // if desired, add a base to the path otherwise add the default "/"
                        outputPath = (this.BasePrefixToAddToOutputPath ?? Path.AltDirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)) + outputPath.TrimStart(Path.AltDirectorySeparatorChar);

                        writer.WriteValue(outputPath);
                        writer.WriteEndElement();
                        foreach (var oldName in this.renamedFilesLog[key].OrderBy(r => r))
                        {
                            writer.WriteStartElement("Input");
                            writer.WriteValue(Path.AltDirectorySeparatorChar + GetUrlPath(oldName).TrimStart(Path.AltDirectorySeparatorChar));
                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
            }

            // write the log value out to a file
            FileHelper.WriteFile(this.LogFileName, stringBuilder.ToString());
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
                    if (File.Exists(outputPath))
                    {
                        var inputs = fileElement.Elements("Input").Select(e => (string)e);
                        foreach (var input in inputs)
                        {
                            this.AppendToWorkLog(input, outputPath, true);
                        }
                    }
                }
            }
        }
    }
}
