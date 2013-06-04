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
    using System.Text.RegularExpressions;

    using WebGrease.Configuration;
    using WebGrease.Extensions;

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
        private const string SassExecuteParametersFormat = "{0} \"{1}\"  --load-path \"{2}\" --";

        /// <summary>
        /// The filename for the sass executable.
        /// </summary>
        private const string SassFile = @"..\lib\ruby\gems\1.9.1\gems\sass-3.2.0.alpha.277\bin\sass";

        /// <summary>
        /// The name of the temp folder.
        /// </summary>
        private const string TempFolderName = "WebGreaseRubySassTemp";

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
        private static readonly Regex ImportsPattern = new Regex(@"@imports\s*?(?<quote>[""'])(?<path>.*?)\k<quote>\s*?;", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// This is the pattern hat is used to match the Import ".*?" statement
        /// </summary>
        private static readonly Regex ImportPattern = new Regex(@"@import\s*?(?<quote>[""'])(?<path>.*?)\k<quote>\s*?;", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>The ruby root path</summary>
        private static string rubyRootPath;

        /// <summary>The context.</summary>
        private IWebGreaseContext context;

        #endregion

        #region Constructors and Destructors

        /// <summary>Gets the name of this pre-processor (Name has to be set in a configuration for the pre-processor to be used)</summary>
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
        /// <param name="contentItem">The full path to the file.</param>
        /// <param name="preprocessConfig">The configuration</param>
        /// <returns>If it thinks it can process it.</returns>
        public bool CanProcess(ContentItem contentItem, PreprocessingConfig preprocessConfig = null)
        {
            var sassConfig = GetConfig(preprocessConfig);
            var extension = Path.GetExtension(contentItem.RelativeContentPath);
            return
                extension != null
                && (extension.EndsWith(sassConfig.SassExtension, StringComparison.OrdinalIgnoreCase)
                    || extension.EndsWith(sassConfig.ScssExtension, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The initialize.</summary>
        /// <param name="webGreaseContext">The web grease context.</param>
        public void SetContext(IWebGreaseContext webGreaseContext)
        {
            if (webGreaseContext == null)
            {
                throw new ArgumentNullException("webGreaseContext");
            }

            this.context = webGreaseContext;
        }

        /// <summary>The main method for Preprocessing, this is where it gets passed the full content, parses it and returns the parsed content.</summary>
        /// <param name="contentItem">Content of the file to parse.</param>
        /// <param name="preprocessingConfig">The configuration.</param>
        /// <param name="minimalOutput">Is the goal to have the most minimal output (true skips lots of comments)</param>
        /// <returns>The processed content.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Is meant to catch all, if delete fails it is not important, it is in the temp folder.")]
        public ContentItem Process(ContentItem contentItem, PreprocessingConfig preprocessingConfig, bool minimalOutput)
        {
            var settingsMinimalOutput = preprocessingConfig != null && preprocessingConfig.Element != null && (bool?)preprocessingConfig.Element.Attribute("minimalOutput") == true;
            var relativeContentPath = contentItem.RelativeContentPath;
            this.context.Log.Information("Sass: Processing contents for file {0}".InvariantFormat(relativeContentPath));

            this.context.SectionedAction(SectionIdParts.Preprocessing, SectionIdParts.Process, "Sass")
                .Execute(
                    () =>
                    {
                        var sassCacheImportsSection = context.Cache.CurrentCacheSection;

                        string fileToProcess = null;
                        var isTemp = false;
                        try
                        {
                            var workingDirectory = Path.IsPathRooted(relativeContentPath)
                                                       ? Path.GetDirectoryName(relativeContentPath)
                                                       : this.context.GetWorkingSourceDirectory(relativeContentPath);

                            var content = ParseImports(contentItem.Content, workingDirectory, sassCacheImportsSection, minimalOutput || settingsMinimalOutput);

                            var currentContentHash = context.GetValueHash(content);

                            var contentIsUnchangedFromDisk = !string.IsNullOrWhiteSpace(contentItem.AbsoluteDiskPath)
                                && File.Exists(contentItem.AbsoluteDiskPath)
                                && context.GetFileHash(contentItem.AbsoluteDiskPath).Equals(currentContentHash);

                            if (contentIsUnchangedFromDisk)
                            {
                                fileToProcess = contentItem.AbsoluteDiskPath;
                            }
                            else if (!string.IsNullOrWhiteSpace(relativeContentPath))
                            {
                                fileToProcess = Path.Combine(this.context.Configuration.SourceDirectory ?? string.Empty, relativeContentPath);

                                fileToProcess = Path.ChangeExtension(fileToProcess, ".generated" + Path.GetExtension(fileToProcess));
                                relativeContentPath = Path.ChangeExtension(relativeContentPath, ".generated" + Path.GetExtension(relativeContentPath));

                                if (!File.Exists(fileToProcess) || !context.GetFileHash(fileToProcess).Equals(currentContentHash))
                                {
                                    File.WriteAllText(fileToProcess, content);
                                }
                            }
                            else
                            {
                                isTemp = true;
                                fileToProcess = Path.GetTempFileName() + Path.GetExtension(relativeContentPath);
                                File.WriteAllText(fileToProcess, content);
                            }

                            content = ProcessFile(fileToProcess, workingDirectory, relativeContentPath, this.context);

                            contentItem = content != null ? ContentItem.FromContent(content, contentItem) : null;

                            return true;
                        }
                        finally
                        {
                            if (isTemp && !string.IsNullOrWhiteSpace(fileToProcess))
                            {
                                try
                                {
                                    File.Delete(fileToProcess);
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                    });

            return contentItem;
        }

        /// <summary>
        /// Parse out the @imports and replace with all files in directory @import.
        /// </summary>
        /// <param name="fileContent">The content</param>
        /// <param name="workingFolder">The workingFolder</param>
        /// <param name="cacheSection">The cache section</param>
        /// <param name="minimalOutput">Is the goal to have the most minimal output (true skips lots of comments)</param>
        /// <returns>The parses less content.</returns>
        private static string ParseImports(string fileContent, string workingFolder, ICacheSection cacheSection, bool minimalOutput)
        {
            var withImports = ImportsPattern.Replace(fileContent, match => ReplaceImports(match, workingFolder.EnsureEndSeparator(), cacheSection, minimalOutput));
            SetImportSourceDependencies(withImports, workingFolder, cacheSection);
            return withImports;
        }

        /// <summary>Sets the import source dependencies.</summary>
        /// <param name="fileContent">The file content.</param>
        /// <param name="workingFolder">The working folder.</param>
        /// <param name="cacheSection">The cache section.</param>
        private static void SetImportSourceDependencies(string fileContent, string workingFolder, ICacheSection cacheSection)
        {
            var matches = ImportPattern.Matches(fileContent);
            foreach (Match match in matches)
            {
                var path = Path.Combine(workingFolder, match.Groups["path"].Value);
                var fi = new FileInfo(path);
                cacheSection.AddSourceDependency(path);
                if (fi.Exists)
                {
                    SetImportSourceDependencies(File.ReadAllText(fi.FullName), fi.DirectoryName, cacheSection);
                }
            }
        }

        /// <summary>The replace imports.</summary>
        /// <param name="match">The match.</param>
        /// <param name="workingFolder">The working folder.</param>
        /// <param name="cacheSection">The cache Section.</param>
        /// <param name="minimalOutput">Is the goal to have the most minimal output (true skips lots of comments)</param>
        /// <returns>The replaced imports.</returns>
        private static string ReplaceImports(Match match, string workingFolder, ICacheSection cacheSection, bool minimalOutput)
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

            cacheSection.AddSourceDependency(new InputSpec { IsOptional = true, Path = pathInfo.FullName, SearchOption = SearchOption.TopDirectoryOnly, SearchPattern = pattern });

            if (!pathInfo.Exists)
            {
                return string.Empty;
            }

            var files = !string.IsNullOrWhiteSpace(pattern)
                            ? Directory.GetFiles(pathInfo.FullName, pattern)
                            : Directory.GetFiles(pathInfo.FullName);

            return
                (minimalOutput ? string.Empty : GetImportsComment(path, string.Empty))
                + string.Join(
                    " ",
                    files
                        .Select(file => MakeRelativePath(workingFolder, file))
                        .Select(relativeFile => "{0}\r\n@import \"{1}\";".InvariantFormat(
                            minimalOutput ? string.Empty : "\r\n"+GetImportsComment(path, Path.GetFileName(relativeFile)), 
                            relativeFile.Replace("\\", "/"))));
        }
        
        /// <summary>Gets the imports comment.</summary>
        /// <param name="path">The path.</param>
        /// <param name="file">The file.</param>
        /// <returns>The import comment string: /* SASS Imports: path : file */</returns>
        private static string GetImportsComment(string path, string file)
        {
            return "/* #imports \"{0} \\{1}\" */".InvariantFormat(path, file);
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        private static string MakeRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath))
            {
                throw new ArgumentNullException("fromPath");
            }

            if (string.IsNullOrEmpty(toPath))
            {
                throw new ArgumentNullException("toPath");
            }

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
                throw new PreprocessingException("Unable to initialize the web grease sass preprocessor:\r\n " + ex, ex);
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
        /// <param name="relativeFilename">The original filename</param>
        /// <param name="context">The webgrease context</param>
        /// <returns>The parsed content of the file.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Catch-all on purpose...")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Need to figure out why this is saying this...")]
        private static string ProcessFile(string fullFileName, string workingDirectory, string relativeFilename, IWebGreaseContext context)
        {
            // One time initialization (Lazy<bool>)
            if (Initialized.Value)
            {
                try
                {
                    context.Log.Information("Sassing: {0}".InvariantFormat(relativeFilename));

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
                                    fullFileInfo.FullName.Equals(errorFileInfo.FullName, StringComparison.OrdinalIgnoreCase)
                                    ? relativeFilename
                                    : errorFileInfo.FullName;

                                // Unused for now: var file = match.Groups["file"].Value.Trim();
                                var line = match.Groups["line"].Value.TryParseInt32();

                                context.Log.Error(
                                    "Sass",
                                    null,
                                    null,
                                    errorFile,
                                    line,
                                    0,
                                    line,
                                    0,
                                    "SASS Syntax error on line: {0} of file: {1}\r{2}".InvariantFormat(line, relativeFilename, message));

                                return null;
                            }

                            // throw a general exception.
                            context.Log.Error(null, errorResult);
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
                    context.Log.Error(ex, "Unknown error occured while trying to pre process sass file '{0}' to css: {1}".InvariantFormat(relativeFilename, ex.Message));
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