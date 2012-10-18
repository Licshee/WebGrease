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
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using Common;

    /// <summary>Copies sources to destination, with new file name based on hash of content.</summary>
    internal sealed class FileHasherActivity
    {
        /// <summary>
        /// Renamed Files Log.
        /// </summary>
        private readonly Dictionary<string, List<string>> m_renamedFilesLog = new Dictionary<string, List<string>>();

        /// <summary>Initializes a new instance of the <see cref="FileHasherActivity"/> class.</summary>
        internal FileHasherActivity()
        {
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

        /// <summary>When overridden in a derived class, executes the task.</summary>
        internal void Execute()
        {
            // Clear out the collection since activities objects may be pooled
            m_renamedFilesLog.Clear();

            try
            {
                if (this.SourceDirectories == null || this.SourceDirectories.Count == 0)
                {
                    // No action for directory not present
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

                if (string.IsNullOrWhiteSpace(this.BasePrefixToRemoveFromInputPathInLog))
                {
                    this.BasePrefixToRemoveFromInputPathInLog = string.Empty;
                }

                foreach (var sourceDirectory in this.SourceDirectories)
                {
                    if (!Directory.Exists(sourceDirectory))
                    {
                        // No action for directory not present
                        Trace.TraceWarning(string.Format(CultureInfo.InvariantCulture, "FileHasherActivity - Could not locate the source directory at {0}", sourceDirectory));
                        continue;
                    }

                    // Copy the directory
                    this.CopyDirectory(sourceDirectory, this.DestinationDirectory, fileTypeFilter);
                }

                // All done, so log the output
                this.WriteLog(this.BasePrefixToRemoveFromInputPathInLog);
            }
            catch (Exception exception)
            {
                throw new WorkflowException("FileHasherActivity - Error happened while executing the activity.", exception);
            }
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

        /// <summary>Copy Directory.</summary>
        /// <param name="source">Path to source directory.</param>
        /// <param name="destination">Path to destination directory.</param>
        /// <param name="filters">Array of file filters to apply.</param>
        private void CopyDirectory(string source, string destination, IEnumerable<string> filters)
        {
            // Create the directory if does not exist.
            Directory.CreateDirectory(destination);

            var sourceDirInfo = new DirectoryInfo(source);

            // Need to do this for only the file type(s) desired
            foreach (var filter in filters)
            {
                foreach (var sourceFileInfo in sourceDirInfo.EnumerateFiles(filter))
                {
                    var logSourceEntry = sourceFileInfo.FullName;
                    var hashedFileName = HashUtility.GetHashStringForFile(sourceFileInfo.FullName) + sourceFileInfo.Extension;
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

                    if (!File.Exists(destinationFilePath))
                    {
                        // Creates the destination directory if it does not exist
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));
                        sourceFileInfo.CopyTo(destinationFilePath);
                    }

                    this.AppendToWorkLog(logSourceEntry, destinationFilePath);
                }
            }

            // recurse through subdirs, either keeping the source dir structure in the destination or flattening it
            foreach (var subDirectoryInfo in sourceDirInfo.GetDirectories())
            {
                this.CopyDirectory(subDirectoryInfo.FullName, this.ShouldPreserveSourceDirectoryStructure ? Path.Combine(destination, subDirectoryInfo.Name) : destination, filters);
            }
        }

        /// <summary>Simple logger for generating report of work done.</summary>
        /// <param name="fileBeforeHashing">Path plus file name before hash renaming.</param>
        /// <param name="fileAfterHashing">Path plus file name after hash renaming.</param>
        private void AppendToWorkLog(string fileBeforeHashing, string fileAfterHashing)
        {
            if (m_renamedFilesLog.ContainsKey(fileAfterHashing))
            {
                // append to its list
                m_renamedFilesLog[fileAfterHashing].Add(fileBeforeHashing);
            }
            else
            {
                // add a new key
                var originalNames = new List<string> { fileBeforeHashing };
                m_renamedFilesLog.Add(fileAfterHashing, originalNames);
            }
        }

        /// <summary>Writes a log in an xml file showing which files were copied, with old and new names in it.</summary>
        /// <param name="sourceDirectory">The source directory</param>
        private void WriteLog(string sourceDirectory)
        {
            if (string.IsNullOrWhiteSpace(this.LogFileName))
            {
                return;
            }

            var stringBuilder = new StringBuilder();
            var settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true };
            
            using (var writer = XmlWriter.Create(stringBuilder, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("RenamedFiles");

                // we still want a log file even if nothing was done to it, just no Flie nodes in it
                if (m_renamedFilesLog == null || m_renamedFilesLog.Keys.Count < 1)
                {
                    writer.WriteComment(ResourceStrings.NoFilesProcessed);
                }
                else
                {
                    foreach (var key in m_renamedFilesLog.Keys)
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
                        foreach (var oldName in m_renamedFilesLog[key])
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
    }
}
