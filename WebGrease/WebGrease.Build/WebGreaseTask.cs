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
    using System.Globalization;
    using System.IO;

    using Activities;
    using Configuration;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Build time task for executing web grease runtime.
    /// </summary>
    public class WebGreaseTask : Task
    {
        /// <summary>
        /// Executes the webgrease runtime.
        /// </summary>
        /// <returns>Returns a value indicating whether the run was successful or not.</returns>
        public override bool Execute()
        {
            bool result = true;
            try
            {
                // get a list of the files in the configuration folder
                var fullPathToConfigFiles = Path.GetFullPath(this.ConfigurationPath);

                foreach (var configFile in Directory.GetFiles(fullPathToConfigFiles))
                {
                    this.LogInformation("Processing " + configFile);
                    var configFileInfo = new FileInfo(configFile);
                    var configSpecificLogPath = Path.Combine(this.LogFolderPath, Path.GetFileNameWithoutExtension(configFileInfo.Name));
                    var config = new WebGreaseConfiguration(configFile, this.RootInputPath, this.RootOutputPath, configSpecificLogPath, this.ApplicationRootPath);

                    switch ((this.Activity ?? string.Empty).ToUpperInvariant())
                    {
                        case ("BUNDLE"):
                            this.LogInformation("Activity: Bundle");
                            var bundleActivity = new BundleActivity(config, this.LogInformation, this.LogError, this.LogExtendedError, this.ConfigType, this.PreprocessingPluginPath);
                            // execute the bundle pipeline
                            result = bundleActivity.Execute() && result;
                            break;
                        case ("EVERYTHING"):
                        default:
                            this.LogInformation("Activity: Everything");
                            var everythingActivity = new EverythingActivity(config, this.LogInformation, this.LogError, this.LogExtendedError, this.ConfigType, this.PreprocessingPluginPath);
                            // execute the full pipeline
                            result = everythingActivity.Execute() && result;
                            break;
                    }

                }
            }
            catch (Exception exception)
            {
                this.LogError(exception, null, null);

                result = false;
            }

            return result;
        }

        /// <summary>
        /// Gets or sets the folder for the configuration assemblies, this is the folder where MEF assemblies will be loaded from.
        /// By default the task will try and load them from the folder where the assembly of the build task resides.
        /// </summary>
        public string PreprocessingPluginPath { get; set; }

        /// <summary>
        /// Gets or sets the type of config to use with for each action of the runtime.
        /// </summary>
        public string Activity { get; set; }

        /// <summary>
        /// Gets or sets the type of config to use with for each action of the runtime.
        /// </summary>
        public string ConfigType { get; set; }

        /// <summary>
        /// Gets or sets the path to the configuration files to use.
        /// </summary>
        public string ConfigurationPath { get; set; }

        /// <summary>
        /// Gets or sets the root output folder
        /// </summary>
        public string RootOutputPath { get; set; }

        /// <summary>
        /// Gets or sets the root intput path, applied to relative paths in the file.
        /// </summary>
        public string RootInputPath { get; set; }

        /// <summary>
        /// Gets or sets the path where the content in the RootInputPath is located in the project (For jumping to error's in visual studio from the error window).
        /// </summary>
        public string OriginalProjectInputPath { get; set; }

        /// <summary>
        /// Gets or sets the root path of the application.
        /// </summary>
        public string ApplicationRootPath { get; set; }

        /// <summary>
        /// Gets or sets the folder path used for writing log files.
        /// </summary>
        public string LogFolderPath { get; set; }

        /// <summary>
        /// Method for logging information messages to the build output.
        /// </summary>
        /// <param name="message">message to log</param>
        private void LogInformation(string message)
        {
            this.Log.LogMessageFromText(message, Microsoft.Build.Framework.MessageImportance.Normal);
        }

        /// <summary>
        /// Logs an extended error
        /// </summary>
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
            this.Log.LogError(subcategory, errorCode, helpKeyword, this.ChangeToOriginalProjectLocation(file), lineNumber ?? 0, columnNumber ?? 0, endLineNumber ?? 0, endColumnNumber ?? 0, message);
        }

        /// TODO: RTUIT: Make this work for bundled files as well. (By reading the /* filename */ comments in the file, this should probably throw both exceptions then, to the bundled amnd the original.
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
