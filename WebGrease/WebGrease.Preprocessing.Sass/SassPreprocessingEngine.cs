// ----------------------------------------------------------------------------------------------------
// <copyright file="SassPreprocessingEngine.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease.Preprocessing.Sass
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    using WebGrease.Activities;
    using WebGrease.Configuration;

    /// <summary>
    /// This is the sass pre processing engine plugin
    /// When called it will try and parse the string passed with the sass engine in ruby.
    /// It will return css.
    /// </summary>
    [Export(typeof(IPreprocessingEngine))]
    public class SassPreprocessingEngine : IPreprocessingEngine
    {
        #region Constants

        /// <summary>
        /// The name of the embedded resource archive containing the ruby and suss runtimes.
        /// </summary>
        private const string EmbeddedResourceName = "WebGrease.Preprocessing.Sass.ruby193.zip";

        /// <summary>
        /// The name of the .hash file to use
        /// </summary>
        private const string HashFilename = ".hash";

        /// <summary>
        /// The location in the archive of ruby.exe
        /// </summary>
        private const string RubyExecutable = "Ruby193\\bin\\ruby.exe";

        /// <summary>
        /// The execution parameters for sass
        /// </summary>
        private const string SassExecuteParametersFormat = "{0} \"{1}\"  --load-path \"{2}\"";

        /// <summary>
        /// The filename for the sass executable.
        /// </summary>
        private const string SassFile = @"..\lib\ruby\gems\1.9.1\gems\sass-3.2.0.alpha.277\bin\sass";

        /// <summary>
        /// The name of the temp folder.
        /// </summary>
        private const string TempFolderName = "ZippyRubyTemp";

        /// <summary>The regex replace token for the TokenRegex.</summary>
        private const string TokenRegexReplaceValue = "${token}";

        #endregion

        #region Static Fields

        /// <summary>
        /// Lazy object used to do Initialize once.
        /// </summary>
        private static readonly Lazy<bool> Initialized = new Lazy<bool>(Initialize, true);

        /// <summary>
        /// The regex to parse a syntaxt error if there is one from sass.
        /// </summary>
        private static readonly Regex SassSyntaxErrorRegex = new Regex("Syntax error:(?<message>.*?)\n.*?on line (?<line>\\d+) of (?<file>.*?)\n", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Regex to match token("%TOKEN_NAME%") in the output with %TOKEN_NAME% to be passed on to webgrease.
        /// token("...") is a valid syntaxt in sass, %TOKEN_NAME% is not.
        /// </summary>
        private static readonly Regex TokenRegex = new Regex(@"token\((?<quote>[""'])(?<token>.*?)\k<quote>\)", RegexOptions.Compiled);

        /// <summary>
        /// This is the patternt hat is used to match the Imports(".*?") statement
        /// </summary>
        private static readonly Regex ImportsPattern = new Regex("@imports \"(?<path>.*?)\";", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>The ruby root path</summary>
        private static string rubyRootPath;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Creates the SassPreprocessingEngine
        /// </summary>
        #endregion

        #region Public Properties

        /// <summary>The name of this preprocessor (Name has to be set in a configuration for the preprocessor to be used)</summary>
        public string Name
        {
            get
            {
                return "sass";
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// This method will be called to check if the processor believes it can handle the file based on the filename.
        /// </summary>
        /// <param name="fullFileName">The full path to the file.</param>
        /// <param name="preprocessConfig">The configuration</param>
        /// <returns>If it thinks it can process it.</returns>
        public bool CanProcess(string fullFileName, PreprocessingConfig preprocessConfig = null)
        {
            var sassConfig = GetConfig(preprocessConfig);
            var fi = new FileInfo(fullFileName);
            return fi.Extension.EndsWith(sassConfig.SassExtension, StringComparison.OrdinalIgnoreCase) || fi.Extension.EndsWith(sassConfig.ScssExtension, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The main method for Preprocessing, this is where the preprocessor gets passed the full content, parses it and returns the parsed content.
        /// </summary>
        /// <param name="fileContent">Content of the file to parse.</param>
        /// <param name="fullFileName">The full filename</param>
        /// <param name="preprocessConfig">The configuration.</param>
        /// <returns>The processed content.</returns>
        /// <param name="logInformation">The information log delegate.</param>
        /// <param name="logError">The error log delegate </param>
        /// <param name="logExtendedError">The extended log error delegate.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Is meant to catch all, if delete fails it is not important, it isn in the temp folder.")]
        public string Process(string fileContent, string fullFileName, PreprocessingConfig preprocessConfig, Action<string> logInformation, LogError logError, LogExtendedError logExtendedError)
        {
            logInformation("Sass: Processing contents for file {0}".InvariantFormat(fullFileName));

            fileContent = ParseImports(fileContent, fullFileName);

            var fi = new FileInfo(fullFileName);
            var tempFile = Path.GetTempFileName() + fi.Extension;
            try
            {
                File.WriteAllText(tempFile, fileContent, Encoding.UTF8);
                return ProcessFile(tempFile, fi.DirectoryName, fullFileName, logInformation, logError, logExtendedError);
            }
            finally
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Parse out the @imports and replace with all files in directory @import.
        /// </summary>
        /// <param name="fileContent">The content</param>
        /// <param name="fullFileName">The full filename</param>
        /// <returns>The parses less content.</returns>
        private static string ParseImports(string fileContent, string fullFileName)
        {
            var fi = new FileInfo(fullFileName);
            var workingFolder = fi.DirectoryName+"\\";
            return ImportsPattern.Replace(fileContent, (match) => ReplaceImports(match, workingFolder));
        }

        private static string ReplaceImports(Match match, string workingFolder)
        {
            var path = match.Groups["path"].Value;
            string pattern = null;
            var pathParts = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var possiblePattern = pathParts.LastOrDefault();
            if (!string.IsNullOrEmpty(possiblePattern) && possiblePattern.Contains("*"))
            {
                pattern = possiblePattern;
                pathParts.RemoveAt(pathParts.Count - 1);
            }

            var pathInfo = new DirectoryInfo(Path.Combine(workingFolder, string.Join("\\", pathParts)));
            if (!pathInfo.Exists)
            {
                return string.Empty;
            }

            var files =
                (!string.IsNullOrWhiteSpace(pattern)
                    ? Directory.GetFiles(pathInfo.FullName, pattern)
                    : Directory.GetFiles(pathInfo.FullName));

            return 
                string.Join(
                    " ", 
                    files.Select(file =>
                        "@import \"{0}\";".InvariantFormat(MakeRelativePath(workingFolder, file).Replace("\\","/"))));
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static String MakeRelativePath(String fromPath, String toPath)
        {
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }


        #endregion

        #region Methods

        /// <summary>
        /// Create a SassConfig from the configuration element in the preprocessingConfig
        /// </summary>
        /// <param name="preprocessConfig">The pre processing config</param>
        /// <returns>The Sass Config</returns>
        private static SassConfig GetConfig(PreprocessingConfig preprocessConfig)
        {
            //TODO: RTUIT: Make the creation of the config cached
            return new SassConfig(preprocessConfig);
        }

        /// <summary>
        /// Method called by the Lazy initialize field, to force single initialization, thread safe.
        /// </summary>
        /// <returns>true if initialized, false when an exception occurred.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "No it is not.")]
        private static bool Initialize()
        {
            try
            {
                var tempPath = Path.GetTempPath();
                if (string.IsNullOrEmpty(tempPath))
                {
                    throw new PreprocessingException("Could not find the temp folder on this system.");
                }

                rubyRootPath = Path.Combine(tempPath, TempFolderName);

                var assembly = typeof(SassPreprocessingEngine).Assembly;
                var hash = assembly.GetName().Version + RetrieveLinkerTimestamp().ToString(CultureInfo.InvariantCulture);
                var hashFile = new FileInfo(Path.Combine(rubyRootPath, HashFilename));
                if (hashFile.Exists)
                {
                    using (var sr = hashFile.OpenText())
                    {
                        var hashContent = sr.ReadToEnd();
                        if (hashContent.Equals(hash))
                        {
                            return true;
                        }
                    }
                }

                ZipLib.ExtractEmbeddedResource(EmbeddedResourceName, rubyRootPath);

                using (var hashReader = hashFile.OpenWrite())
                {
                    using (var sr = new StreamWriter(hashReader))
                    {
                        sr.Write(hash);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new PreprocessingException("Unable to initialize the web grease sass preprocessor:\r\n "+ex, ex);
            }
        }

        /// <summary>
        /// This will:
        /// - Use a file on disk
        /// - Pass it onto ruby with sass
        /// - read the result
        /// - detect any syntax errors
        /// - return the parsed result and/or throw an exception.
        /// </summary>
        /// <param name="fullFileName">The full filename</param>
        /// <param name="workingDirectory">The working directory</param>
        /// <param name="originalFilename">The original filename</param>
        /// <param name="logInformation">The information log delegate.</param>
        /// <param name="logError">The error log delegate </param>
        /// <param name="logExtendedError">The extended log error delegate.</param>
        /// <returns>The parsed content of the file.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Catch-all on purpose...")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Need to figure out why this is saying this...")]
        private static string ProcessFile(string fullFileName, string workingDirectory, string originalFilename, Action<string> logInformation, LogError logError, LogExtendedError logExtendedError)
        {
            // One time initialization (Lazy<bool>)
            if (Initialized.Value)
            {
                try
                {
                    logInformation("Sassing: {0}".InvariantFormat(originalFilename));
                    // Determine all the file and paths
                    var targetFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), fullFileName));
                    var targetFolder = workingDirectory ?? targetFile.DirectoryName;
                    var rubyFilename = new FileInfo(Path.Combine(rubyRootPath, RubyExecutable));

                    // Create a new process object with all the execute information
                    var processStartInfo = new ProcessStartInfo(rubyFilename.FullName, SassExecuteParametersFormat.InvariantFormat(SassFile, targetFile, targetFolder)) { WorkingDirectory = rubyFilename.DirectoryName ?? string.Empty, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                    using (var proc = new Process { StartInfo = processStartInfo }) 
                    {
                        // Start the process
                        proc.Start();

                        // get the standard output
                        var result = proc.StandardOutput.ReadToEnd();
                        // get the standard error
                        var errorResult = proc.StandardError.ReadToEnd();

                        // if the standard error is not empty there was an error.
                        if (!string.IsNullOrEmpty(errorResult))
                        {
                            // try and match to see if it is a syntax error.
                            var match = SassSyntaxErrorRegex.Match(errorResult);
                            if (match.Success)
                            {
                                // parse the syntax error.
                                var message = match.Groups["message"].Value.Trim();
                                var fileName = match.Groups["file"].Value.Trim();
                                var errorFileInfo = new FileInfo(fileName);

                                // Get the info of the file we are working on
                                var fullFileInfo = new FileInfo(fullFileName);

                                // If the file is the same as the error, use the original filename (Initial file is copied in place)
                                // Else use whatever file we get passed as the error file.
                                var errorFile = 
                                    (fullFileInfo.FullName.Equals(errorFileInfo.FullName, StringComparison.OrdinalIgnoreCase))
                                    ? originalFilename
                                    : errorFileInfo.FullName;
                                
                                // Unused for now: var file = match.Groups["file"].Value.Trim();
                                var line = match.Groups["line"].Value.TryParseInt32();

                                logExtendedError(
                                    "Sass", 
                                    null, 
                                    null, 
                                    errorFile,
                                    line, 
                                    0, 
                                    line, 
                                    0,
                                    "SASS Syntax error on line: {0} of file: {1}\r{2}".InvariantFormat(line, originalFilename, message));

                                return null;
                            }
                            // throw a general exception.
                            logError(null, errorResult);
                            return null;
                        }

                        // Return the content with the tokens correctly replaces for webgrease.
                        return TokenRegex
                            .Replace(result.Replace("\r\n", "\n"), TokenRegexReplaceValue)
                            .Replace("\n", "\r\n");
                    }
                }
                catch (Exception ex)
                {
                    logError(ex, "Unknown error occured while trying to pre process sass file '{0}' to css: {1}".InvariantFormat(originalFilename, ex.Message), null);
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// This methiod retrieves the build date/time thje current calling assembly.
        /// </summary>
        /// <returns>The build/datetime of the current calling assembly</returns>
        private static DateTime RetrieveLinkerTimestamp()
        {
            var filePath = Assembly.GetCallingAssembly().Location;
            const int CPeHeaderOffset = 60;
            const int CLinkerTimestampOffset = 8;
            var b = new byte[2048];
            Stream s = null;

            try
            {
                s = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }

            var i = BitConverter.ToInt32(b, CPeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(b, i + CLinkerTimestampOffset);
            var dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt;
        }

        #endregion
    }
}