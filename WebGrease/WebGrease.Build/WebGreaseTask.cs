// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebGreaseBuildTask.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Component for build time optimization of components with MSBuild.
// </summary>
// --------------------------------------------------------------------------------------------------------------------


namespace WebGrease.Build
{
    using System;
    using System.IO;
    using System.Linq;

    using Activities;

    using Microsoft.Build.Utilities;

    using WebGrease.Configuration;
    using WebGrease.Extensions;

    /// <summary>
    /// Build time task for executing web grease runtime.
    /// </summary>
    public class WebGreaseTask : Task
    {
        public WebGreaseTask()
        {
            this.FileType = FileTypes.All;
        }

        /// <summary>
        /// Executes the webgrease runtime.
        /// </summary>
        /// <returns>Returns a value indicating whether the run was successful or not.</returns>
        public override bool Execute()
        {
            var start = DateTime.UtcNow;
            var result = true;

            var sessionContext = this.CreateSessionContext();
            if (this.CleanCache)
            {
                sessionContext.CleanCache();
            }

            if (this.CleanToolsTemp)
            {
                sessionContext.CleanToolsTemp();
            }

            if (this.CleanDestination)
            {
                sessionContext.CleanDestination();
            }

            var fullPathToConfigFiles = Path.GetFullPath(this.ConfigurationPath);
            var inputFiles = new InputSpec { Path = fullPathToConfigFiles, IsOptional = true, SearchOption = SearchOption.TopDirectoryOnly }.GetFiles();

            var sessionReportFile = Path.Combine(this.RootOutputPath, new DirectoryInfo(fullPathToConfigFiles).Name);
            sessionContext.Measure.Start(TimeMeasureNames.Unidentified);
            var buildTaskCacheSection = sessionContext.Cache.BeginSection("buildtask", inputFiles.ToDictionary(f=>f, sessionContext.GetFileHash));
            if (!buildTaskCacheSection.CanBeSkipped())
            {
                try
                {
                    // get a list of the files in the configuration folder
                    foreach (var configFile in inputFiles.Select(f => new FileInfo(f)))
                    {
                        var configFileStart = DateTime.UtcNow;

                        var fileContext = new WebGreaseContext(sessionContext, configFile);

                        var configReportFile = Path.Combine(this.RootOutputPath, configFile.Name);
                        fileContext.Measure.Start(TimeMeasureNames.ConfigurationFile);
                        fileContext.Measure.BeginSection();
                        var configFileCacheSection = sessionContext.Cache.BeginSection("buildtask.file", configFile);
                        try
                        {
                            if (!configFileCacheSection.CanBeSkipped())
                            {
                                this.LogInformation("Processing " + configFile);

                                switch ((this.Activity ?? string.Empty).ToUpperInvariant())
                                {
                                    case ("BUNDLE"):
                                        this.LogInformation("Activity: Bundle");
                                        var bundleActivity = new BundleActivity(fileContext);
                                        // execute the bundle pipeline
                                        result = bundleActivity.Execute(this.FileType) && result;
                                        break;
                                    case ("EVERYTHING"):
                                    default:
                                        this.LogInformation("Activity: Everything");
                                        var everythingActivity = new EverythingActivity(fileContext);
                                        // execute the full pipeline
                                        result = everythingActivity.Execute(this.FileType) && result;
                                        break;
                                }
                                configFileCacheSection.Store(configReportFile);
                            }
                        }
                        finally
                        {
                            configFileCacheSection.EndSection();
                            fileContext.Measure.WriteResults(configReportFile, configFile.FullName, configFileStart);
                            fileContext.Measure.EndSection();
                            fileContext.Measure.End(TimeMeasureNames.ConfigurationFile);
                        }

                    }

                    buildTaskCacheSection.Store(sessionReportFile);
                }
                catch (Exception exception)
                {
                    this.LogError(exception, null, null);
                    result = false;
                }
                finally
                {
                    buildTaskCacheSection.EndSection();
                    sessionContext.Measure.End(TimeMeasureNames.Unidentified);
                }
            }

            if (result)
            {
                sessionContext.Cache.CleanUp();
                sessionContext.Measure.WriteResults(sessionReportFile, fullPathToConfigFiles, start);
                this.MeasureResults = sessionContext.Measure.GetResults();
            }

            return result;
        }

        internal TimeMeasureResult[] MeasureResults { get; set; }

        private IWebGreaseContext CreateSessionContext()
        {
            return new WebGreaseContext(
                       this.GetSessionConfiguration(),
                       this.LogInformation,
                       this.WarningsAsErrors ? this.LogExtendedError : (LogExtendedError)this.LogExtendedWarning,
                       this.LogError,
                       this.LogExtendedError);
        }

