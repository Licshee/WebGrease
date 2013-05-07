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

    using WebGrease.Css.Extensions;

    /// <summary>The main activity.</summary>
    internal sealed class EverythingActivity
    {
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
            this.destinationDirectory = context.Configuration.DestinationDirectory;
            this.logDirectory = context.Configuration.LogsDirectory;
            this.toolsTempDirectory = context.Configuration.ToolsTempDirectory.IsNullOrWhitespace()
                ? Path.Combine(context.Configuration.LogsDirectory, ToolsTempDirectoryName)
                : context.Configuration.ToolsTempDirectory;
            this.imagesLogFile = Path.Combine(this.logDirectory, Strings.ImagesLogFile);
            this.imagesDestinationDirectory = Path.Combine(this.destinationDirectory, ImagesDestinationDirectoryName);
            this.preprocessingTempDirectory = Path.Combine(this.toolsTempDirectory, PreprocessingTempDirectory);
            this.staticAssemblerDirectory = Path.Combine(this.toolsTempDirectory, StaticAssemblerDirectoryName);
            this.themesDestinationDirectory = Path.Combine(this.toolsTempDirectory, Path.Combine(ResourcesDestinationDirectoryName, ThemesDestinationDirectoryName));
            this.localesDestinationDirectory = Path.Combine(this.toolsTempDirectory, Path.Combine(ResourcesDestinationDirectoryName, LocalesDestinationDirectoryName));
            this.applicationRootDirectory = context.Configuration.ApplicationRootDirectory;
        }

        /// <summary>The main execution point.</summary>
        /// <param name="fileTypes">The file Types.</param>
        /// <returns>If it failed or succeeded.</returns>
        internal bool Execute(FileTypes fileTypes = FileTypes.All)
        {
            var result = true;
            if (fileTypes.HasFlag(FileTypes.JavaScript))
            {
                result = this.ExecuteJS();
            }

            if (fileTypes.HasFlag(FileTypes.StyleSheet))
            {
                result = this.ExecuteCss();
            }

            return result;
        }

        /// <summary>Gets the image hasher.</summary>
        /// <param name="context">The context.</param>
        /// <param name="imageLogFileName">The image log file name.</param>
        /// <param name="imageDestinationDirectory">The image destination directory.</param>
        /// <param name="sourceDirectory">The source directory.</param>
        /// <returns>The image hasher.</returns>
        private static FileHasherActivity GetImageHasher(IWebGreaseContext context, string imageLogFileName, string imageDestinationDirectory, string sourceDirectory)
        {
            var imageHasherActivity =
                new FileHasherActivity(context)
                    {
                        DestinationDirectory = imageDestinationDirectory,
                        BasePrefixToAddToOutputPath = "../../",
                        BasePrefixToRemoveFromInputPathInLog = context.Configuration.SourceDirectory,
                        BasePrefixToRemoveFromOutputPathInLog = context.Configuration.DestinationDirectory,
                        CreateExtraDirectoryLevelFromHashes = true,
                        SourceDirectory = sourceDirectory,
                        ShouldPreserveSourceDirectoryStructure = false,
                        FileType = FileTypes.Image,
                        FileTypeFilter = string.Join(
                            new string(Strings.FileFilterSeparator),
                            context.Configuration.ImageExtensions),
                        LogFileName = imageLogFileName,
                    };
            return imageHasherActivity;
        }

        /// <summary>Hashes a selection of files in the input path, and copies them to the output folder.</summary>
        /// <param name="context">The context.</param>
        /// <param name="inputPath">Starting paths to start looking for files. Subfolders will be processed</param>
        /// <param name="outputPath">Path to copy the output.</param>
        /// <param name="logFileName">log path for log data</param>
        /// <param name="fileType">The file type</param>
        /// <param name="basePrefixToRemoveFromOutputPathInLog">The base Prefix To Remove From Output Path In Log.</param>
        /// <param name="sourceDirectory">The source Directory.</param>
        /// <returns>True if successfull false if not.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Catch to handle all errors.")]
        private static FileHasherActivity GetFileHasher(IWebGreaseContext context, string inputPath, string outputPath, string logFileName, FileTypes fileType, string basePrefixToRemoveFromOutputPathInLog, string sourceDirectory)
        {
            return new FileHasherActivity(context)
                       {
                           CreateExtraDirectoryLevelFromHashes = true,
                           DestinationDirectory = outputPath,
                           SourceDirectory = sourceDirectory,
                           LogFileName = logFileName,
                           BasePrefixToRemoveFromInputPathInLog = inputPath,
                           FileType = fileType,
                           BasePrefixToRemoveFromOutputPathInLog = basePrefixToRemoveFromOutputPathInLog
                       };
        }

        /// <summary>Expands the resource tokens for locales and themes</summary>
        /// <param name="cssFileSet">The file set to be processed</param>
        /// <param name="context">Config object with locations of needed directories.</param>
        /// <param name="cssThemesOutputPath">path for output of css theme resources</param>
        /// <param name="fileType">The file type</param>
        private static void ResolveThemeResources(CssFileSet cssFileSet, IWebGreaseContext context, string cssThemesOutputPath, FileTypes fileType)
        {
            var themeResourceActivity = new ResourcesResolutionActivity(context)
                                            {
                                                DestinationDirectory = cssThemesOutputPath,
                                                SourceDirectory = context.Configuration.SourceDirectory,
                                                ApplicationDirectoryName = context.Configuration.TokensDirectory,
                                                SiteDirectoryName = context.Configuration.OverrideTokensDirectory,
                                                ResourceTypeFilter = ResourceType.Themes,
                                                FileType = fileType
                                            };

            foreach (var theme in cssFileSet.Themes)
            {
                themeResourceActivity.ResourceKeys.Add(theme);
            }

            themeResourceActivity.Execute();
        }

        /// <summary>Expands the resource tokens for locales and themes</summary>
        /// <param name="fileSet">The js File Set.</param>
        /// <param name="context">The context.</param>
        /// <param name="targetPath">path for output of js resources</param>
        /// <param name="fileType">The file type</param>
        private static void ResolveLocaleResources(IFileSet fileSet, IWebGreaseContext context, string targetPath, FileTypes fileType)
        {
            var localeResourceActivity = new ResourcesResolutionActivity(context)
                                             {
                                                 DestinationDirectory = targetPath,
                                                 SourceDirectory = context.Configuration.SourceDirectory,
                                                 ApplicationDirectoryName = context.Configuration.TokensDirectory,
                                                 SiteDirectoryName = context.Configuration.OverrideTokensDirectory,
                                                 ResourceTypeFilter = ResourceType.Locales,
                                                 FileType = fileType
                                             };

            foreach (var locale in fileSet.Locales)
            {
                localeResourceActivity.ResourceKeys.Add(locale);
            }

            localeResourceActivity.Execute();
        }

        /// <summary>Execute the css pipeline.</summary>
        /// <returns>If it was successfull</returns>
        private bool ExecuteCss()
        {
            var hashInputPath = Path.Combine(this.toolsTempDirectory, PreHashDirectoryName);
            var cssLogPath = Path.Combine(this.context.Configuration.LogsDirectory, Strings.CssLogFile);
            var cssLocalizedOutputPath = Path.Combine(this.toolsTempDirectory, Strings.CssLocalizedOutput);
            var cssThemesOutputPath = Path.Combine(this.themesDestinationDirectory, Strings.Css);
            var cssLocalesOutputPath = Path.Combine(this.localesDestinationDirectory, Strings.Css);
            var cssHashOutputPath = Path.Combine(this.context.Configuration.DestinationDirectory, CssDestinationDirectoryName);

            WebGreaseContext.CleanDirectory(cssLocalizedOutputPath);
            WebGreaseContext.CleanDirectory(cssLocalesOutputPath);
            WebGreaseContext.CleanDirectory(cssThemesOutputPath);

            if (!this.context.Configuration.CssFileSets.Any())
            {
                return true;
            }

            var cssCacheSection = this.context.Cache.BeginSection(
                TimeMeasureNames.EverythingActivity + "." + TimeMeasureNames.Css,
                new
                {
                    this.context.Configuration.CssFileSets,
                    this.context.Configuration.ImageExtensions,
                    this.context.Configuration.SourceDirectory,
                    this.context.Configuration.DestinationDirectory,
                    this.context.Configuration.ConfigType
                });

            try
            {
                if (cssCacheSection.CanBeSkipped())
                {
                    return true;
                }

                this.context.Measure.Start(TimeMeasureNames.EverythingActivity, TimeMeasureNames.Css);
                try
                {
                    bool encounteredError = false;

                    // CSS processing pipeline per file set in the config
                    this.context.Log.Information("Begin CSS file pipeline.");
                    var imageHasher = GetImageHasher(this.context, this.imagesLogFile, this.imagesDestinationDirectory, this.context.Configuration.SourceDirectory);
                    var cssHasher = GetFileHasher(this.context, hashInputPath, cssHashOutputPath, cssLogPath, FileTypes.StyleSheet, this.applicationRootDirectory, this.context.Configuration.ToolsTempDirectory);

                    foreach (var cssFileSet in this.context.Configuration.CssFileSets)
                    {
                        var cssFileSetCacheSection = this.context.Cache.BeginSection(
                            TimeMeasureNames.CssFileSet,
                            new
                            {
                                cssFileSet,
                                this.context.Configuration.ConfigType
                            });

                        try
                        {
                            if (cssFileSetCacheSection.CanBeSkipped())
                            {
                                continue;
                            }

                            this.context.Measure.Start(TimeMeasureNames.CssFileSet);

                            var outputFile = Path.Combine(this.staticAssemblerDirectory, cssFileSet.Output);

                            try
                            {
                                var localizedInputFiles = this.Bundle(cssFileSet, outputFile, FileTypes.StyleSheet, this.context.Configuration.ConfigType);
                                if (localizedInputFiles == null)
                                {
                                    encounteredError = true;
                                    continue;
                                }

                                // localization
                                this.context.Log.Information("Resolving tokens and performing localization.");

                                // Resolve resources
                                ResolveLocaleResources(cssFileSet, this.context, cssLocalesOutputPath, FileTypes.StyleSheet);
                                ResolveThemeResources(cssFileSet, this.context, cssThemesOutputPath, FileTypes.StyleSheet);

                                WebGreaseContext.CleanDirectory(cssLocalizedOutputPath);

                                // Localize the css
                                if (!this.LocalizeCss(
                                    localizedInputFiles,
                                    cssFileSet.Locales,
                                    cssFileSet.Themes,
                                    cssLocalizedOutputPath,
                                    cssThemesOutputPath,
                                    cssLocalesOutputPath,
                                    this.imagesLogFile))
                                {
                                    // localization failed for this batch
                                    this.context.Log.Error(null, "There were errors encountered while resolving tokens.");
                                    encounteredError = true;
                                    continue; // skip to next set.
                                }

                                // if bundling occured, there should be only 1 file to process, otherwise find all the css files.
                                var minifySearchMask = localizedInputFiles.Count() == 1
                                                              ? "*" + Path.GetFileName(cssFileSet.Output)
                                                              : "*." + Strings.Css;

                                // minify files
                                this.context.Log.Information("Minimizing css files, and spriting background images.");
                                var minifyResults = this.MinifyCss(
                                    cssLocalizedOutputPath,
                                    hashInputPath,
                                    minifySearchMask,
                                    WebGreaseConfiguration.GetNamedConfig(cssFileSet.Minification, this.context.Configuration.ConfigType),
                                    WebGreaseConfiguration.GetNamedConfig(cssFileSet.ImageSpriting, this.context.Configuration.ConfigType),
                                    imageHasher,
                                    cssHasher);

                                if (!minifyResults)
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

                            if (!encounteredError)
                            {
                                cssFileSetCacheSection.Store();
                            }
                        }
                        finally
                        {
                            cssFileSetCacheSection.EndSection();
                        }
                    }

                    if (!encounteredError)
                    {
                        imageHasher.Save();
                        cssHasher.Save();
                        cssCacheSection.Store();
                    }

                    return !encounteredError;
                }
                finally
                {
                    this.context.Measure.End(TimeMeasureNames.EverythingActivity, TimeMeasureNames.Css);
                }
            }
            finally
            {
                cssCacheSection.EndSection();
            }
        }

        /// <summary>Execute the javascript pipeline.</summary>
        /// <returns>If it was successfull</returns>
        private bool ExecuteJS()
        {
            var hashInputPath = Path.Combine(this.toolsTempDirectory, PreHashDirectoryName);
            var jsLogPath = Path.Combine(this.context.Configuration.LogsDirectory, Strings.JsLogFile);
            var jsLocalizedOutputPath = Path.Combine(this.toolsTempDirectory, Strings.JsLocalizedOutput);
            var jsLocalesOutputPath = Path.Combine(this.localesDestinationDirectory, Strings.JS);
            var jsHashOutputPath = Path.Combine(this.context.Configuration.DestinationDirectory, JsDestinationDirectoryName);

            if (!this.context.Configuration.JSFileSets.Any())
            {
                return true;
            }

            // Clean the target temp folders.
            WebGreaseContext.CleanDirectory(jsLocalizedOutputPath);
            WebGreaseContext.CleanDirectory(jsLocalesOutputPath);

            var jsCacheSection = this.context.Cache.BeginSection(
                TimeMeasureNames.EverythingActivity + "." + TimeMeasureNames.Js,
                new
                {
                    this.context.Configuration.JSFileSets,
                    this.context.Configuration.ConfigType,
                    this.context.Configuration.SourceDirectory,
                    this.context.Configuration.DestinationDirectory,
                });

            try
            {
                if (jsCacheSection.CanBeSkipped())
                {
                    return true;
                }

                this.context.Measure.Start(TimeMeasureNames.EverythingActivity, TimeMeasureNames.Js);
                try
                {
                    var encounteredError = false;

                    var jsHasher = GetFileHasher(this.context, hashInputPath, jsHashOutputPath, jsLogPath, FileTypes.JavaScript, this.applicationRootDirectory, this.context.Configuration.ToolsTempDirectory);

                    // process each js file set.
                    foreach (var jsFileSet in this.context.Configuration.JSFileSets)
                    {
                        var jsFileSetCacheSection = this.context.Cache.BeginSection(
                            TimeMeasureNames.JsFileSet,
                            new
                            {
                                jsFileSet,
                                this.context.Configuration.ConfigType
                            });

                        try
                        {
                            if (jsFileSetCacheSection.CanBeSkipped())
                            {
                                var endResults = jsFileSetCacheSection.GetResults(CacheKeys.MinifyJsResultCacheKey);
                                jsHasher.AppendToWorkLog(endResults);
                                continue;
                            }

                            this.context.Measure.Start(TimeMeasureNames.JsFileSet);

                            try
                            {
                                var outputFile = Path.Combine(this.staticAssemblerDirectory, jsFileSet.Output);

                                // bundling
                                var localizedInputFiles = this.Bundle(jsFileSet, outputFile, FileTypes.JavaScript, this.context.Configuration.ConfigType);

                                if (localizedInputFiles == null)
                                {
                                    encounteredError = true;
                                    continue;
                                }

                                // resolve the resources
                                WebGreaseContext.CleanDirectory(jsLocalesOutputPath);
                                ResolveLocaleResources(jsFileSet, this.context, jsLocalesOutputPath, FileTypes.JavaScript);

                                WebGreaseContext.CleanDirectory(jsLocalizedOutputPath);
                                
                                // localize
                                this.context.Log.Information("Resolving tokens and performing localization.");
                                if (!this.LocalizeJs(localizedInputFiles, jsFileSet.Locales, jsLocalizedOutputPath, jsLocalesOutputPath))
                                {
                                    this.context.Log.Error(null, "There were errors encountered while resolving tokens.");
                                    encounteredError = true;
                                    continue;
                                }

                                this.context.Log.Information("Minimizing javascript files");
                                string minifySearchMask = localizedInputFiles.Count() > 1
                                                              ? "*" + Path.GetFileName(jsFileSet.Output)
                                                              : Strings.JsFilter;
                                var jsFileSetResults = this.MinifyJs(
                                        jsLocalizedOutputPath,
                                        hashInputPath,
                                        minifySearchMask,
                                        WebGreaseConfiguration.GetNamedConfig(jsFileSet.Minification, this.context.Configuration.ConfigType),
                                        WebGreaseConfiguration.GetNamedConfig(jsFileSet.Validation, this.context.Configuration.ConfigType),
                                        jsHasher);

                                if (!jsFileSetResults)
                                {
                                    this.context.Log.Error(
                                        null, "There were errors encountered while minimizing javascript files.");
                                    encounteredError = true;
                                }

                                if (!encounteredError)
                                {
                                    jsFileSetCacheSection.Store();
                                }
                            }
                            finally
                            {
                                this.context.Measure.End(TimeMeasureNames.JsFileSet);
                            }
                        }
                        finally
                        {
                            jsFileSetCacheSection.EndSection();
                        }
                    }

                    if (!encounteredError)
                    {
                        jsHasher.Save();
                        jsCacheSection.Store();
                    }

                    return !encounteredError;
                }
                finally
                {
                    this.context.Measure.End(TimeMeasureNames.EverythingActivity, TimeMeasureNames.Js);
                }
            }
            finally
            {
                jsCacheSection.EndSection();
            }
        }

        /// <summary>Executes bundling.</summary>
        /// <param name="fileSet">The file set.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="fileType">The file type.</param>
        /// <param name="configType">The config type.</param>
        /// <returns>The resulting files.</returns>
        private IEnumerable<string> Bundle(IFileSet fileSet, string outputFile, FileTypes fileType, string configType)
        {
            var bundleConfig = WebGreaseConfiguration.GetNamedConfig(fileSet.Bundling, configType);

            if (bundleConfig.ShouldBundleFiles)
            {
                this.context.Log.Information("Bundling files.");
                if (!this.BundleFiles(fileSet.InputSpecs, outputFile, fileSet.Preprocessing, fileType, true))
                {
                    // bundling failed
                    this.context.Log.Error(null, "There were errors while bundling files.");
                    return null;
                }

                // input for the next step is the output file from bundling
                return new[] { outputFile };
            }

            if (fileSet.Preprocessing.Enabled)
            {
                // bundling calls the preprocessor, so we need to do it seperately if there was no bundling.
                return this.PreprocessFiles(this.preprocessingTempDirectory, fileSet.InputSpecs, fileType == FileTypes.JavaScript ? "js" : "css", fileSet.Preprocessing);
            }

            fileSet.InputSpecs.ForEach(this.context.Cache.CurrentCacheSection.AddSourceDependency);
            return fileSet.InputSpecs.GetFiles(this.context.Configuration.SourceDirectory);
        }

        /// <summary>
        /// Pre processes each file in the inputs list, outputs them into the target folder, using filename.defaultTargetExtensions, or if the same as input extension, .processed.defaultTargetExtensions
        /// </summary>
        /// <param name="targetFolder">Target folder</param>
        /// <param name="inputFiles">Input files</param>
        /// <param name="defaultTargetExtensions">Default target extensions</param>
        /// <param name="preprocessingConfig">The pre processing config </param>
        /// <returns>The preprocessed file</returns>
        private IEnumerable<string> PreprocessFiles(string targetFolder, IEnumerable<InputSpec> inputFiles, string defaultTargetExtensions, PreprocessingConfig preprocessingConfig)
        {
            var preprocessorActivity = new PreprocessorActivity(this.context)
                {
                    DefaultExtension = defaultTargetExtensions,
                    OutputFolder = targetFolder,
                    PreprocessingConfig = preprocessingConfig,
                };

            preprocessorActivity.Inputs.AddRange(inputFiles);
            return preprocessorActivity.Execute();
        }

        /// <summary>Minifies css files.</summary>
        /// <param name="rootInputPath">Path to look in for css files.</param>
        /// <param name="outputPath">The output path </param>
        /// <param name="searchFilter">filter to qualify files</param>
        /// <param name="cssConfig">configuration settings</param>
        /// <param name="spritingConfig">The sprite configuration </param>
        /// <param name="imageHasher">The image hasher</param>
        /// <param name="cssHasher">The css hasher</param>
        /// <returns>True is successfull false if not.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private bool MinifyCss(string rootInputPath, string outputPath, string searchFilter, CssMinificationConfig cssConfig, CssSpritingConfig spritingConfig, FileHasherActivity imageHasher, FileHasherActivity cssHasher)
        {
            var successful = true;

            var minifier = new MinifyCssActivity(this.context)
            {
                ShouldAssembleBackgroundImages = spritingConfig.ShouldAutoSprite,
                ShouldMinify = cssConfig.ShouldMinify,
                ShouldOptimize = cssConfig.ShouldMinify,
                ShouldValidateForLowerCase = cssConfig.ShouldValidateLowerCase,
                ShouldExcludeProperties = cssConfig.ShouldExcludeProperties,
                ShouldHashImages = true,

                ImageExtensions = this.context.Configuration.ImageExtensions,
                ImageDirectories = this.context.Configuration.ImageDirectories,
                ImageHasher = imageHasher,

                BannedSelectors = new HashSet<string>(cssConfig.RemoveSelectors.ToArray()),
                HackSelectors = new HashSet<string>(cssConfig.ForbiddenSelectors.ToArray()),
                ImageAssembleReferencesToIgnore = new HashSet<string>(spritingConfig.ImagesToIgnore.ToArray()),
                OutputUnit = spritingConfig.OutputUnit,
                OutputUnitFactor = spritingConfig.OutputUnitFactor,
                ImagesOutputDirectory = this.imagesDestinationDirectory,
                IgnoreImagesWithNonDefaultBackgroundSize = spritingConfig.IgnoreImagesWithNonDefaultBackgroundSize,

                CssHasher = cssHasher,
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
                minifier.ImageAssembleScanDestinationFile = scanFilePath;
                minifier.DestinationFile = outputFile;
                minifier.OutputPath = outputPath;

                try
                {
                    // execute the minifier on the css.
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

        /// <summary>Minify js activity</summary>
        /// <param name="inputPath">path to localized js files to be minified</param>
        /// <param name="outputPath">The output Path.</param>
        /// <param name="searchFilter">The search Filter.</param>
        /// <param name="jsConfig">The js Config.</param>
        /// <param name="jsValidateConfig">The js Validate Config.</param>
        /// <param name="jsHasher">The javascript hasher.</param>
        /// <returns>True if successfull false if not.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private bool MinifyJs(string inputPath, string outputPath, string searchFilter, JsMinificationConfig jsConfig, JSValidationConfig jsValidateConfig, FileHasherActivity jsHasher)
        {
            var success = true;
            var minifier = new MinifyJSActivity(this.context);

            minifier.FileHasher = jsHasher;

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
                minifier.OutputPath = outputPath;

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
        /// <param name="isOriginalSource">Whetere the input is the original source</param>
        /// <returns>a value indicating whether the operation was successful</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private bool BundleFiles(IEnumerable<InputSpec> inputSpecs, string outputFile, PreprocessingConfig preprocessing, FileTypes fileType, bool isOriginalSource)
        {
            // now we have the input prepared, so use Assembler activity to create the one file to use as input (if we were't assembling, we'd need to grab all) 
            // we are bundling either JS or CSS files -- for JS files we want to append semicolons between them and use single-line comments; for CSS file we don't.
            var assemblerActivity = new AssemblerActivity(this.context)
                {
                    PreprocessingConfig = preprocessing,
                    AddSemicolons = fileType == FileTypes.JavaScript,
                    InputIsOriginalSource = isOriginalSource
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
    }
}
