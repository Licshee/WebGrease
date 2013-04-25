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
        private readonly string sourceDirectory;

        /// <summary>The destination directory.</summary>
        private readonly string destinationDirectory;

        /// <summary>
        /// the web application root path.
        /// </summary>
        private readonly string applicationRootDirectory;

        /// <summary>The tools temp directory.</summary>
        private readonly string toolsTempDirectory;

        /// <summary>The static assembler directory.</summary>
        private readonly string staticAssemblerDirectory;

        /// <summary>The log directory.</summary>
        private readonly string logDirectory;

        /// <summary>The images destination directory.</summary>
        private readonly string imagesDestinationDirectory;

        /// <summary>The pre processing temp directory.</summary>
        private readonly string preprocessingTempDirectory;

        /// <summary>
        /// the temp working folder for images (for css hash/sprite resolution).
        /// </summary>
        private readonly string imagesTempWorkDirectory;

        /// <summary>The themes destination directory.</summary>
        private readonly string themesDestinationDirectory;

        /// <summary>The locales destination directory.</summary>
        private readonly string localesDestinationDirectory;

        /// <summary>The images log file.</summary>
        private readonly string imagesLogFile;

        /// <summary>The web grease configuration root.</summary>
        private readonly IWebGreaseContext context;

        /// <summary>Initializes a new instance of the <see cref="EverythingActivity"/> class.</summary>
        /// <param name="context">The context.</param>
        internal EverythingActivity(IWebGreaseContext context)
        {
            Contract.Requires(context != null);

            // Assuming we get a validated WebGreaseConfiguration object here.
            this.context = context;
            this.sourceDirectory = context.Configuration.SourceDirectory;
            this.destinationDirectory = context.Configuration.DestinationDirectory;
            this.logDirectory = context.Configuration.LogsDirectory;
            this.toolsTempDirectory = context.Configuration.ToolsTempDirectory.IsNullOrWhitespace()
                ? Path.Combine(context.Configuration.LogsDirectory, ToolsTempDirectoryName)
                : context.Configuration.ToolsTempDirectory;
            this.imagesLogFile = Path.Combine(this.logDirectory, Strings.ImagesLogFile);
            this.imagesTempWorkDirectory = Path.Combine(this.toolsTempDirectory, ImagesDestinationDirectoryName);
            this.imagesDestinationDirectory = Path.Combine(this.destinationDirectory, ImagesDestinationDirectoryName);
            this.preprocessingTempDirectory = Path.Combine(this.toolsTempDirectory, PreprocessingTempDirectory);
            this.staticAssemblerDirectory = Path.Combine(this.toolsTempDirectory, StaticAssemblerDirectoryName);
            this.themesDestinationDirectory = Path.Combine(this.toolsTempDirectory, Path.Combine(ResourcesDestinationDirectoryName, ThemesDestinationDirectoryName));
            this.localesDestinationDirectory = Path.Combine(this.toolsTempDirectory, Path.Combine(ResourcesDestinationDirectoryName, LocalesDestinationDirectoryName));
            this.applicationRootDirectory = context.Configuration.ApplicationRootDirectory;
        }

        /// <summary>Gets or sets a value indicating whether skip hash images.</summary>
        internal bool SkipHashImages { get; set; }

        /// <summary>The main execution point.</summary>
        /// <returns>If it failed or succeeded.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "RTUIT: Next iteration move css and js to seperate methods, for keep it to not change too much.")]
        internal bool Execute()
        {
            this.context.Measure.Start("EverythingActivity", "Css");
            var jsExpandedResourcesPath = Path.Combine(this.localesDestinationDirectory, Strings.JS);
            var cssThemesOutputPath = Path.Combine(this.themesDestinationDirectory, Strings.Css);
            var cssLocalesOutputPath = Path.Combine(this.localesDestinationDirectory, Strings.Css);
            var localizedCssOutputPath = Path.Combine(this.toolsTempDirectory, Strings.CssLocalizedOutput);
            var jsLocalizedOutputPath = Path.Combine(this.toolsTempDirectory, Strings.JsLocalizedOutput);
            var jsLogPath = Path.Combine(this.context.Configuration.LogsDirectory, Strings.JsLogFile);
            var cssLogPath = Path.Combine(this.context.Configuration.LogsDirectory, Strings.CssLogFile);

            // where minimized js and css files go to be hashed.
            var hashInputPath = Path.Combine(this.toolsTempDirectory, PreHashDirectoryName);

            // final destination of 
            var jsHashOutputPath = Path.Combine(this.context.Configuration.DestinationDirectory, JsDestinationDirectoryName);
            var cssHashOutputPath = Path.Combine(this.context.Configuration.DestinationDirectory, CssDestinationDirectoryName);

            bool encounteredError = false;

            if (this.context.Configuration.CssFileSets.Any())
            {
                if (!this.SkipHashImages)
                {
                    // hash the images
                    this.context.Log.Information("Renaming (hashing) image files");
                    const string RelativeImgPath = @"../..";

                    this.HashImages(this.imagesTempWorkDirectory, RelativeImgPath, this.toolsTempDirectory, this.context.Configuration.ImageExtensions);
                }

                // CSS processing pipeline per file set in the config
                this.context.Log.Information("Begin CSS file pipeline.");

                foreach (var cssFileSet in this.context.Configuration.CssFileSets)
                {
                    var outputFile = Path.Combine(this.staticAssemblerDirectory, cssFileSet.Output);
                    this.context.Measure.Start(TimeMeasureNames.CssFileSet);

                    try
                    {
                        // bundling
                        var bundleConfig = WebGreaseConfiguration.GetNamedConfig(cssFileSet.Bundling, this.context.Configuration.ConfigType);

                        IEnumerable<string> localizeInputFiles;
                        if (bundleConfig.ShouldBundleFiles)
                        {
                            this.context.Log.Information("Bundling css files.");
                            if (!this.BundleFiles(cssFileSet.InputSpecs, outputFile, cssFileSet.Preprocessing, FileType.Stylesheet))
                            {
                                // bundling failed
                                this.context.Log.Error(null, "There were errors encountered while bundling files.");
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
                            localizeInputFiles = this.PreprocessFiles(this.preprocessingTempDirectory, preProcesInputFiles, "css", cssFileSet.Preprocessing);
                        }

                        // localization
                        this.context.Log.Information("Resolving tokens and performing localization.");

                        // Resolve resources and localize the files.
                        ResolveCSSResources(cssFileSet, this.context, cssThemesOutputPath, cssLocalesOutputPath);
                        if (!this.LocalizeCss(localizeInputFiles, cssFileSet.Locales, cssFileSet.Themes, localizedCssOutputPath, cssThemesOutputPath, cssLocalesOutputPath, this.imagesLogFile))
                        {
                            // localization failed for this batch
                            this.context.Log.Error(null, "There were errors encountered while resolving tokens.");
                            encounteredError = true;
                            continue; // skip to next set.
                        }

                        // if bundling occured, there should be only 1 file to process, otherwise find all the css files.
                        string minifySearchMask = bundleConfig.ShouldBundleFiles ? "*" + Path.GetFileName(cssFileSet.Output) : "*." + Strings.Css;

                        // minify files
                        this.context.Log.Information("Minimizing css files, and spriting background images.");

                        if (!this.MinifyCss(
                                localizedCssOutputPath,
                                hashInputPath,
                                minifySearchMask,
                                WebGreaseConfiguration.GetNamedConfig(cssFileSet.Minification, this.context.Configuration.ConfigType),
                                WebGreaseConfiguration.GetNamedConfig(cssFileSet.ImageSpriting, this.context.Configuration.ConfigType)))
                        {
                            // minification failed.
                            this.context.Log.Error(null, "There were errors encountered while minimizing the css files.");
                            encounteredError = true;
                        }
                    }
                    finally
                    {
                        this.context.Measure.End(TimeMeasureNames.CssFileSet);
                    }
                }

                // hash all the css files.
                this.context.Log.Information("Renaming css files.");
                if (!this.HashFiles(hashInputPath, Strings.CssFilter, cssHashOutputPath, cssLogPath))
                {
                    this.context.Log.Error(null, "There was a problem encountered while renaming the css files.");
                    encounteredError = true;
                }

                // move images from temp folder to final destination
                if (!encounteredError && Directory.Exists(this.imagesTempWorkDirectory))
                {
                    CopyImagesToFinalDestination(this.imagesTempWorkDirectory, this.imagesDestinationDirectory, this.context);
                }
            }

            this.context.Measure.End("EverythingActivity", "Css");

            this.context.Measure.Start("EverythingActivity", "Js");

            if (this.context.Configuration.JSFileSets.Any())
            {
                // process each js file set.
                foreach (var jsFileSet in this.context.Configuration.JSFileSets)
                {
                    this.context.Measure.Start(TimeMeasureNames.JsFileSet);
                    try
                    {
                        // bundling
                        var bundleConfig = WebGreaseConfiguration.GetNamedConfig(jsFileSet.Bundling, this.context.Configuration.ConfigType);

                        IEnumerable<string> localizeInputFiles;
                        if (bundleConfig.ShouldBundleFiles)
                        {
                            this.context.Log.Information("Bundling js files.");
                            var outputFile = Path.Combine(this.staticAssemblerDirectory, jsFileSet.Output);

                            // bundle
                            if (!this.BundleFiles(jsFileSet.InputSpecs, outputFile, jsFileSet.Preprocessing, FileType.JavaScript))
                            {
                                // bundling failed
                                this.context.Log.Error(null, "There were errors encountered while bundling files.");
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
                            localizeInputFiles = this.PreprocessFiles(this.preprocessingTempDirectory, preProcesInputFiles, "js", jsFileSet.Preprocessing);
                        }

                        // resolve the resources
                        ResolveJsResources(jsFileSet, this.context, jsExpandedResourcesPath);

                        // localize
                        this.context.Log.Information("Resolving tokens and performing localization.");
                        if (!this.LocalizeJs(localizeInputFiles, jsFileSet.Locales, jsLocalizedOutputPath, jsExpandedResourcesPath))
                        {
                            this.context.Log.Error(null, "There were errors encountered while resolving tokens.");
                            encounteredError = true;
                            continue;
                        }

                        this.context.Log.Information("Minimizing javascript files");
                        string minifySearchMask = bundleConfig.ShouldBundleFiles ? "*" + Path.GetFileName(jsFileSet.Output) : Strings.JsFilter;
                        if (!this.MinifyJs(
                            jsLocalizedOutputPath,
                            hashInputPath,
                            minifySearchMask,
                            WebGreaseConfiguration.GetNamedConfig(jsFileSet.Minification, this.context.Configuration.ConfigType),
                            WebGreaseConfiguration.GetNamedConfig(jsFileSet.Validation, this.context.Configuration.ConfigType)))
                        {
                            this.context.Log.Error(null, "There were errors encountered while minimizing javascript files.");
                            encounteredError = true;
                        }
                    }
                    finally
                    {
                        this.context.Measure.End(TimeMeasureNames.JsFileSet);
                    }
                }

                // hash all the js files
                this.context.Log.Information("Renaming javascript files.");
                if (!this.HashFiles(hashInputPath, Strings.JsFilter, jsHashOutputPath, jsLogPath))
                {
                    this.context.Log.Error(null, "There was an error renaming javascript files.");
                    encounteredError = true;
                }
            }

            this.context.Measure.End("EverythingActivity", "Js");
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
                var preprocessorActivity = new PreprocessorActivity(this.context)
                    {
                        DefaultExtension = defaultTargetExtensions,
                        OutputFolder = targetFolder,
                        PreprocessingConfig = preprocessingConfig,
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
        /// <param name="context">The webgrease context</param>
        private static void CopyImagesToFinalDestination(string imagesTempWorkDirectory, string imagesDestinationDirectory, IWebGreaseContext context)
        {
            context.Measure.Start(TimeMeasureNames.CopyImagesToFinalDestination);
            try
            {
                foreach (var file in Directory.EnumerateFiles(imagesTempWorkDirectory, "*.*", SearchOption.AllDirectories))
                {
                    var relativeImagePath = file.Replace(imagesTempWorkDirectory, string.Empty);
                    var destinationFolder = Path.GetDirectoryName(imagesDestinationDirectory + relativeImagePath);
                    if (destinationFolder != null && !Directory.Exists(destinationFolder))
                    {
                        Directory.CreateDirectory(destinationFolder);
                    }

                    var destinationFile = Path.Combine(destinationFolder, Path.GetFileName(file));
                    if (!File.Exists(destinationFile))
                    {
                        File.Copy(file, Path.Combine(destinationFolder, Path.GetFileName(file)));
                    }
                }
            }
            finally
            {
                context.Measure.End(TimeMeasureNames.CopyImagesToFinalDestination);
            }
        }

        /// <summary>Minifies css files.</summary>
        /// <param name="rootInputPath">Path to look in for css files.</param>
        /// <param name="outputPath">The output path </param>
        /// <param name="searchFilter">filter to qualify files</param>
        /// <param name="cssConfig">configuration settings</param>
        /// <param name="spriteConfig">The sprite configuration </param>
        /// <returns>True is successfull false if not.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private bool MinifyCss(string rootInputPath, string outputPath, string searchFilter, CssMinificationConfig cssConfig, CssSpritingConfig spriteConfig)
        {
            var successful = true;
            var minifier = new MinifyCssActivity(this.context) 
            {
                ShouldAssembleBackgroundImages = spriteConfig.ShouldAutoSprite,
                ShouldMinify = cssConfig.ShouldMinify,
                ShouldOptimize = cssConfig.ShouldMinify,
                ShouldValidateForLowerCase = cssConfig.ShouldValidateLowerCase,
                ShouldExcludeProperties = cssConfig.ShouldExcludeProperties,
                BannedSelectors = new HashSet<string>(cssConfig.RemoveSelectors.ToArray()),
                HackSelectors = new HashSet<string>(cssConfig.ForbiddenSelectors.ToArray()),
                ImageAssembleReferencesToIgnore = new HashSet<string>(spriteConfig.ImagesToIgnore.ToArray()),
                HashedImagesLogFile = this.imagesLogFile,
                OutputUnit = spriteConfig.OutputUnit,
                OutputUnitFactor = spriteConfig.OutputUnitFactor,
                IgnoreImagesWithNonDefaultBackgroundSize = spriteConfig.IgnoreImagesWithNonDefaultBackgroundSize
            };

            this.context.Log.Information(string.Format(CultureInfo.InvariantCulture, "MinifyCSS Called --> rootInputPath:{0}, searchFilter:{1}, configName:{2}, excludeSelectors:{3},  hackSelectors:{4}, shouldMinify:{5}, shouldValidateLowerCase: {6}, shouldExcludeProperties:{7}", rootInputPath, searchFilter, cssConfig.Name, string.Join(",", minifier.BannedSelectors), string.Join(",", minifier.HackSelectors), minifier.ShouldMinify, minifier.ShouldValidateForLowerCase, minifier.ShouldExcludeProperties));
            foreach (var file in Directory.EnumerateFiles(rootInputPath, searchFilter, SearchOption.AllDirectories))
            {
                this.context.Log.Information("Css Minify start: " + file);
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
                minifier.ImagesOutputDirectory = this.imagesTempWorkDirectory;
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
                        var errors = aggEx.CreateBuildErrors(file);
                        foreach (var error in errors)
                        {
                            this.HandleError(error);
                        }
                    }
                    else
                    {
                        // Catch, record and display error
                        this.HandleError(ex, file);
                    }
                }
            }

            return successful;
        }

        /// <summary>Hashes a selection of files in the input path, and copies them to the output folder.</summary>
        /// <param name="inputPath">Starting paths to start looking for files. Subfolders will be processed</param>
        /// <param name="filter">Filter for the files to be included.</param>
        /// <param name="outputPath">Path to copy the output.</param>
        /// <param name="logFileName">log path for log data</param>
        /// <returns>True if successfull false if not.</returns>
        private bool HashFiles(string inputPath, string filter, string outputPath, string logFileName)
        {
            var success = true;
            var hasher = new FileHasherActivity(this.context) 
            {
                CreateExtraDirectoryLevelFromHashes = true,
                DestinationDirectory = outputPath,
                FileTypeFilter = filter,
                FileTypeName = filter.Trim('*', '.'),
                LogFileName = logFileName,
                BasePrefixToRemoveFromInputPathInLog = inputPath,
                BasePrefixToRemoveFromOutputPathInLog = this.applicationRootDirectory
            };

            hasher.SourceDirectories.Add(inputPath);

            try
            {
                hasher.Execute();
            }
            catch (Exception ex)
            {
                this.HandleError(ex);
                success = false;
            }

            return success;
        }

        /// <summary>Minify js activity</summary>
        /// <param name="inputPath">path to localized js files to be minified</param>
        /// <param name="outputPath">The output Path.</param>
        /// <param name="searchFilter">The search Filter.</param>
        /// <param name="jsConfig">The js Config.</param>
        /// <param name="jsValidateConfig">The js Validate Config.</param>
        /// <returns>True if successfull false if not.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private bool MinifyJs(string inputPath, string outputPath, string searchFilter, JsMinificationConfig jsConfig, JSValidationConfig jsValidateConfig)
        {
            var success = true;
            var minifier = new MinifyJSActivity(this.context);

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
                var locale = Directory.GetParent(file).Name;
                var outputFile = Path.Combine(outputPath, locale, JsDestinationDirectoryName, Path.GetFileNameWithoutExtension(file) + "." + Strings.JS);
                minifier.DestinationFile = outputFile;
                try
                {
                    minifier.Execute();
                }
                catch (Exception ex)
                {
                    this.HandleError(ex, file);
                    success = false;
                }
            }

            return success;
        }

        /// <summary>Localize the js files based on the expanded resource tokens for locales</summary>
        /// <param name="jsFiles">the files to resolve tokens in.</param>
        /// <param name="locales">A collection of locale codes</param>
        /// <param name="jsLocalizedOutputPath">path for output</param>
        /// <param name="jsExpandedResourcesPath">path for resources to use</param>
        /// <returns>True if successfull false if not.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private bool LocalizeJs(IEnumerable<string> jsFiles, IEnumerable<string> locales, string jsLocalizedOutputPath, string jsExpandedResourcesPath)
        {
            var success = true;
            var jsLocalizer = new JSLocalizationActivity(this.context) { DestinationDirectory = jsLocalizedOutputPath, ResourcesDirectory = jsExpandedResourcesPath };
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
                    this.HandleError(ex, jsFile);
                    success = false;
                }
            }

            return success;
        }

        /// <summary>Localize the css files based on the expanded resource tokens for locales and themes</summary>
        /// <param name="cssFiles">The css files to localize</param>
        /// <param name="locales">A collection of locale codes to localize for</param>
        /// <param name="themes">A collection of theme names to base resources on.</param>
        /// <param name="localizedCssOutputPath">path for output</param>
        /// <param name="cssThemesOutputPath">path to css themes resources</param>
        /// <param name="cssLocalesOutputPath">The css Locales Output Path.</param>
        /// <param name="imageLogPath">The image Log Path.</param>
        /// <returns>True if it succeeded, false if it did not</returns>
        private bool LocalizeCss(IEnumerable<string> cssFiles, IEnumerable<string> locales, IEnumerable<string> themes, string localizedCssOutputPath, string cssThemesOutputPath, string cssLocalesOutputPath, string imageLogPath)
        {
            bool result = true;

            var cssLocalizer = new CssLocalizationActivity(this.context)
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
                    this.HandleError(ex, cssFile);
                    result = false; // mark that this step did not succeed.
                }
            }

            return result;
        }

        /// <summary>Combine files discovered through the input specs into the output file</summary>
        /// <param name="inputSpecs">A collection of files to be processed</param>
        /// <param name="outputFile">name of the output file</param>
        /// <param name="preprocessing">The preprocessing.</param>
        /// <param name="fileType">JavaScript of Stylesheets</param>
        /// <returns>a value indicating whether the operation was successful</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private bool BundleFiles(IEnumerable<InputSpec> inputSpecs, string outputFile, PreprocessingConfig preprocessing, FileType fileType)
        {
            // now we have the input prepared, so use Assembler activity to create the one file to use as input (if we were't assembling, we'd need to grab all) 
            // we are bundling either JS or CSS files -- for JS files we want to append semicolons between them and use single-line comments; for CSS file we don't.
            var assemblerActivity = new AssemblerActivity(this.context)
                {
                    PreprocessingConfig = preprocessing,
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
                this.HandleError(ex);
                return false;
            }

            return true;
        }

        /// <summary>Expands the resource tokens for locales and themes</summary>
        /// <param name="cssFileSet">The file set to be processed</param>
        /// <param name="context">Config object with locations of needed directories.</param>
        /// <param name="cssThemesOutputPath">path for output of css theme resources</param>
        /// <param name="cssLocalesOutputPath">path for output of css locale resources</param>
        private static void ResolveCSSResources(CssFileSet cssFileSet, IWebGreaseContext context, string cssThemesOutputPath, string cssLocalesOutputPath)
        {
            var themeResourceActivity = new ResourcesResolutionActivity(context) 
            {
                DestinationDirectory = cssThemesOutputPath,
                SourceDirectory = context.Configuration.SourceDirectory,
                ApplicationDirectoryName = context.Configuration.TokensDirectory,
                SiteDirectoryName = context.Configuration.OverrideTokensDirectory,
                ResourceTypeFilter = ResourceType.Themes,
                MeasureName = TimeMeasureNames.Css
            };

            foreach (var theme in cssFileSet.Themes)
            {
                themeResourceActivity.ResourceKeys.Add(theme);
            }

            themeResourceActivity.Execute();

            var localeResourceActivity = new ResourcesResolutionActivity(context) 
            {
                DestinationDirectory = cssLocalesOutputPath,
                SourceDirectory = context.Configuration.SourceDirectory,
                ApplicationDirectoryName = context.Configuration.TokensDirectory,
                SiteDirectoryName = context.Configuration.OverrideTokensDirectory,
                ResourceTypeFilter = ResourceType.Locales,
                MeasureName = TimeMeasureNames.Css
            };

            foreach (var locale in cssFileSet.Locales)
            {
                localeResourceActivity.ResourceKeys.Add(locale);
            }

            localeResourceActivity.Execute();
        }

        /// <summary>Expands the resource tokens for locales and themes</summary>
        /// <param name="jsFileSet">The js File Set.</param>
        /// <param name="context">The context.</param>
        /// <param name="jsExpandedResourcesPath">path for output of js resources</param>
        private static void ResolveJsResources(JSFileSet jsFileSet, IWebGreaseContext context, string jsExpandedResourcesPath)
        {
            var jsLocaleResourceActivity = new ResourcesResolutionActivity(context)
            {
                DestinationDirectory = jsExpandedResourcesPath,
                SourceDirectory = context.Configuration.SourceDirectory,
                ApplicationDirectoryName = context.Configuration.TokensDirectory,
                SiteDirectoryName = context.Configuration.OverrideTokensDirectory,
                ResourceTypeFilter = ResourceType.Locales,
                MeasureName = TimeMeasureNames.Js
            };

            foreach (var locale in jsFileSet.Locales)
            {
                jsLocaleResourceActivity.ResourceKeys.Add(locale);
            }

            jsLocaleResourceActivity.Execute();
        }

        /// <summary>Hashes the images.</summary>
        /// <param name="fullOutputDirectory">The full Output Directory.</param>
        /// <param name="relativePathPrefix">The relative Path Prefix.</param>
        /// <param name="outputPath">The output Path.</param>
        /// <param name="fileFilters">The file Filters.</param>
        private void HashImages(string fullOutputDirectory, string relativePathPrefix, string outputPath, IList<string> fileFilters)
        {
            if (this.context.Configuration.ImageDirectories.Count <= 0)
            {
                return;
            }

            var fileHasherActivity = new FileHasherActivity(this.context)
            {
                DestinationDirectory = fullOutputDirectory,
                BasePrefixToAddToOutputPath = relativePathPrefix,
                BasePrefixToRemoveFromInputPathInLog = this.sourceDirectory,
                BasePrefixToRemoveFromOutputPathInLog = outputPath,
                CreateExtraDirectoryLevelFromHashes = true,
                ShouldPreserveSourceDirectoryStructure = false,
                FileTypeName = "Image",
                LogFileName = this.imagesLogFile
            };

            if (fileFilters != null && fileFilters.Any())
            {
                fileHasherActivity.FileTypeFilter = string.Join(new string(Strings.FileFilterSeparator), fileFilters.ToArray());
            }

            foreach (var imageDirectory in this.context.Configuration.ImageDirectories)
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
            if (ex.InnerException is BuildWorkflowException)
            {
                ex = ex.InnerException;
            }

            if (!string.IsNullOrWhiteSpace(file))
            {
                this.context.Log.Error(null, string.Format(CultureInfo.InvariantCulture, ResourceStrings.ErrorsInFileFormat, file), file);
            }

            this.context.Log.Error(ex, message);
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
            return !Path.GetExtension(inputPath).IsNullOrWhitespace() 
                ? new[] { inputPath } 
                : Directory.EnumerateFiles(inputPath, searchPattern, searchOption);
        }
    }
}