        private WebGreaseConfiguration GetSessionConfiguration()
        {
            return new WebGreaseConfiguration(
                this.ConfigType,
                this.RootInputPath,
                this.RootOutputPath,
                this.LogFolderPath,
                this.ToolsTempPath,
                this.ApplicationRootPath,
                this.PreprocessingPluginPath)
                       {
                           Measure = this.Measure,
                           CacheEnabled = this.CacheEnabled,
                           CacheRootPath = this.CacheRootPath,
                           CacheUniqueKey = this.CacheUniqueKey,
                           CacheTimeout = !string.IsNullOrWhiteSpace(this.CacheTimeout) ? TimeSpan.Parse(this.CacheTimeout) : TimeSpan.Zero,
                           CacheOutputDependencies = this.CacheOutputDependencies
                       };
        }

        /// <summary>
        /// Gets or sets the folder for the configuration assemblies, this is the folder where MEF assemblies will be loaded from.
        /// By default the task will try and load them from the folder where the assembly of the build task resides.
        /// </summary>
        public string PreprocessingPluginPath { get; set; }

        /// <summary>Gets or sets the type of config to use with for each action of the runtime.</summary>
        public string Activity { get; set; }

        /// <summary>Gets or sets the type of config to use with for each action of the runtime.</summary>
        public string ConfigType { get; set; }

        /// <summary>Gets or sets the path to the configuration files to use.</summary>
        public string ConfigurationPath { get; set; }

        /// <summary>Gets or sets the root output folder</summary>
        public string RootOutputPath { get; set; }

        // If set to stylesheet or javascript will exeucte only for the set file type.
        public FileTypes FileType { get; set; }

        /// <summary>Gets or sets the root intput path, applied to relative paths in the file.</summary>
        public string RootInputPath { get; set; }

        /// <summary>Gets or sets the path where the content in the RootInputPath is located in the project (For jumping to error's in visual studio from the error window).</summary>
        public string OriginalProjectInputPath { get; set; }

        /// <summary>Gets or sets the root path of the application.</summary>
        public string ApplicationRootPath { get; set; }

        /// <summary>Gets or sets the folder path used for writing log files.</summary>
        public string LogFolderPath { get; set; }

        /// <summary>Gets or sets the folder path used as temp folder.</summary>
        public string ToolsTempPath { get; set; }

        /// <summary>Gets or sets if warnings should be logged as errors.</summary>
        public bool WarningsAsErrors { get; set; }

        /// <summary>Gets or sets if it should measure and write measure files in the output folder.</summary>
        public bool Measure { get; set; }

        public bool CleanToolsTemp { get; set; }

        public bool CleanDestination { get; set; }
        
        public bool CleanCache { get; set; }

        /// <summary>Gets or sets the value that determines to use cache.</summary>
        public bool CacheEnabled { get; set; }

        /// <summary>Gets or sets the value that determines if it outputs .dgml cache dependency files.</summary>
        public bool CacheOutputDependencies { get; set; }

        /// <summary>
        /// Gets or sets the root path used for caching, this defaults to the ToolsTempPath.
        /// Use the system temp folder (%temp%) if you want to enable this on the build server.
        /// </summary>
        public string CacheRootPath { get; set; }

        /// <summary>
        /// Gets or sets the unique key for the unique key, is required when enabling cache.
        /// You should use the project guid and debug/release mode to make distinction between cache for different projects.
        /// </summary>
        public string CacheUniqueKey { get; set; }

        /// <summary>gets or sets the value that determines how long to keep cache items that have not been touched. (both read and right will touch a file)</summary>
        public string CacheTimeout { get; set; }

        /// <summary>Method for logging information messages to the build output.</summary>
        /// <param name="message">message to log</param>
        private void LogInformation(string message)
        {
            this.Log.LogMessageFromText(message, Microsoft.Build.Framework.MessageImportance.Normal);
        }

        /// <summary>Logs an extended error</summary>
        /// <param name="subcategory">The sub category</param>
        /// <param name="errorCode">The error code</param>
        /// <param name="helpKeyword">The help keyword</param>
        /// <param name="file">The file</param>
        /// <param name="lineNumber">The line number</param>
        /// <param name="columnNumber">The column number</param>
        /// <param name="endLineNumber">The end line number</param>
        /// <param name="endColumnNumber">The end column number</param>
        /// <param name="message">The message</param>
        private void LogExtendedError(string subcategory, string errorCode, string helpKeyword, string file, int? lineNumber, int? columnNumber, int? endLineNumber, int? endColumnNumber, string message)
        {
            this.Log.LogError(subcategory, errorCode, helpKeyword, this.ChangeToOriginalProjectLocation(file), lineNumber ?? 0, columnNumber ?? 0, endLineNumber ?? lineNumber ?? 0, endColumnNumber ?? columnNumber ?? 0, message);
        }

