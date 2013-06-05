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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Activities;

    using WebGrease.Configuration;
    using WebGrease.Css.Extensions;
    using WebGrease.Extensions;

    /// <summary>
    /// Build time task for executing web grease runtime.
    /// </summary>
    public class WebGreaseTask : Microsoft.Build.Utilities.Task
    {
        /// <summary>Gets set to true if there were any errors.</summary>
        private bool hasErrors;

        /// <summary>Initializes a new instance of the <see cref="WebGreaseTask"/> class.</summary>
        public WebGreaseTask()
        {
            this.FileType = FileTypes.All;
            this.Activity = "EVERYTHING";
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

        /// <summary>Gets or sets the folder path used for writing report files.</summary>
        public string MeasureReportPath { get; set; }

        /// <summary>Gets or sets the folder path used as temp folder.</summary>
        public string ToolsTempPath { get; set; }

        /// <summary>Gets or sets if warnings should be logged as errors.</summary>
        public bool WarningsAsErrors { get; set; }

        /// <summary>Gets or sets if it should measure and write measure files in the output folder.</summary>
        public bool Measure { get; set; }

        /// <summary>Gets or sets the value that determines to use cache.</summary>
        public bool CacheEnabled { get; set; }

        /// <summary>Gets or sets the value that determines if the the task cleans the destinations before running the activity.</summary>
        public bool CleanDestination { get; set; }

        /// <summary>Gets or sets the value that determines if the the task cleans the cache before running the activity.</summary>
        public bool CleanCache { get; set; }

        /// <summary>Gets or sets the value that has the temporary override file path.</summary>
        public string OverrideFile { get; set; }

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

            var activity = this.Activity.TryParseToEnum<ActivityName>();
            if (!activity.HasValue)
            {
                this.LogError("Unknown activity: {0}".InvariantFormat(this.Activity));
                return false;
            }

            var logManager = this.CreateLogManager();

            var globalContext = this.CreateSessionContext(logManager);

            if (globalContext.Configuration.Overrides != null && globalContext.Configuration.Overrides.SkipAll)
            {
                logManager.Information("WebGrease Skipping because of SkipAll in webgrease override file.");
                return true;
            }

            if (this.CleanDestination)
            {
                globalContext.CleanDestination();
            }

            var fileTypesToExecuteParalel = this.FileType == FileTypes.All ? new[] { FileTypes.JS, FileTypes.CSS | FileTypes.Image } : new[] { this.FileType };

            if (this.CleanCache)
            {
                globalContext.CleanCache();
            }

            var parallelSessions = new List<Tuple<IWebGreaseContext, FileTypes, DelayedLogManager>>();
            foreach (var fileType in fileTypesToExecuteParalel)
            {
                var delayedLogManager = new DelayedLogManager(logManager, fileType.ToString());
                var sessionContext = this.CreateSessionContext(delayedLogManager.LogManager, fileType, activity);
                if (this.CleanCache)
                {
                    sessionContext.CleanCache(logManager);
                }

                parallelSessions.Add(Tuple.Create(sessionContext, fileType, delayedLogManager));
            }

            if (Directory.Exists(globalContext.Configuration.IntermediateErrorDirectory))
            {
                Directory.Delete(globalContext.Configuration.IntermediateErrorDirectory, true);
            }

            var totalSuccess = true;
            this.MeasureResults = new TimeMeasureResult[] { };
            var localLock = new object();

            var activityResults = new Dictionary<FileTypes, Tuple<bool, TimeMeasureResult[], WebGreaseConfiguration>>();

            logManager.Information("WebGrease starting");
            Parallel.ForEach(
                parallelSessions,
                parallelSession =>
                {
                    logManager.Information("Starting WebGrease thread for " + parallelSession.Item2);
                    try
                    {
                        var fileTypeResult = this.Execute(parallelSession.Item2, activity.Value, this.ConfigurationPath, parallelSession.Item1);
                        totalSuccess &= fileTypeResult != null && fileTypeResult.Item1;
                        if (totalSuccess)
                        {
                            lock (localLock)
                            {
                                activityResults.Add(parallelSession.Item2, fileTypeResult);
                            }
                        }
                    }
                    finally
                    {
                        lock (localLock)
                        {
                            parallelSession.Item3.Flush();
                            parallelSessions.ForEach(dlm => dlm.Item3.Flush());
                        }
                    }
                });

            this.FinalizeMeasure(totalSuccess, activityResults, logManager, start, activity);

            logManager.Information("WebGrease done");
            return totalSuccess && !this.hasErrors;
        }

        /// <summary>The get file sets.</summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="fileType">The file type.</param>
        /// <returns>The filesets fot the file type.</returns>
        private static IEnumerable<IFileSet> GetFileSets(WebGreaseConfiguration configuration, FileTypes fileType)
        {
            var result = new List<IFileSet>();
            if (fileType.HasFlag(FileTypes.JS))
            {
                result.AddRange(configuration.JSFileSets);
            }

            if (fileType.HasFlag(FileTypes.CSS))
            {
                result.AddRange(configuration.CssFileSets);
            }

            return result;
        }

        /// <summary>Execute a single config file.</summary>
        /// <param name="sessionContext">The session context.</param>
        /// <param name="configFile">The config file.</param>
        /// <param name="activity">The activity</param>
        /// <param name="fileType">The file types</param>
        /// <param name="measure">If it should measure.</param>
        /// <returns>The success.</returns>
        private static Tuple<bool, int> ExecuteConfigFile(IWebGreaseContext sessionContext, string configFile, ActivityName activity, FileTypes fileType, bool measure)
        {
            var configFileStart = DateTimeOffset.Now;
            var configFileInfo = new FileInfo(configFile);

            // Creates the context specific to the configuration file
            var fileContext = new WebGreaseContext(sessionContext, configFileInfo);

            var configFileSuccess = true;
            var fileSets = GetFileSets(fileContext.Configuration, fileType).ToArray();
            if (fileSets.Length > 0 || (fileType.HasFlag(FileTypes.Image) && fileContext.Configuration.ImageDirectoriesToHash.Any()))
            {
                var configFileContentItem = ContentItem.FromFile(configFileInfo.FullName);
                configFileSuccess = sessionContext
                    .SectionedActionGroup(SectionIdParts.WebGreaseBuildTask, SectionIdParts.ConfigurationFile)
                    .MakeCachable(configFileContentItem, new { activity, fileContext.Configuration }, activity == ActivityName.Bundle) // Cached action can only be skipped when it is the bundle activity, otherwise don't.
                    .Execute(configFileCacheSection =>
                        {
                            fileContext.Configuration.AllLoadedConfigurationFiles.ForEach(configFileCacheSection.AddSourceDependency);

                            var success = true;
                            fileContext.Log.Information("Activity Start: [{0}] for [{1}] on configuration file \"{2}\"".InvariantFormat(activity, fileType, configFile), MessageImportance.High);
                            switch (activity)
                            {
                                case ActivityName.Bundle:
                                    // execute the bundle pipeline
                                    var bundleActivity = new BundleActivity(fileContext);
                                    success &= bundleActivity.Execute(fileSets);
                                    break;
                                case ActivityName.Everything:
                                    // execute the full pipeline
                                    var everythingActivity = new EverythingActivity(fileContext);
                                    success &= everythingActivity.Execute(fileSets, fileType);
                                    break;
                            }

                            if (success && measure)
                            {
                                var configReportFile = Path.Combine(sessionContext.Configuration.MeasureReportPath, (configFileInfo.Directory != null ? configFileInfo.Directory.Name + "." : string.Empty) + activity.ToString() + "." + fileType + "." + configFileInfo.Name + ".");
                                fileContext.Measure.WriteResults(configReportFile, configFileInfo.FullName, configFileStart);
                            }

                            fileContext.Log.Information("Activity End: [{0}] for [{1}] on configuration file \"{2}\"".InvariantFormat(activity, fileType, configFile), MessageImportance.High);
                            return success;
                        });
            }

            return Tuple.Create(configFileSuccess, fileSets.Length);
        }

        /// <summary>Finalizes the measure values, stores thejm on disk if configured that way.</summary>
        /// <param name="success">If the activity was successfull.</param>
        /// <param name="activityResults">The activity results.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="start">The start time of the current run..</param>
        /// <param name="activity">The current activity name.</param>
        private void FinalizeMeasure(bool success, ICollection<KeyValuePair<FileTypes, Tuple<bool, TimeMeasureResult[], WebGreaseConfiguration>>> activityResults, LogManager logManager, DateTimeOffset start, ActivityName? activity)
        {
            if (this.Measure && success && activityResults.Count > 0)
            {
                var configuration = activityResults.Select(ar => ar.Value.Item3).FirstOrDefault(ar => ar != null);
                if (configuration != null)
                {
                    var reportFileBase = Path.Combine(configuration.MeasureReportPath, new DirectoryInfo(this.ConfigurationPath).Name);
                    logManager.Information("Writing overal report file to:".InvariantFormat(reportFileBase));
                    TimeMeasure.WriteResults(
                        reportFileBase, activityResults.ToDictionary(k => k.Key, v => v.Value.Item2), this.ConfigurationPath, start, activity.ToString());
                }

                this.MeasureResults = activityResults.SelectMany(ar => ar.Value.Item2).ToArray();
            }
        }

        /// <summary>Create the log manager for the current msbuild context.</summary>
        /// <returns>The <see cref="LogManager"/>.</returns>
        private LogManager CreateLogManager()
        {
            return new LogManager(
                this.LogInformation,
                this.LogWarning,
                this.LogWarning,
                this.LogError,
                this.LogError,
                this.LogError,
                this.WarningsAsErrors);
        }

        /// <summary>The execute.</summary>
        /// <param name="fileType">The file type.</param>
        /// <param name="activity">The activity.</param>
        /// <param name="configurationPath">The configuration path</param>
        /// <param name="sessionContext">The session context</param>
        /// <returns>The <see cref="Tuple"/>.</returns>
        private Tuple<bool, TimeMeasureResult[], WebGreaseConfiguration> Execute(FileTypes fileType, ActivityName activity, string configurationPath, IWebGreaseContext sessionContext)
        {
            var availableFileSetsForFileType = 0;

            var fullPathToConfigFiles = Path.GetFullPath(configurationPath);

            var contentTypeSectionId = new[] { SectionIdParts.WebGreaseBuildTask };

            var sessionSuccess = sessionContext
                .SectionedAction(SectionIdParts.WebGreaseBuildTaskSession)
                .MakeCachable(new { fullPathToConfigFiles, activity })
                .Execute(sessionCacheSection =>
                {
                    var sessionCacheData = sessionCacheSection.GetCacheData<SessionCacheData>(CacheFileCategories.SolutionCacheConfig);

                    var inputFiles = new InputSpec { Path = fullPathToConfigFiles, IsOptional = true, SearchOption = SearchOption.TopDirectoryOnly }.GetFiles();
                    var contentTypeSuccess = sessionContext
                        .SectionedAction(contentTypeSectionId)
                        .MakeCachable(new { activity, inputFiles, sessionContext.Configuration }, activity == ActivityName.Bundle)
                        .Execute(contentTypeCacheSection =>
                            {
                                var success = true;
                                try
                                {
                                    // get a list of the files in the configuration folder
                                    foreach (var configFile in inputFiles)
                                    {
                                        var executeConfigFileResult = ExecuteConfigFile(sessionContext, configFile, activity, fileType, this.Measure);
                                        success &= executeConfigFileResult.Item1;
                                        availableFileSetsForFileType += executeConfigFileResult.Item2;
                                        GC.Collect();
                                    }
                                }
                                catch (BuildWorkflowException exception)
                                {
                                    this.LogError(exception.Subcategory, exception.ErrorCode, exception.HelpKeyword, exception.File, exception.LineNumber, exception.EndLineNumber, exception.ColumnNumber, exception.EndColumnNumber, exception.Message);
                                    return false;
                                }
                                catch (Exception exception)
                                {
                                    this.LogError(exception, exception.Message, null);
                                    return false;
                                }

                                if (success)
                                {
                                    // Add the current cachesection to the sessionCacheData
                                    if (sessionCacheData != null)
                                    {
                                        sessionCacheData.SetConfigTypeUniqueKey(this.ConfigType, contentTypeCacheSection.UniqueKey);
                                        sessionCacheSection.SetCacheData(CacheFileCategories.SolutionCacheConfig, sessionCacheData);
                                    }
                                }

                                return success;
                            });

                    if (contentTypeSuccess)
                    {
                        // Add the cache sections of the other already cached contentTypes (Debug/Release) so that they will not get removed.
                        this.HandleOtherContentTypeCacheSections(sessionContext, sessionCacheSection, sessionCacheData, contentTypeSectionId);
                    }

                    return contentTypeSuccess;
                });

            if (sessionSuccess && (availableFileSetsForFileType > 0))
            {
                sessionContext.Cache.CleanUp();
            }

            // Return tuple, that contains,the success, the results and the configuration for this thread.
            return Tuple.Create(sessionSuccess, sessionContext.Measure.GetResults(), sessionContext.Configuration);
        }

        /// <summary>The handle other content type cache sections.</summary>
        /// <param name="context">The context.</param>
        /// <param name="parentCacheSection">The session cache section.</param>
        /// <param name="sessionCacheData">The session cache data.</param>
        /// <param name="contentTypeSectionId">The content type section id.</param>
        private void HandleOtherContentTypeCacheSections(IWebGreaseContext context, ICacheSection parentCacheSection, SessionCacheData sessionCacheData, string[] contentTypeSectionId)
        {
            if (sessionCacheData != null)
            {
                foreach (var configType in sessionCacheData.ConfigTypes.Keys.Where(ct => !ct.Equals(this.ConfigType, StringComparison.OrdinalIgnoreCase)))
                {
                    // TODO: Do a better job at touching all the files used by the last build from another config type.
                    var otherContentTypeCacheSection = CacheSection.Begin(context, WebGreaseContext.ToStringId(contentTypeSectionId), sessionCacheData.GetConfigTypeUniqueKey(configType), parentCacheSection);
                    otherContentTypeCacheSection.Save();
                }
            }
        }

        /// <summary>Creates a new session context base on the current settings.</summary>
        /// <param name="logManager">The log Manager.</param>
        /// <param name="fileTypes">The file Type.</param>
        /// <param name="activity">The activity.</param>
        /// <returns>The <see cref="IWebGreaseContext"/>.</returns>
        private IWebGreaseContext CreateSessionContext(LogManager logManager, FileTypes? fileTypes = null, ActivityName? activity = null)
        {
            return new WebGreaseContext(this.CreateSessionConfiguration(fileTypes, activity), logManager);
        }

        /// <summary>Creates the session configuration.</summary>
        /// <param name="fileTypes">The file Type.</param>
        /// <param name="activity">The activity.</param>
        /// <returns>The <see cref="WebGreaseConfiguration"/>.</returns>
        private WebGreaseConfiguration CreateSessionConfiguration(FileTypes? fileTypes = null, ActivityName? activity = null)
        {
            var fileTypeKey = fileTypes != null ? "." + fileTypes : string.Empty;
            var activityKey = activity != null ? "." + activity : string.Empty;
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
                           CacheUniqueKey = this.CacheUniqueKey + fileTypeKey + activityKey,
                           MeasureReportPath = this.MeasureReportPath,
                           CacheTimeout = !string.IsNullOrWhiteSpace(this.CacheTimeout) ? TimeSpan.Parse(this.CacheTimeout) : TimeSpan.FromHours(48),
                           Overrides = TemporaryOverrides.Create(this.OverrideFile)
                       };
        }

        /// <summary>Method for logging information messages to the build output.</summary>
        /// <param name="message">message to log</param>
        /// <param name="messageImportance">The importance of the message</param>
        private void LogInformation(string message, MessageImportance messageImportance = MessageImportance.Normal)
        {
            var importance = Microsoft.Build.Framework.MessageImportance.Normal;
            switch (messageImportance)
            {
                case MessageImportance.High:
                    importance = Microsoft.Build.Framework.MessageImportance.High;
                    break;
                case MessageImportance.Low:
                    importance = Microsoft.Build.Framework.MessageImportance.High;
                    break;
            }

            this.Log.LogMessageFromText(message, importance);
        }

        /// <summary>Method for logging information messages to the build output.</summary>
        /// <param name="message">message to log</param>
        private void LogError(string message)
        {
            this.hasErrors = true;
            this.Log.LogError(message);
        }

        /// <summary>Method for logging information messages to the build output.</summary>
        /// <param name="message">message to log</param>
        private void LogWarning(string message)
        {
            this.Log.LogWarning(message);
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
        private void LogError(string subcategory, string errorCode, string helpKeyword, string file, int? lineNumber, int? columnNumber, int? endLineNumber, int? endColumnNumber, string message)
        {
            this.hasErrors = true;
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
        private void LogWarning(string subcategory, string errorCode, string helpKeyword, string file, int? lineNumber, int? columnNumber, int? endLineNumber, int? endColumnNumber, string message)
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
            this.hasErrors = true;
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
