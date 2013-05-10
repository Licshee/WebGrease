// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebGreaseTask.cs" company="Microsoft">
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

    using Activities;

    using Microsoft.Build.Utilities;

    using WebGrease.Configuration;
    using WebGrease.Extensions;

    /// <summary>
    /// Build time task for executing web grease runtime.
    /// </summary>
    public class WebGreaseTask : Task
    {
        /// <summary>Initializes a new instance of the <see cref="WebGreaseTask"/> class.</summary>
        public WebGreaseTask()
        {
            this.FileType = FileTypes.All;
        }

        /// ReSharper disable UnusedAutoPropertyAccessor.Global
        /// ReSharper disable MemberCanBePrivate.Global
        /// <summary>Gets or sets the folder for the configuration assemblies, this is the folder where MEF assemblies will be loaded from.
        /// By default the task will try and load them from the folder where the assembly of the build task resides.</summary>
        public string PreprocessingPluginPath { get; set; }

        /// <summary>Gets or sets the type of config to use with for each action of the runtime.</summary>
        public string Activity { get; set; }

        /// <summary>Gets or sets the type of config to use with for each action of the runtime.</summary>
        public string ConfigType { get; set; }

        /// <summary>Gets or sets the path to the configuration files to use.</summary>
        public string ConfigurationPath { get; set; }

        /// <summary>Gets or sets the root output folder</summary>
        public string RootOutputPath { get; set; }

        /// <summary>If set to stylesheet or javascript will exeucte only for the set file type.</summary>
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

        /// <summary>Gets or sets a value indicating whether to clean the tools temp folder.</summary>
        public bool CleanToolsTemp { get; set; }

        /// <summary>Gets or sets a value indicating whether to clean the destination folder.</summary>
        public bool CleanDestination { get; set; }

        /// <summary>Gets or sets a value indicating whether to clean the cache folders.</summary>
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

        /// <summary>Gets or sets the value that determines how long to keep cache items that have not been touched. (both read and right will touch a file)</summary>
        public string CacheTimeout { get; set; }

        /// ReSharper restore MemberCanBePrivate.Global
        /// ReSharper restore UnusedAutoPropertyAccessor.Global
        /// <summary>Gets the measure results.</summary>
        internal TimeMeasureResult[] MeasureResults { get; private set; }

        /// <summary>
        /// Executes the webgrease runtime.
        /// </summary>
        /// <returns>Returns a value indicating whether the run was successful or not.</returns>
        public override bool Execute()
        {
            var start = DateTimeOffset.Now;
            var sessionContext = this.CreateSessionContext();

            this.ExecuteClean(sessionContext);

            var fullPathToConfigFiles = Path.GetFullPath(this.ConfigurationPath);

            var inputFiles = new InputSpec { Path = fullPathToConfigFiles, IsOptional = true, SearchOption = SearchOption.TopDirectoryOnly }.GetFiles();

            var endResult = sessionContext.Section(
                new[] { SectionIdParts.WebGreaseBuildTask },
                new { inputFiles, sessionContext.Configuration },
                true,
                sessionCacheSection =>
                {
                    var result = true;
                    try
                    {
                        // get a list of the files in the configuration folder
                        foreach (var configFile in inputFiles)
                        {
                            result = this.ExecuteConfigFile(sessionContext, configFile) && result;
                        }
                    }
                    catch (BuildWorkflowException exception)
                    {
                        this.LogExtendedError(exception.Subcategory, exception.ErrorCode, exception.HelpKeyword, exception.File, exception.LineNumber, exception.EndLineNumber, exception.ColumnNumber, exception.EndColumnNumber, exception.Message);
                        return false;
                    }
                    catch (Exception exception)
                    {
                        this.LogError(exception, exception.Message, null);
                        return false;
                    }

                    return result;
                });

            if (endResult)
            {
                sessionContext.Cache.CleanUp();

                var sessionReportFile = Path.Combine(this.RootOutputPath, new DirectoryInfo(fullPathToConfigFiles).Name);
                sessionContext.Measure.WriteResults(sessionReportFile, fullPathToConfigFiles, start);
                this.MeasureResults = sessionContext.Measure.GetResults();
            }

            return endResult;
        }

        /// <summary>Execute a single config file.</summary>
        /// <param name="sessionContext">The session context.</param>
        /// <param name="configFile">The config file.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private bool ExecuteConfigFile(IWebGreaseContext sessionContext, string configFile)
        {
            var configFileStart = DateTimeOffset.Now;
            var configFileInfo = new FileInfo(configFile);
            var fileContext = new WebGreaseContext(sessionContext, configFileInfo);

            return sessionContext.Section(
                new[] { SectionIdParts.WebGreaseBuildTask, SectionIdParts.ConfigurationFile },
                ContentItem.FromFile(configFileInfo.FullName),
                fileContext.Configuration,
                true,
                configFileCacheSection =>
                {
                    bool result;
                    switch ((this.Activity ?? string.Empty).ToUpperInvariant())
                    {
                        case "BUNDLE":
                            fileContext.Log.Information("Activity: Bundle");
                            var bundleActivity = new BundleActivity(fileContext);

                            // execute the bundle pipeline
                            result = bundleActivity.Execute(this.FileType);
                            break;
                        case "EVERYTHING":
                        default:
                            fileContext.Log.Information("Activity: Everything");
                            var everythingActivity = new EverythingActivity(fileContext);

                            // execute the full pipeline
                            result = everythingActivity.Execute(this.FileType);
                            break;
                    }

                    if (result)
                    {
                        var configReportFile = Path.Combine(this.RootOutputPath, configFileInfo.Name);
                        if (this.CacheOutputDependencies)
                        {
                            configFileCacheSection.WriteDependencyGraph(configReportFile);
                        }

                        fileContext.Measure.WriteResults(configReportFile, configFileInfo.FullName, configFileStart);
                    }

                    return result;
                });
        }

        /// <summary>The execute clean.</summary>
        /// <param name="sessionContext">The session context.</param>
        private void ExecuteClean(IWebGreaseContext sessionContext)
        {
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
        }

        /// <summary>Creates a new session context base on the current settings.</summary>
        /// <returns>The <see cref="IWebGreaseContext"/>.</returns>
        private IWebGreaseContext CreateSessionContext()
        {
            return new WebGreaseContext(
                this.CreateSessionConfiguration(),
                this.LogInformation,
                this.WarningsAsErrors ? this.LogExtendedError : (LogExtendedError)this.LogExtendedWarning,
                this.LogError,
                this.LogExtendedError);
        }

        /// <summary>Creates the session configuration.</summary>
        /// <returns>The <see cref="WebGreaseConfiguration"/>.</returns>
        private WebGreaseConfiguration CreateSessionConfiguration()
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
                       };
        }

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
            if (string.IsNullOrWhiteSpace(this.OriginalProjectInputPath))
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
                    this.Log.LogError(
                        buildException.Subcategory,
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