        /// <summary>Logs an extended error</summary>
        /// <param name="subcategory">The sub category</param>
        /// <param name="errorCode">The error code</param>
        /// <param name="helpKeyword">The help keyword</param>
        /// <param name="file">The file</param>
        /// <param name="lineNumber">The line number</param>
        /// <param name="columnNumber">The column number</param>
        /// <param name="endLineNumber">The end line number</param>
        /// <param name="endColumnNumber">The end column number</param>
        /// <param name="message">The message</param>
        private void LogExtendedWarning(string subcategory, string errorCode, string helpKeyword, string file, int? lineNumber, int? columnNumber, int? endLineNumber, int? endColumnNumber, string message)
        {
            this.Log.LogWarning(subcategory, errorCode, helpKeyword, this.ChangeToOriginalProjectLocation(file), lineNumber ?? 0, columnNumber ?? 0, endLineNumber ?? lineNumber ?? 0, endColumnNumber ?? columnNumber ?? 0, message);
        }

        /// TODO: RTUIT: Make this work for bundled files as well. (By available using source maps)
        /// <summary>
        /// This method is used when handling syntaxt error's thrown by any of the sub methods/activities.
        /// Changes the file the error originated in from the folder it is processed in to the location of the actual file in the project if it exists.
        /// If there is a OriginalProjectInputPath property set on the task it will try and use this as original path, if the file exists in the new path it will change the file for the error.
        /// If there is no OriginalProjectInputPath or the file does not exist in the rewritten path it will return the original filename.
        /// </summary>
        /// <param name="file">The file to find in the project</param>
        /// <returns>The file path changed to it's original path, so that visual studio error handling can use it.</returns>
        private string ChangeToOriginalProjectLocation(string file)
        {
            // If no OriginalProjectInputPath is set, which is fallback for every existing call, we just return the file.
            if (String.IsNullOrWhiteSpace(this.OriginalProjectInputPath))
            {
                return file;
            }

            // We try and rewrite the filepath with the original path
            var fi = new FileInfo(file);

            // Using directoryinfo to get rid of any double slashes and get a full absolute path.
            var rootInputPath = new DirectoryInfo(this.RootInputPath);
            var originalprojectInputPath = new DirectoryInfo(this.OriginalProjectInputPath);

            // Remove the inputroot path from the file
            var relativeFile = fi.FullName.Replace(rootInputPath.FullName, string.Empty).TrimStart(Path.DirectorySeparatorChar);

            // prepend the original path.
            var projectFilePath = Path.Combine(originalprojectInputPath.FullName, relativeFile);

            // if it exists we return the new file.
            if (File.Exists(projectFilePath))
            {
                return projectFilePath;
            }

            // otherwise we return the original filename
            return file;
        }

        /// <summary>
        /// Method for reporting errors to the build output (will fail build if invoked).
        /// </summary>
        /// <param name="exception">Exception to log</param>
        /// <param name="customMessage">Custom message to display</param>
        /// <param name="file">File that caused to the error</param>
        private void LogError(Exception exception, string customMessage, string file)
        {
            if (!string.IsNullOrWhiteSpace(customMessage))
            {
                if (!string.IsNullOrWhiteSpace(file))
                {
                    this.Log.LogError("error", null, null, this.ChangeToOriginalProjectLocation(file), 0, 0, 0, 0, customMessage);
                }
                else
                {
                    this.Log.LogError(customMessage);
                }
            }

            if (exception != null)
            {
                var buildException = exception as BuildWorkflowException;

                // log a friendlier message if one exists in the detail.
                if (buildException != null &&
                    buildException.HasDetailedError)
                {
                    this.Log.LogError(buildException.Subcategory,
                                      buildException.ErrorCode,
                                      buildException.HelpKeyword,
                                      this.ChangeToOriginalProjectLocation(buildException.File),
                                      buildException.LineNumber,
                                      buildException.ColumnNumber,
                                      buildException.EndLineNumber,
                                      buildException.EndColumnNumber,
                                      buildException.Message,
                                      new object[0]);
                }
                else
                {
                    // if not, just log the exception message.
                    this.Log.LogErrorFromException(exception, false);
                }

                if (exception.InnerException != null)
                {
                    this.Log.LogErrorFromException(exception.InnerException, false);
                }
            }

        }
    }
}
