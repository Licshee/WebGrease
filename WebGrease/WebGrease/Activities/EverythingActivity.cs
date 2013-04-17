// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EverythingActivity.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// <summary>
//   An activity that runs all the other activities based on configs.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Configuration;
    using Css;
    using Extensions;

    using WebGrease.Preprocessing;

    /// <summary>The main activity.</summary>
    internal sealed class EverythingActivity
    {
        /// <summary>
        /// private enumeration for the type of files being worked on
        /// </summary>
        private enum FileType
        {
            JavaScript,
            Stylesheet
        }

        /// <summary>The images destination directory name.</summary>
        private const string ImagesDestinationDirectoryName = "i";

        /// <summary>
        /// the folder name of where the js files will be stored.
        /// </summary>
        private const string JsDestinationDirectoryName = "js";

        /// <summary>
        /// directory where final css files are stored
        /// </summary>
        private const string CssDestinationDirectoryName = "css";

        /// <summary>The tools temp directory name.</summary>
        private const string ToolsTempDirectoryName = "ToolsTemp";

        /// <summary>The static assembler directory name.</summary>
        private const string StaticAssemblerDirectoryName = "StaticAssemblerOutput";

        /// <summary>The pre processing directory name.</summary>
        private const string PreprocessingTempDirectory = "PreCompiler";

        /// <summary>The directory for resolved resources.</summary>
        private const string ResourcesDestinationDirectoryName = "Resources";

        /// <summary>The directory for theme resources.</summary>
        private const string ThemesDestinationDirectoryName = "Themes";

        /// <summary>The directory for locale resources.</summary>
        private const string LocalesDestinationDirectoryName = "Locales";

        /// <summary>
        /// folder where non-image files will be placed prior to hashing.
        /// </summary>
        private const string PreHashDirectoryName = "PreHashOutput";

        /// <summary>The source directory.</summary>
        private readonly string _sourceDirectory;

        /// <summary>The destination directory.</summary>
        private readonly string _destinationDirectory;

        /// <summary>
        /// the web application root path.
        /// </summary>
        private readonly string _applicationRootDirectory;

        /// <summary>
        /// the plugin directory.
        /// </summary>
        private readonly string _pluginDirectory;

        /// <summary>The tools temp directory.</summary>
        private readonly string _toolsTempDirectory;

        /// <summary>The static assembler directory.</summary>
        private readonly string _staticAssemblerDirectory;

        /// <summary>The log directory.</summary>
        private readonly string _logDirectory;

        /// <summary>The images destination directory.</summary>
        private readonly string _imagesDestinationDirectory;

        /// <summary>The pre processing temp directory.</summary>
        private readonly string _preprocessingTempDirectory;

        /// <summary>
        /// the temp working folder for images (for css hash/sprite resolution).
        /// </summary>
        private readonly string _imagesTempWorkDirectory;

        /// <summary>The themes destination directory.</summary>
        private readonly string _themesDestinationDirectory;

        /// <summary>The locales destination directory.</summary>
        private readonly string _localesDestinationDirectory;

        /// <summary>The images log file.</summary>
        private readonly string _imagesLogFile;

        /// <summary>The web grease configuration root.</summary>
        private readonly WebGreaseConfiguration _webGreaseConfig;

        /// <summary>
        /// Named config settings to use (if provided), otherwise the first set is used.
        /// </summary>
        private readonly string _configName;

        /// <summary>
        /// Action used to write out information messages
        /// </summary>
        private readonly Action<string> _logInformation;

        /// <summary>
        /// Action used to record exception information.
        /// </summary>
        private readonly LogError _logError;

        /// <summary>
        /// Action used to record extended exception information.
        /// </summary>
        private readonly LogExtendedError _logExtended;

        /// <summary>Initializes a new instance of the <see cref="EverythingActivity"/> class.</summary>
        /// <param name="config">The web grease configuration root.</param>
        /// <param name="logInformation">The list of log information actions.</param>
        /// <param name="logError">The error log delegate </param>
        /// <param name="logExtendedError">The extended error log delegate </param>
        /// <param name="configName">(Optional) Named config settings to used. If left blank, the first group will be used. </param>
        /// <param name="pluginDirectory">(optional) The plugin directory.</param>
        internal EverythingActivity(WebGreaseConfiguration config, Action<string> logInformation = null, LogError logError = null, LogExtendedError logExtendedError = null, string configName = null, string pluginDirectory = null)
        {
            Contract.Requires(config != null);

            // Assuming we get a validated WebGreaseConfiguration object here.
            _webGreaseConfig = config;
            _sourceDirectory = config.SourceDirectory;
            _destinationDirectory = config.DestinationDirectory;
            _logDirectory = config.LogsDirectory;
            _toolsTempDirectory = Path.Combine(_logDirectory, ToolsTempDirectoryName);
            _imagesLogFile = Path.Combine(_logDirectory, Strings.ImagesLogFile);
            _imagesTempWorkDirectory = Path.Combine(_toolsTempDirectory, ImagesDestinationDirectoryName);
            _imagesDestinationDirectory = Path.Combine(_destinationDirectory, ImagesDestinationDirectoryName);
            _preprocessingTempDirectory = Path.Combine(_toolsTempDirectory, PreprocessingTempDirectory);
            _staticAssemblerDirectory = Path.Combine(_toolsTempDirectory, StaticAssemblerDirectoryName);
            _themesDestinationDirectory = Path.Combine(_toolsTempDirectory, Path.Combine(ResourcesDestinationDirectoryName, ThemesDestinationDirectoryName));
            _localesDestinationDirectory = Path.Combine(_toolsTempDirectory, Path.Combine(ResourcesDestinationDirectoryName, LocalesDestinationDirectoryName));
            _configName = configName;
            _logInformation = logInformation ?? ((s) => { });
            _logError = logError ?? ((e, m, f) => { });
            _logExtended = logExtendedError ?? ((m1, m2, m3, m4, m5, m6, m7, m8, m9) => { });
            _applicationRootDirectory = config.ApplicationRootDirectory;
            _pluginDirectory = pluginDirectory;
        }

        /// <summary>The main execution point.</summary>
        internal bool Execute()
        {
            var jsExpandedResourcesPath = Path.Combine(_localesDestinationDirectory, Strings.JS);
            var cssThemesOutputPath = Path.Combine(_themesDestinationDirectory, Strings.Css);
            var cssLocalesOutputPath = Path.Combine(_localesDestinationDirectory, Strings.Css);
            var localizedCssOutputPath = Path.Combine(_toolsTempDirectory, Strings.CssLocalizedOutput);
            var jsLocalizedOutputPath = Path.Combine(_toolsTempDirectory, Strings.JsLocalizedOutput);
            var jsLogPath = Path.Combine(_webGreaseConfig.LogsDirectory, Strings.JsLogFile);
            var cssLogPath = Path.Combine(_webGreaseConfig.LogsDirectory, Strings.CssLogFile);

            // where minimized js and css files go to be hashed.
            var hashInputPath = Path.Combine(_toolsTempDirectory, PreHashDirectoryName);

            // final destination of 
            var jsHashOutputPath = Path.Combine(_webGreaseConfig.DestinationDirectory, JsDestinationDirectoryName);
            var cssHashOutputPath = Path.Combine(_webGreaseConfig.DestinationDirectory, CssDestinationDirectoryName);

            bool encounteredError = false;

            // Initialize the preprocessors
            PreprocessingManager.Instance.Initialize(this._logInformation, this._logError, this._pluginDirectory);

            // hash the images
            _logInformation("Renaming (hashing) image files");
            var relativeImgPath = @"../..";
            HashImages(_imagesTempWorkDirectory, relativeImgPath, _toolsTempDirectory, _webGreaseConfig.ImageExtensions);

            // CSS processing pipeline per file set in the config
            _logInformation("Begin CSS file pipeline.");

            foreach (var cssFileSet in _webGreaseConfig.CssFileSets)
            {
                // bundling
                var bundleConfig = WebGreaseConfiguration.GetNamedConfig(cssFileSet.Bundling, _configName);

                IEnumerable<string> localizeInputFiles;
                if (bundleConfig.ShouldBundleFiles)
                {
                    _logInformation("Bundling css files.");
                    var outputFile = Path.Combine(_staticAssemblerDirectory, cssFileSet.Output);
                    if (!BundleFiles(cssFileSet.InputSpecs, outputFile, cssFileSet.Preprocessing, FileType.Stylesheet))
                    {
                        // bundling failed
                        _logError(null, "There were errors encountered while bundling files.");
                        encounteredError = true;
                        continue;
                    }

                    // input for the next step is the output file from bundling
                    localizeInputFiles = new[] { outputFile };
                }
                else
                {
                    // bundling was skipped so the input is the bare input files
                    var preProcesInputFiles = cssFileSet.InputSpecs.SelectMany(inputSpec => GetFiles(inputSpec.Path, inputSpec.SearchPattern, inputSpec.SearchOption));
                    // bundling calls the preprocessor, so we need to do it seperately if there was no bundling.
                    localizeInputFiles = PreprocessFiles(this._preprocessingTempDirectory, preProcesInputFiles, "css", cssFileSet.Preprocessing);
                }

                // localization
                _logInformation("Resolving tokens and performing localization.");
                // Resolve resources and localize the files.
                ResolveCSSResources(cssFileSet, _webGreaseConfig, cssThemesOutputPath, cssLocalesOutputPath);
                if (!LocalizeCss(localizeInputFiles, cssFileSet.Locales, cssFileSet.Themes, localizedCssOutputPath, cssThemesOutputPath, cssLocalesOutputPath, _imagesLogFile))
                {
                    // localization failed for this batch
                    _logError(null, "There were errors encountered while resolving tokens.");
                    encounteredError = true;
                    continue; // skip to next set.
                }

                // if bundling occured, there should be only 1 file to process, otherwise find all the css files.
                string minifySearchMask = bundleConfig.ShouldBundleFiles ? "*" + Path.GetFileName(cssFileSet.Output) : "*." + Strings.Css;

                // minify files
                _logInformation("Minimizing css files, and spriting background images.");

                if (!(MinifyCss(localizedCssOutputPath, hashInputPath, minifySearchMask,
                    WebGreaseConfiguration.GetNamedConfig(cssFileSet.Minification, _configName),
                    WebGreaseConfiguration.GetNamedConfig(cssFileSet.ImageSpriting, _configName), _imagesLogFile)))
                {
                    // minification failed.
                    _logError(null, "There were errors encountered while minimizing the css files.");
                    encounteredError = true;
                    continue; // skip to next set.
                }

            }

            // hash all the css files.
            _logInformation("Renaming css files.");
            if (!HashFiles(hashInputPath, Strings.CssFilter, cssHashOutputPath, cssLogPath))
            {
                _logError(null, "There was a problem encountered while renaming the css files.");
                encounteredError = true;
            }

            // move images from temp folder to final destination
            if (!encounteredError && Directory.Exists(_imagesTempWorkDirectory))
            {
                MoveImagesToFinalDestination(_imagesTempWorkDirectory, _imagesDestinationDirectory);
            }

            // process each js file set.
            foreach (var jsFileSet in _webGreaseConfig.JSFileSets)
            {
                // bundling
                var bundleConfig = WebGreaseConfiguration.GetNamedConfig(jsFileSet.Bundling, _configName);

                IEnumerable<string> localizeInputFiles;
                if (bundleConfig.ShouldBundleFiles)
                {
                    _logInformation("Bundling js files.");
                    var outputFile = Path.Combine(_staticAssemblerDirectory, jsFileSet.Output);
                    // bundle
                    if (!BundleFiles(jsFileSet.InputSpecs, outputFile, jsFileSet.Preprocessing, FileType.JavaScript))
                    {
                        // bundling failed
                        _logError(null, "There were errors encountered while bundling files.");
                        encounteredError = true;
                        continue;
                    }

                    localizeInputFiles = new[] { outputFile };
                }
                else
                {
                    // bundling was skipped so the input is the bare input files
                    var preProcesInputFiles = jsFileSet.InputSpecs.SelectMany(inputSpec => GetFiles(inputSpec.Path, inputSpec.SearchPattern, inputSpec.SearchOption));
                    // bundling has the proprocessor in it, so we need to do it seperately.
                    localizeInputFiles = PreprocessFiles(this._preprocessingTempDirectory, preProcesInputFiles, "js", jsFileSet.Preprocessing);
                }


                // resolve the resources
                ResolveJsResources(jsFileSet, _webGreaseConfig, jsExpandedResourcesPath);
                // localize
                _logInformation("Resolving tokens and performing localization.");
                if (!LocalizeJs(localizeInputFiles, jsFileSet.Locales, jsLocalizedOutputPath, jsExpandedResourcesPath))
                {
                    _logError(null, "There were errors encountered while resolving tokens.");
                    encounteredError = true;
                    continue;
                }

                _logInformation("Minimizing javascript files");
                string minifySearchMask = bundleConfig.ShouldBundleFiles ? "*" + Path.GetFileName(jsFileSet.Output) : Strings.JsFilter;
                if (!MinifyJs(jsLocalizedOutputPath, hashInputPath, minifySearchMask,
                    WebGreaseConfiguration.GetNamedConfig(jsFileSet.Minification, _configName),
                    WebGreaseConfiguration.GetNamedConfig(jsFileSet.Validation, _configName)))
                {
                    _logError(null, "There were errors encountered while minimizing javascript files.");
                    encounteredError = true;
                    continue;
                }
            }

            // hash all the js files
            _logInformation("Renaming javascript files.");
            if (!HashFiles(hashInputPath, Strings.JsFilter, jsHashOutputPath, jsLogPath))
            {
                _logError(null, "There was an error renaming javascript files.");
                encounteredError = true;
            }

            return !encounteredError;
        }

        /// <summary>
        /// Pre processes each file in the inputs list, outputs them into the target folder, using filename.defaultTargetExtensions, or if the same as input extension, .processed.defaultTargetExtensions
        /// </summary>
        /// <param name="targetFolder">Target folder</param>
        /// <param name="inputFiles">Input files</param>
        /// <param name="defaultTargetExtensions">Default target extensions</param>
        /// <param name="preprocessingConfig">The pre processing config </param>
        /// <returns>The preprocessed file</returns>
        private IEnumerable<string> PreprocessFiles(string targetFolder, IEnumerable<string> inputFiles, string defaultTargetExtensions, PreprocessingConfig preprocessingConfig)
        {
            if (preprocessingConfig.Enabled)
            {
                var preprocessorActivity = new PreprocessorActivity
                    {
                        DefaultExtension = defaultTargetExtensions,
                        OutputFolder = targetFolder,
                        PreprocessingConfig = preprocessingConfig,
                        LogInformation = this._logInformation,
                        LogError = this._logError,
                        LogExtendedError = this._logExtended
                    };

                foreach (var inputFile in inputFiles)
                {
                    preprocessorActivity.Inputs.Add(inputFile);
                }

                return preprocessorActivity.Execute();
            }
            return inputFiles;
        }

        /// <summary>
        /// Moves images from the temp folder to their final destination
        /// </summary>
        /// <param name="imagesTempWorkDirectory">temp working folder</param>
        /// <param name="imagesDestinationDirectory">destination folder.</param>
        private static void MoveImagesToFinalDestination(string imagesTempWorkDirectory, string imagesDestinationDirectory)
        {
            foreach (var file in Directory.EnumerateFiles(imagesTempWorkDirectory, "*.*", SearchOption.AllDirectories))
            {
                string relativeImagePath = file.Replace(imagesTempWorkDirectory, string.Empty);
                string destinationFolder = Path.GetDirectoryName(imagesDestinationDirectory + relativeImagePath);

                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                string destinationFile = Path.Combine(destinationFolder, Path.GetFileName(file));

                if (!File.Exists(destinationFile))
                {
                    File.Move(file, Path.Combine(destinationFolder, Path.GetFileName(file)));
                }
            }
        }

        /// <summary>
        /// Minifies css files.
        /// </summary>
        /// <param name="rootInputPath">Path to look in for css files.</param>
        /// <param name="outputPath">The output path </param>
        /// <param name="searchFilter">filter to qualify files</param>
        /// <param name="cssConfig">configuration settings</param>
        /// <param name="spriteConfig">The sprite configuration </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private bool MinifyCss(string rootInputPath, string outputPath, string searchFilter, CssMinificationConfig cssConfig, CssSpritingConfig spriteConfig, string imagesLogFile)
        {
            bool successful = true;
            var minifier = new MinifyCssActivity
                               {
                                   ShouldAssembleBackgroundImages = spriteConfig.ShouldAutoSprite,
                                   ShouldMinify = cssConfig.ShouldMinify,
                                   ShouldOptimize = cssConfig.ShouldMinify,
                                   ShouldValidateForLowerCase = cssConfig.ShouldValidateLowerCase,
                                   ShouldExcludeProperties = cssConfig.ShouldExcludeProperties,
                                   BannedSelectors = new HashSet<string>(cssConfig.RemoveSelectors.ToArray()),
                                   HackSelectors = new HashSet<string>(cssConfig.ForbiddenSelectors.ToArray()),
                                   ImageAssembleReferencesToIgnore = new HashSet<string>(spriteConfig.ImagesToIgnore.ToArray()),
                                   HashedImagesLogFile = imagesLogFile,
                                   OutputUnit = spriteConfig.OutputUnit,
                                   OutputUnitFactor = spriteConfig.OutputUnitFactor,
                                   IgnoreImagesWithNonDefaultBackgroundSize = spriteConfig.IgnoreImagesWithNonDefaultBackgroundSize
                               };

            _logInformation(string.Format(CultureInfo.InvariantCulture, "MinifyCSS Called --> rootInputPath:{0}, searchFilter:{1}, configName:{2}, excludeSelectors:{3},  hackSelectors:{4}, shouldMinify:{5}, shouldValidateLowerCase: {6}, shouldExcludeProperties:{7}", rootInputPath, searchFilter, cssConfig.Name, string.Join(",", minifier.BannedSelectors), string.Join(",", minifier.HackSelectors), minifier.ShouldMinify, minifier.ShouldValidateForLowerCase, minifier.ShouldExcludeProperties));
            foreach (var file in Directory.EnumerateFiles(rootInputPath, searchFilter, SearchOption.AllDirectories))
            {
                _logInformation("Css Minify start: " + file);
                var workingFolder = Path.GetDirectoryName(file);

                // This is to pull the locale value from the path... in the current pipeline this is given to be present as the last portion of the path is the locale
                // TODO: refactor locales/themes into a generic matrix
                string locale = Directory.GetParent(file).Name;
                minifier.SourceFile = file;
                var outputFile = Path.Combine(outputPath, locale, CssDestinationDirectoryName, Path.GetFileNameWithoutExtension(file) + "." + Strings.Css);
                var scanFilePath = Path.Combine(workingFolder, Path.GetFileNameWithoutExtension(file) + ".scan." + Strings.Css);
                var updateFilePath = Path.Combine(workingFolder, Path.GetFileNameWithoutExtension(file) + ".update." + Strings.Css);
                minifier.ImageAssembleScanDestinationFile = scanFilePath;
                minifier.ImageAssembleUpdateDestinationFile = updateFilePath;
                minifier.ImagesOutputDirectory = _imagesTempWorkDirectory;
                minifier.DestinationFile = outputFile;

                try
                {
                    minifier.Execute();
                }
                catch (Exception ex)
                {
                    successful = false;
                    AggregateException aggEx;

                    if (ex.InnerException != null && (aggEx = ex.InnerException as AggregateException) != null)
                    {
                        // antlr can throw a blob of errors, so they need to be deduped to get the real set of errors
                        IEnumerable<BuildWorkflowException> errors = aggEx.CreateBuildErrors(file);
                        foreach (var error in errors)
                        {
                            HandleError(error);
                        }
                    }
                    else
                    {
                        // Catch, record and display error
                        HandleError(ex, file);
                    }
                }
            }

            return successful;
        }


        /// <summary>
        /// Hashes a selection of files in the input path, and copies them to the output folder.
        /// </summary>
        /// <param name="inputPath">Starting paths to start looking for files. Subfolders will be processed</param>
        /// <param name="outputPath">Path to copy the output.</param>
        /// <param name="filter">Filter for the files to be included.</param>
        /// <param name="logFileName">log path for log data</param>
        private bool HashFiles(string inputPath, string filter, string outputPath, string logFileName)
        {
            bool success = true;
            var hasher = new FileHasherActivity()
                             {
                                 CreateExtraDirectoryLevelFromHashes = true,
                                 DestinationDirectory = outputPath,
                                 FileTypeFilter = filter,
                                 LogFileName = logFileName,
                                 BasePrefixToRemoveFromInputPathInLog = inputPath,
                                 BasePrefixToRemoveFromOutputPathInLog = _applicationRootDirectory
                             };

            hasher.SourceDirectories.Add(inputPath);

            try
            {
                hasher.Execute();
            }
            catch (Exception ex)
            {
                HandleError(ex);
                success = false;
            }

            return success;
        }

        /// <summary>Minify js activity</summary>
        /// <param name="inputPath">path to localized js files to be minified</param>
        /// <param name="outputPath"> </param>
        /// <param name="searchFilter"> </param>
        /// <param name="jsConfig"> </param>
        /// <param name="jsValidateConfig"> </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private bool MinifyJs(string inputPath, string outputPath, string searchFilter, JsMinificationConfig jsConfig, JSValidationConfig jsValidateConfig)
        {
            var success = true;
            var minifier = new MinifyJSActivity()
            {
                LogExtendedError = _logExtended
            };

            // if we specified some globals to ignore, format them on the command line with the
            // other minification arguments
            if (!string.IsNullOrWhiteSpace(jsConfig.GlobalsToIgnore))
            {
                minifier.MinifyArgs = Strings.GlobalsToIgnoreArg + jsConfig.GlobalsToIgnore + ' ' + jsConfig.MinificationArugments;
            }
            else
            {
                minifier.MinifyArgs = jsConfig.MinificationArugments;
            }

            minifier.ShouldMinify = jsConfig.ShouldMinify;
            minifier.ShouldAnalyze = jsValidateConfig.ShouldAnalyze;
            minifier.AnalyzeArgs = jsValidateConfig.AnalyzeArguments;

            foreach (var file in Directory.EnumerateFiles(inputPath, searchFilter, SearchOption.AllDirectories))
            {
                minifier.SourceFile = file;

                // This is to pull the locale value from the path... in the current pipeline this is given to be present as the last portion of the path is the locale
                // TODO: refactor locales/themes into a generic matrix
                string locale = Directory.GetParent(file).Name;
                var outputFile = Path.Combine(outputPath, locale, JsDestinationDirectoryName, Path.GetFileNameWithoutExtension(file) + "." + Strings.JS);
                minifier.DestinationFile = outputFile;
                try
                {
                    minifier.Execute();
                }
                catch (Exception ex)
                {
                    HandleError(ex, file);
                    success = false;
                    continue;
                }
            }

            return success;
        }

        /// <summary>Localize the js files based on the expanded resource tokens for locales</summary>
        /// <param name="locales">A collection of locale codes</param>
        /// <param name="jsLocalizedOutputPath">path for output</param>
        /// <param name="jsExpandedResourcesPath">path for resources to use</param>
        /// <param name="jsFiles">the files to resolve tokens in.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private bool LocalizeJs(IEnumerable<string> jsFiles, IEnumerable<string> locales, string jsLocalizedOutputPath, string jsExpandedResourcesPath)
        {
            var success = true;
            var jsLocalizer = new JSLocalizationActivity() { DestinationDirectory = jsLocalizedOutputPath, ResourcesDirectory = jsExpandedResourcesPath };
            foreach (var jsFile in jsFiles)
            {
                jsLocalizer.JsLocalizationInputs.Clear();

                var jsInput = new JSLocalizationInput();
                foreach (var locale in locales)
                {
                    jsInput.Locales.Add(locale);
                }


                jsInput.SourceFile = jsFile;
                jsInput.DestinationFile = Path.GetFileNameWithoutExtension(jsFile);

                jsLocalizer.JsLocalizationInputs.Add(jsInput);

                try
                {
                    jsLocalizer.Execute();
                }
                catch (Exception ex)
                {
                    HandleError(ex, jsFile);
                    success = false;
                    continue;
                }
            }

            return success;
        }

        /// <summary>Localize the css files based on the expanded resource tokens for locales and themes</summary>
        /// <param name="themes">A collection of theme names to base resources on.</param>
        /// <param name="localizedCssOutputPath">path for output</param>
        /// <param name="cssThemesOutputPath">path to css themes resources</param>
        /// <param name="cssFiles">The css files to localize</param>
        /// <param name="locales">A collection of locale codes to localize for</param>
        /// <param name="cssLocalesOutputPath"> </param>
        /// <param name="imageLogPath"> </param>
        private bool LocalizeCss(IEnumerable<string> cssFiles, IEnumerable<string> locales, IEnumerable<string> themes, string localizedCssOutputPath, string cssThemesOutputPath, string cssLocalesOutputPath, string imageLogPath)
        {
            bool result = true;

            var cssLocalizer = new CssLocalizationActivity()
            {
                DestinationDirectory = localizedCssOutputPath,
                LocalesResourcesDirectory = cssLocalesOutputPath,
                ThemesResourcesDirectory = cssThemesOutputPath,
                HashedImagesLogFile = imageLogPath
            };

            foreach (var cssFile in cssFiles)
            {
                // create new one and add locales and themes, set output based on current fileset
                var localizationInput = new CssLocalizationInput();

                foreach (var loc in locales)
                {
                    localizationInput.Locales.Add(loc);
                }

                foreach (var theme in themes)
                {
                    localizationInput.Themes.Add(theme);
                }

                localizationInput.SourceFile = cssFile;

                cssLocalizer.CssLocalizationInputs.Add(localizationInput);
                localizationInput.DestinationFile = Path.GetFileNameWithoutExtension(cssFile);

                try
                {
                    cssLocalizer.Execute();
                }
                catch (Exception ex)
                {
                    HandleError(ex, cssFile);
                    result = false; // mark that this step did not succeed.
                }
            }

            return result;
        }

        /// <summary>
        /// Combine files discovered through the input specs into the output file
        /// </summary>
        /// <param name="inputSpecs">A collection of files to be processed</param>
        /// <param name="outputFile">name of the output file</param>
        /// <param name="preprocessing"> </param>
        /// <param name="fileType">JavaScript of Stylesheets</param>
        /// <returns>a value indicating whether the operation was successful</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private bool BundleFiles(IEnumerable<InputSpec> inputSpecs, string outputFile, PreprocessingConfig preprocessing, FileType fileType)
        {
            // now we have the input prepared, so use Assembler activity to create the one file to use as input (if we were't assembling, we'd need to grab all) 
            // we are bundling either JS or CSS files -- for JS files we want to append semicolons between them and use single-line comments; for CSS file we don't.
            var assemblerActivity = new AssemblerActivity
                {
                    PreprocessingConfig = preprocessing,
                    logInformation = this._logInformation,
                    logError = (e,s1,s2) => this._logError(e,s1,s2),
                    logExtendedError = this._logExtended,
                    AddSemicolons = fileType == FileType.JavaScript,
                };

            foreach (var inputSpec in inputSpecs)
            {
                assemblerActivity.Inputs.Add(inputSpec);
            }

            assemblerActivity.OutputFile = outputFile;

            try
            {
                assemblerActivity.Execute();
            }
            catch (Exception ex)
            {
                // catch/record/display error.
                HandleError(ex);
                return false;
            }

            return true;
        }

        /// <summary>Expands the resource tokens for locales and themes</summary>
        /// <param name="cssFileSet">The file set to be processed</param>
        /// <param name="wgConfig">Config object with locations of needed directories.</param>
        /// <param name="cssThemesOutputPath">path for output of css theme resources</param>
        /// <param name="cssLocalesOutputPath">path for output of css locale resources</param>
        private static void ResolveCSSResources(CssFileSet cssFileSet, WebGreaseConfiguration wgConfig, string cssThemesOutputPath, string cssLocalesOutputPath)
        {

            var themeResourceActivity = new ResourcesResolutionActivity()
                                            {
                                                DestinationDirectory = cssThemesOutputPath,
                                                SourceDirectory = wgConfig.SourceDirectory,
                                                ApplicationDirectoryName = wgConfig.TokensDirectory,
                                                SiteDirectoryName = wgConfig.OverrideTokensDirectory,
                                                ResourceTypeFilter = ResourceType.Themes,
                                            };

            foreach (var theme in cssFileSet.Themes)
            {
                themeResourceActivity.ResourceKeys.Add(theme);
            }

            themeResourceActivity.Execute();

            var localeResourceActivity = new ResourcesResolutionActivity()
                                             {
                                                 DestinationDirectory = cssLocalesOutputPath,
                                                 SourceDirectory = wgConfig.SourceDirectory,
                                                 ApplicationDirectoryName = wgConfig.TokensDirectory,
                                                 SiteDirectoryName = wgConfig.OverrideTokensDirectory,
                                                 ResourceTypeFilter = ResourceType.Locales
                                             };

            foreach (var locale in cssFileSet.Locales)
            {
                localeResourceActivity.ResourceKeys.Add(locale);
            }

            localeResourceActivity.Execute();
        }

        /// <summary>Expands the resource tokens for locales and themes</summary>
        /// <param name="wgConfig"> </param>
        /// <param name="jsExpandedResourcesPath">path for output of js resources</param>
        /// <param name="jsFileSet"> </param>
        private static void ResolveJsResources(JSFileSet jsFileSet, WebGreaseConfiguration wgConfig, string jsExpandedResourcesPath)
        {
            var jsLocaleResourceActivity = new ResourcesResolutionActivity()
            {
                DestinationDirectory = jsExpandedResourcesPath,
                SourceDirectory = wgConfig.SourceDirectory,
                ApplicationDirectoryName = wgConfig.TokensDirectory,
                SiteDirectoryName = wgConfig.OverrideTokensDirectory,
                ResourceTypeFilter = ResourceType.Locales
            };

            foreach (var locale in jsFileSet.Locales)
            {
                jsLocaleResourceActivity.ResourceKeys.Add(locale);
            }

            jsLocaleResourceActivity.Execute();

        }

        /// <summary>Hashes the images.</summary>
        private void HashImages(string fullOutputDirectory, string relativePathPrefix, string outputPath, IList<string> fileFilters)
        {
            if (_webGreaseConfig.ImageDirectories.Count <= 0)
            {
                return;
            }

            var fileHasherActivity = new FileHasherActivity()
            {
                DestinationDirectory = fullOutputDirectory,
                BasePrefixToAddToOutputPath = relativePathPrefix,
                BasePrefixToRemoveFromInputPathInLog = _sourceDirectory,
                BasePrefixToRemoveFromOutputPathInLog = outputPath,
                CreateExtraDirectoryLevelFromHashes = true,
                ShouldPreserveSourceDirectoryStructure = false,
                LogFileName = _imagesLogFile
            };

            if (fileFilters != null && fileFilters.Any())
            {
                fileHasherActivity.FileTypeFilter = string.Join(new string(Strings.FileFilterSeparator), fileFilters.ToArray());
            }

            foreach (var imageDirectory in _webGreaseConfig.ImageDirectories)
            {
                fileHasherActivity.SourceDirectories.Add(imageDirectory);
            }

            fileHasherActivity.Execute();
        }

        /// <summary>
        /// general handler for errors
        /// </summary>
        /// <param name="ex">exception caught</param>
        /// <param name="file">File being processed that caused the error.</param>
        /// <param name="message">message to be shown (instead of Exception.Message)</param>
        private void HandleError(Exception ex, string file = null, string message = null)
        {
            if (ex.InnerException != null && ex.InnerException is BuildWorkflowException)
            {
                ex = ex.InnerException;
            }

            if (!string.IsNullOrWhiteSpace(file))
            {
                this._logError(null, string.Format(CultureInfo.InvariantCulture, ResourceStrings.ErrorsInFileFormat, file), file);
            }

            _logError(ex, message);

        }

        /// <summary>
        /// Gets the collection of files that match the path and filter.
        /// </summary>
        /// <param name="inputPath">Input path to search. Can be a filename, which will be the only member of the returned set.</param>
        /// <param name="searchPattern">Pattern to match for results.</param>
        /// <param name="searchOption">Directory processing option</param>
        /// <returns>A collection of matching files.</returns>
        private static IEnumerable<string> GetFiles(string inputPath, string searchPattern, SearchOption searchOption)
        {
            if (!Path.GetExtension(inputPath).IsNullOrWhitespace())
            {
                // path is a file
                return new[] { inputPath };
            }

            // path is a folder
            return Directory.EnumerateFiles(inputPath, searchPattern, searchOption);
        }
    }
}
