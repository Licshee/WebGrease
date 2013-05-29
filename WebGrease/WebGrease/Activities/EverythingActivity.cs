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

        /// <summary>the folder name of where the js files will be stored.</summary>
        private const string JsDestinationDirectoryName = "js";

        /// <summary>directory where final css files are stored</summary>
        private const string CssDestinationDirectoryName = "css";

        /// <summary>The tools temp directory name.</summary>
        private const string ToolsTempDirectoryName = "ToolsTemp";

        /// <summary>The static assembler directory name.</summary>
        private const string StaticAssemblerDirectoryName = "StaticAssemblerOutput";

        /// <summary>The pre processing directory name.</summary>
        private const string PreprocessingTempDirectory = "PreCompiler";

        /// <summary>The tools temp directory.</summary>
        private readonly string toolsTempDirectory;

        /// <summary>The static assembler directory.</summary>
        private readonly string staticAssemblerDirectory;

        /// <summary>The log directory.</summary>
        private readonly string logDirectory;

        /// <summary>The pre processing temp directory.</summary>
        private readonly string preprocessingTempDirectory;

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
            this.logDirectory = context.Configuration.LogsDirectory;
            this.toolsTempDirectory = context.Configuration.ToolsTempDirectory.IsNullOrWhitespace()
                ? Path.Combine(context.Configuration.LogsDirectory, ToolsTempDirectoryName)
                : context.Configuration.ToolsTempDirectory;
            this.imagesLogFile = Path.Combine(this.logDirectory, Strings.ImagesLogFile);
            this.preprocessingTempDirectory = Path.Combine(this.toolsTempDirectory, PreprocessingTempDirectory);
            this.staticAssemblerDirectory = Path.Combine(this.toolsTempDirectory, StaticAssemblerDirectoryName);
        }

        /// <summary>Will execute the pipeline for all filesets in the provided context configuration.</summary>
        /// <returns>If it is successfull.</returns>
        internal bool Execute()
        {
            return this.Execute(this.context.Configuration.CssFileSets.OfType<IFileSet>().Concat(this.context.Configuration.JSFileSets));
        }

        /// <summary>The main execution point, executes for all given file sets.</summary>
        /// <param name="fileSets">The file Types.</param>
        /// <returns>If it failed or succeeded.</returns>
        internal bool Execute(IEnumerable<IFileSet> fileSets)
        {
            var success = true;

            var jsFileSets = fileSets.OfType<JSFileSet>().ToArray();
            if (jsFileSets.Any())
            {
                success &= this.ExecuteJS(
                    jsFileSets,
                    this.context.Configuration.ConfigType,
                    this.context.Configuration.SourceDirectory,
                    this.context.Configuration.DestinationDirectory);
            }

            var cssFileSets = fileSets.OfType<CssFileSet>().ToArray();
            if (cssFileSets.Any())
            {
                success &= this.ExecuteCss(
                    cssFileSets,
                    this.context.Configuration.SourceDirectory,
                    this.context.Configuration.DestinationDirectory,
                    this.context.Configuration.ConfigType,
                    this.context.Configuration.ImageDirectories,
                    this.context.Configuration.ImageExtensions);
            }

            return success;
        }

        /// <summary>Hashes a selection of files in the input path, and copies them to the output folder.</summary>
        /// <param name="context">The context.</param>
        /// <param name="hashOutputPath">Path to copy the output.</param>
        /// <param name="logFileName">log path for log data</param>
        /// <param name="fileType">The file type</param>
        /// <param name="outputRelativeToPath">The output Relative To Path.</param>
        /// <param name="basePrefixToAddToOutputPath">The base Prefix To Add To Output Path.</param>
        /// <param name="fileTypeFilters">The file type filters.</param>
        /// <returns>True if successfull false if not.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Catch to handle all errors.")]
        private static FileHasherActivity GetFileHasher(IWebGreaseContext context, string hashOutputPath, string logFileName, FileTypes fileType, string outputRelativeToPath, string basePrefixToAddToOutputPath = null, IEnumerable<string> fileTypeFilters = null)
        {
            var fileTypeFilter = fileTypeFilters != null
                ? string.Join(new string(Strings.FileFilterSeparator), fileTypeFilters)
                : null;
            return new FileHasherActivity(context)
                       {
                           DestinationDirectory = hashOutputPath,
                           BasePrefixToRemoveFromOutputPathInLog = outputRelativeToPath,
                           CreateExtraDirectoryLevelFromHashes = true,
                           ShouldPreserveSourceDirectoryStructure = false,
                           LogFileName = logFileName,
                           FileType = fileType,
                           FileTypeFilter = fileTypeFilter,
                           BasePrefixToAddToOutputPath = basePrefixToAddToOutputPath
                       };
        }

        /// <summary>Expands the resource tokens for locales and themes</summary>
        /// <param name="cssFileSet">The file set to be processed</param>
        /// <param name="context">Config object with locations of needed directories.</param>
        /// <param name="fileType">The file type</param>
        /// <returns>The merged resources.</returns>
        private static IDictionary<string, IDictionary<string, string>> GetThemeResources(CssFileSet cssFileSet, IWebGreaseContext context, FileTypes fileType)
        {
            var themeResourceActivity = new ResourcesResolutionActivity(context)
                                            {
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

            return themeResourceActivity.GetMergedResources();
        }

        /// <summary>Expands the resource tokens for locales and themes</summary>
        /// <param name="fileSet">The js File Set.</param>
        /// <param name="context">The context.</param>
        /// <param name="fileType">The file type</param>
        /// <returns>The merged resources.</returns>
        private static IDictionary<string, IDictionary<string, string>> GetLocaleResources(IFileSet fileSet, IWebGreaseContext context, FileTypes fileType)
        {
            var localeResourceActivity = new ResourcesResolutionActivity(context)
                                             {
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

            return localeResourceActivity.GetMergedResources();
        }

        /// <summary>The ensure css log file.</summary>
        /// <param name="cssHasher">The css hasher.</param>
        /// <param name="imageHasher">The image hasher.</param>
        /// <param name="cacheSection">The cache section.</param>
        private static void EnsureCssLogFile(FileHasherActivity cssHasher, FileHasherActivity imageHasher, ICacheSection cacheSection)
        {
            cssHasher.AppendToWorkLog(cacheSection.GetCachedContentItems(CacheFileCategories.MinifiedCssResult));
            imageHasher.AppendToWorkLog(cacheSection.GetCachedContentItems(CacheFileCategories.HashedImage));
        }

        /// <summary>The ensure js log file.</summary>
        /// <param name="jsHasher">The js hasher.</param>
        /// <param name="cacheSection">The cache section.</param>
        private static void EnsureJsLogFile(FileHasherActivity jsHasher, ICacheSection cacheSection)
        {
            jsHasher.AppendToWorkLog(cacheSection.GetCachedContentItems(CacheFileCategories.MinifyJsResult));
        }

        /// <summary>Gets the destination file paths.</summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="destinationDirectoryName">The destination directory name.</param>
        /// <param name="destinationExtension">The destination Extension.</param>
        /// <returns>The destination files as a colon seperated list.</returns>
        private string GetDestinationFilePaths(ContentItem inputFile, string destinationDirectoryName, string destinationExtension)
        {
            if (inputFile.Pivots == null || !inputFile.Pivots.Any())
            {
                return GetContentPivotDestinationFilePath(inputFile.RelativeContentPath, destinationDirectoryName, destinationExtension);
            }

            var fileNames = new List<string>();
            foreach (var contentPivot in inputFile.Pivots)
            {
                if (this.context.TemporaryIgnore(contentPivot))
                {
                    continue;
                }

                fileNames.Add(GetContentPivotDestinationFilePath(inputFile.RelativeContentPath, destinationDirectoryName, destinationExtension, contentPivot));
            }

            return string.Join("|", fileNames);
        }

        /// <summary>The get content pivot destination file path.</summary>
        /// <param name="relativeContentPath">The relative Content Path.</param>
        /// <param name="destinationDirectoryName">The destination directory name.</param>
        /// <param name="destinationExtension">The destination Extension.</param>
        /// <param name="contentPivot">The content pivot.</param>
        /// <returns>The destination path.</returns>
        private static string GetContentPivotDestinationFilePath(string relativeContentPath, string destinationDirectoryName, string destinationExtension, ContentPivot contentPivot = null)
        {
            var themeFilePrefix = contentPivot != null && !contentPivot.Theme.IsNullOrWhitespace()
                                      ? contentPivot.Theme + "_"
                                      : string.Empty;

            var localePath = contentPivot != null && !contentPivot.Locale.IsNullOrWhitespace()
                                 ? contentPivot.Locale
                                 : string.Empty;

            var fileName = Path.Combine(localePath, destinationDirectoryName, themeFilePrefix + Path.ChangeExtension(relativeContentPath, destinationExtension));

            return fileName;
        }

        /// <summary>Execute the css pipeline.</summary>
        /// <param name="cssFileSets">The css File Sets.</param>
        /// <param name="sourceDirectory">The source Directory.</param>
        /// <param name="destinationDirectory">The destination Directory.</param>
        /// <param name="configType">The config Type.</param>
        /// <param name="imageDirectories">The image Directories.</param>
        /// <param name="imageExtensions">The image Extensions.</param>
        /// <returns>If it was successfull</returns>
        private bool ExecuteCss(IEnumerable<CssFileSet> cssFileSets, string sourceDirectory, string destinationDirectory, string configType, IList<string> imageDirectories, IList<string> imageExtensions)
        {
            var cssLogPath = Path.Combine(this.context.Configuration.LogsDirectory, Strings.CssLogFile);
            var cssHashedOutputPath = Path.Combine(destinationDirectory, CssDestinationDirectoryName);
            var imageHashedOutputPath = Path.Combine(destinationDirectory, ImagesDestinationDirectoryName);

            if (!cssFileSets.Any())
            {
                return true;
            }

            var imageHasher = GetFileHasher(
                    this.context,
                    Path.Combine(destinationDirectory, ImagesDestinationDirectoryName),
                    this.imagesLogFile,
                    FileTypes.Image,
                    destinationDirectory,
                    "../../", // The hashed image path relative to the css path.
                    imageExtensions);

            var cssHasher = GetFileHasher(this.context, cssHashedOutputPath, cssLogPath, FileTypes.CSS, this.context.Configuration.ApplicationRootDirectory);

            var totalSuccess = this.context.SectionedAction(SectionIdParts.EverythingActivity, SectionIdParts.Css)
                .CanBeCached(new { cssFileSets, sourceDirectory, destinationDirectory, configType, imageExtensions, imageDirectories }, true)
                .WhenSkipped(cacheSection => EnsureCssLogFile(cssHasher, imageHasher, cacheSection))
                .Execute(cacheSection =>
                {
                    var success = true;

                    foreach (var cssFileSet in cssFileSets)
                    {
                        success &= this.ExecuteCssFileSet(configType, imageDirectories, imageExtensions, cssFileSet, cssHasher, imageHasher, imageHashedOutputPath);
                    }

                    return success;
                });

            if (totalSuccess)
            {
                imageHasher.Save();
                cssHasher.Save();
            }

            return totalSuccess;
        }

        /// <summary>The execute css file set.</summary>
        /// <param name="configType">The config type.</param>
        /// <param name="imageDirectories">The image directories.</param>
        /// <param name="imageExtensions">The image extensions.</param>
        /// <param name="cssFileSet">The css file set.</param>
        /// <param name="cssHasher">The css hasher.</param>
        /// <param name="imageHasher">The image hasher.</param>
        /// <param name="imagesDestinationDirectory">The images Destination Directory.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private bool ExecuteCssFileSet(string configType, IList<string> imageDirectories, IList<string> imageExtensions, CssFileSet cssFileSet, FileHasherActivity cssHasher, FileHasherActivity imageHasher, string imagesDestinationDirectory)
        {
            return this.context
                .SectionedAction(SectionIdParts.CssFileSet)
                .CanBeCached(cssFileSet, new { configType }, true)
                .WhenSkipped(cacheSection => EnsureCssLogFile(cssHasher, imageHasher, cacheSection))
                .Execute(cacheSection =>
                    {
                        var cssMinifier = this.CreateCssMinifier(cssFileSet, cssHasher, imageHasher, imageExtensions, imageDirectories, imagesDestinationDirectory);
                        var outputFile = Path.Combine(this.staticAssemblerDirectory, cssFileSet.Output);
                        var inputFiles = this.Bundle(cssFileSet, outputFile, FileTypes.CSS, configType);
                        if (inputFiles == null)
                        {
                            return false;
                        }

                        // localization
                        this.context.Log.Information("Resolving tokens and performing localization.");

                        // Resolve resources
                        var localeResources = GetLocaleResources(cssFileSet, this.context, FileTypes.CSS);
                        var themeResources = GetThemeResources(cssFileSet, this.context, FileTypes.CSS);

                        // Localize the css
                        var localizedAndThemedCssItems = this.LocalizeAndThemeCss(
                            inputFiles,
                            localeResources,
                            themeResources,
                            cssMinifier.ShouldMinify);

                        this.context.Log.Information("Minifying css files, and spriting background images.");
                        var localizeSuccess = localizedAndThemedCssItems.All(l => l != null);

                        if (!localizeSuccess)
                        {
                            // localization failed for this batch
                            this.context.Log.Error(null, "There were errors while minifying the css files.");
                        }
                        
                        var minifySuccess = true;
                        foreach (var localizedAndThemedCssItem in localizedAndThemedCssItems)
                        {
                            minifySuccess &= this.MinifyCss(localizedAndThemedCssItem, cssMinifier);
                        }

                        if (!minifySuccess)
                        {
                            // localization failed for this batch
                            this.context.Log.Error(null, "There were errors while minifying the css files.");
                        }

                        return minifySuccess && localizeSuccess;
                    });
        }

        /// <summary>Execute the javascript pipeline.</summary>
        /// <param name="jsFileSets">The js File Sets.</param>
        /// <param name="configType">The config Type.</param>
        /// <param name="sourceDirectory">The source Directory.</param>
        /// <param name="destinationDirectory">The destination Directory.</param>
        /// <returns>If it was successfull</returns>
        private bool ExecuteJS(IEnumerable<JSFileSet> jsFileSets, string configType, string sourceDirectory, string destinationDirectory)
        {
            var jsLogPath = Path.Combine(this.context.Configuration.LogsDirectory, Strings.JsLogFile);
            var jsHashedOutputPath = Path.Combine(destinationDirectory, JsDestinationDirectoryName);

            if (!jsFileSets.Any())
            {
                return true;
            }

            var jsHasher = GetFileHasher(this.context, jsHashedOutputPath, jsLogPath, FileTypes.JS, this.context.Configuration.ApplicationRootDirectory);
            var varBySettings = new { jsFileSets, configType, sourceDirectory, destinationDirectory };
            var totalSuccess = this.context.SectionedAction(SectionIdParts.EverythingActivity, SectionIdParts.Js)
                .CanBeCached(varBySettings, true)
                .WhenSkipped(cacheSection => EnsureJsLogFile(jsHasher, cacheSection))
                .Execute(cacheSection =>
                {
                    var success = true;

                    // process each js file set.
                    foreach (var jsFileSet in jsFileSets)
                    {
                        success &= this.ExecuteJSFileSet(jsFileSet, jsHasher, configType);
                    }

                    return success;
                });

            if (totalSuccess)
            {
                jsHasher.Save();
            }

            return totalSuccess;
        }

        /// <summary>The execute js file set.</summary>
        /// <param name="jsFileSet">The js file set.</param>
        /// <param name="jsHasher">The js hasher.</param>
        /// <param name="configType">The config Type.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "WebGrease.LogManager.Information(System.String,WebGrease.MessageImportance)", Justification = "Debug messages")]
        private bool ExecuteJSFileSet(JSFileSet jsFileSet, FileHasherActivity jsHasher, string configType)
        {
            return this.context.SectionedAction(SectionIdParts.JsFileSet)
                .CanBeCached(jsFileSet, new { configType }, true)
                .WhenSkipped(cacheSection => EnsureJsLogFile(jsHasher, cacheSection))
                .Execute(jsFileSetCacheSection =>
                    {
                        var outputFile = Path.Combine(this.staticAssemblerDirectory, jsFileSet.Output);

                        // bundling
                        var bundledFiles = this.Bundle(jsFileSet, outputFile, FileTypes.JS, configType);

                        if (bundledFiles == null)
                        {
                            return false;
                        }

                        // resolve the resources
                        var localeResources = GetLocaleResources(jsFileSet, this.context, FileTypes.JS);

                        // localize
                        this.context.Log.Information("Resolving tokens and performing localization.");
                        var localizedJsFiles = this.LocalizeJs(bundledFiles, jsFileSet.Locales, localeResources);
                        if (localizedJsFiles == null)
                        {
                            this.context.Log.Error(null, "There were errors encountered while resolving tokens.");
                            return false;
                        }

                        this.context.Log.Information("Minimizing javascript files");

                        var jsFileSetResults = this.MinifyJs(
                            localizedJsFiles,
                            jsFileSet.Minification.GetNamedConfig(configType),
                            jsFileSet.Validation.GetNamedConfig(configType),
                            jsHasher);

                        if (!jsFileSetResults)
                        {
                            this.context.Log.Error(
                                null, "There were errors encountered while minimizing javascript files.");

                            return false;
                        }

                        return true;
                    });
        }

        /// <summary>Executes bundling.</summary>
        /// <param name="fileSet">The file set.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="fileType">The file type.</param>
        /// <param name="configType">The config type.</param>
        /// <returns>The resulting files.</returns>
        private IEnumerable<ContentItem> Bundle(IFileSet fileSet, string outputFile, FileTypes fileType, string configType)
        {
            var bundleConfig = fileSet.Bundling.GetNamedConfig(configType);
            var preprocessingConfig = fileSet.Preprocessing.GetNamedConfig(this.context.Configuration.ConfigType);

            if (bundleConfig.ShouldBundleFiles)
            {
                this.context.Log.Information("Bundling files.");
                var resultFile = this.BundleFiles(fileSet.InputSpecs, outputFile, preprocessingConfig, fileType);
                if (resultFile == null)
                {
                    // bundling failed
                    this.context.Log.Error(null, "There were errors while bundling files.");
                    return null;
                }

                // input for the next step is the output file from bundling
                return new[] { resultFile };
            }

            if (preprocessingConfig != null && preprocessingConfig.Enabled)
            {
                // bundling calls the preprocessor, so we need to do it seperately if there was no bundling.
                return this.PreprocessFiles(this.preprocessingTempDirectory, fileSet.InputSpecs, preprocessingConfig);
            }

            fileSet.InputSpecs.ForEach(this.context.Cache.CurrentCacheSection.AddSourceDependency);
            return fileSet.InputSpecs
                .GetFiles(this.context.Configuration.SourceDirectory)
                .Select(f => ContentItem.FromFile(f, f, this.context.Configuration.SourceDirectory));
        }

        /// <summary>The create css minifier.</summary>
        /// <param name="cssFileSet">The css fileset.</param>
        /// <param name="cssHasher">The css hasher.</param>
        /// <param name="imageHasher">The image hasher.</param>
        /// <param name="imageExtensions">The image Extensions.</param>
        /// <param name="imageDirectories">The image Directories.</param>
        /// <param name="imagesDestinationDirectory">The images Destination Directory.</param>
        /// <returns>The <see cref="MinifyCssActivity"/>.</returns>
        private MinifyCssActivity CreateCssMinifier(CssFileSet cssFileSet, FileHasherActivity cssHasher, FileHasherActivity imageHasher, IList<string> imageExtensions, IList<string> imageDirectories, string imagesDestinationDirectory)
        {
            var cssConfig = cssFileSet.Minification.GetNamedConfig(this.context.Configuration.ConfigType);
            var spritingConfig = cssFileSet.ImageSpriting.GetNamedConfig(this.context.Configuration.ConfigType);
            var cssMinifier = new MinifyCssActivity(this.context)
                                  {
                                      ShouldAssembleBackgroundImages = spritingConfig.ShouldAutoSprite,
                                      ShouldMinify = cssConfig.ShouldMinify,
                                      ShouldOptimize = cssConfig.ShouldMinify,
                                      ShouldValidateForLowerCase = cssConfig.ShouldValidateLowerCase,
                                      ShouldExcludeProperties = cssConfig.ShouldExcludeProperties,
                                      ShouldHashImages = true,
                                      ImageExtensions = imageExtensions,
                                      ImageDirectories = imageDirectories,
                                      ImageHasher = imageHasher,
                                      BannedSelectors = new HashSet<string>(cssConfig.RemoveSelectors.ToArray()),
                                      HackSelectors = new HashSet<string>(cssConfig.ForbiddenSelectors.ToArray()),
                                      ImageAssembleReferencesToIgnore = new HashSet<string>(spritingConfig.ImagesToIgnore.ToArray()),
                                      OutputUnit = spritingConfig.OutputUnit,
                                      OutputUnitFactor = spritingConfig.OutputUnitFactor,
                                      ImagesOutputDirectory = imagesDestinationDirectory,
                                      IgnoreImagesWithNonDefaultBackgroundSize = spritingConfig.IgnoreImagesWithNonDefaultBackgroundSize,
                                      CssHasher = cssHasher,
                                  };
            return cssMinifier;
        }

        /// <summary>
        /// Pre processes each file in the inputs list, outputs them into the target folder, using filename.defaultTargetExtensions, or if the same as input extension, .processed.defaultTargetExtensions
        /// </summary>
        /// <param name="targetFolder">Target folder</param>
        /// <param name="inputFiles">Input files</param>
        /// <param name="preprocessingConfig">The pre processing config </param>
        /// <returns>The preprocessed file</returns>
        private IEnumerable<ContentItem> PreprocessFiles(string targetFolder, IEnumerable<InputSpec> inputFiles, PreprocessingConfig preprocessingConfig)
        {
            var preprocessorActivity = new PreprocessorActivity(this.context)
                {
                    OutputFolder = targetFolder,
                    PreprocessingConfig = preprocessingConfig,
                };

            preprocessorActivity.Inputs.AddRange(inputFiles);
            return preprocessorActivity.Execute();
        }

        /// <summary>Minifies css files.</summary>
        /// <param name="inputFile">The css content item.</param>
        /// <param name="minifier">The css minifier</param>
        /// <returns>True is successfull false if not.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private bool MinifyCss(ContentItem inputFile, MinifyCssActivity minifier)
        {
            var successful = true;

            var sourceFile = inputFile.RelativeContentPath;
            var destinationFiles = this.GetDestinationFilePaths(inputFile, CssDestinationDirectoryName, Strings.Css);

            if (!destinationFiles.IsNullOrWhitespace())
            {
                this.context.Log.Information("Css Minify start: " + destinationFiles + string.Join(string.Empty, inputFile.Pivots.Select(p => p.ToString())));

                minifier.SourceFile = sourceFile;
                minifier.DestinationFile = destinationFiles;

                try
                {
                    // execute the minifier on the css.
                    minifier.Execute(inputFile);
                }
                catch (Exception ex)
                {
                    successful = false;
                    AggregateException aggEx;

                    if ((aggEx = ex as AggregateException) != null || ((ex.InnerException != null) && (aggEx = ex.InnerException as AggregateException) != null))
                    {
                        // antlr can throw a blob of errors, so they need to be deduped to get the real set of errors
                        // TODO: RTUIT: Save the actual content in a temp folder and point to it for errors.
                        var errors = aggEx.CreateBuildErrors(sourceFile);
                        foreach (var error in errors)
                        {
                            this.HandleError(error);
                        }
                    }
                    else
                    {
                        // Catch, record and display error
                        // TODO: RTUIT: Save the actual content in a temp folder and point to it for errors.
                        this.HandleError(ex, sourceFile);
                    }
                }
            }

            return successful;
        }

        /// <summary>Minify js activity</summary>
        /// <param name="inputFiles">path to localized js files to be minified</param>
        /// <param name="jsConfig">The js Config.</param>
        /// <param name="jsValidateConfig">The js Validate Config.</param>
        /// <param name="jsHasher">The javascript hasher.</param>
        /// <returns>True if successfull false if not.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private bool MinifyJs(IEnumerable<ContentItem> inputFiles, JsMinificationConfig jsConfig, JSValidationConfig jsValidateConfig, FileHasherActivity jsHasher)
        {
            var success = true;
            var minifier = new MinifyJSActivity(this.context) { FileHasher = jsHasher };

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

            foreach (var inputFile in inputFiles)
            {
                var sourceFile = inputFile.RelativeContentPath;
                var destinationFiles = this.GetDestinationFilePaths(inputFile, JsDestinationDirectoryName, Strings.JS);
                if (!destinationFiles.IsNullOrWhitespace())
                {

                    minifier.DestinationFile = destinationFiles;

                    this.context.Log.Information("Js Minify start: " + sourceFile + string.Join(string.Empty, inputFile.Pivots.Select(p => p.ToString())));
                    try
                    {
                        minifier.Execute(inputFile);
                    }
                    catch (Exception ex)
                    {
                        this.HandleError(ex, sourceFile);
                        success = false;
                    }
                }
            }

            return success;
        }

        /// <summary>Localize the js files based on the expanded resource tokens for locales</summary>
        /// <param name="inputFiles">the files to resolve tokens in.</param>
        /// <param name="locales">A collection of locale codes</param>
        /// <param name="localeResources">The locale Resources.</param>
        /// <returns>True if successfull false if not.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private IEnumerable<ContentItem> LocalizeJs(IEnumerable<ContentItem> inputFiles, IEnumerable<string> locales, IDictionary<string, IDictionary<string, string>> localeResources)
        {
            if (!locales.Any())
            {
                return inputFiles;
            }

            var results = new List<ContentItem>();

            foreach (var jsFile in inputFiles)
            {
                try
                {
                    results.AddRange(
                        JSLocalizationActivity.Localize(
                            this.context,
                            jsFile,
                            locales,
                            localeResources));
                }
                catch (Exception ex)
                {
                    this.HandleError(ex, jsFile.RelativeContentPath);
                }
            }

            return results;
        }

        /// <summary>Localize the css files based on the expanded resource tokens for locales and themes</summary>
        /// <param name="cssInputItems">The css files to localize</param>
        /// <param name="localeResources">The locale resources</param>
        /// <param name="themeResources">The theme resources</param>
        /// <param name="shouldMinify">If it should minify, in this case we preemtively remove comments, faster to do before multiplication of files.</param>
        /// <returns>True if it succeeded, false if it did not</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Handles all errors")]
        private IEnumerable<ContentItem> LocalizeAndThemeCss(IEnumerable<ContentItem> cssInputItems, IDictionary<string, IDictionary<string, string>> localeResources, IDictionary<string, IDictionary<string, string>> themeResources, bool shouldMinify)
        {
            var results = new List<ContentItem>();

            if (!localeResources.Any())
            {
                localeResources[Strings.DefaultLocale] = new Dictionary<string, string>();
            }

            foreach (var cssInputItem in cssInputItems)
            {
                try
                {
                    results.AddRange(CssLocalizationActivity.LocalizeAndTheme(this.context, cssInputItem, localeResources, themeResources, shouldMinify));
                }
                catch (Exception ex)
                {
                    this.HandleError(ex, cssInputItem.RelativeContentPath);
                    results.Add(null);
                }
            }

            return results;
        }

        /// <summary>Combine files discovered through the input specs into the output file</summary>
        /// <param name="inputSpecs">A collection of files to be processed</param>
        /// <param name="outputFile">name of the output file</param>
        /// <param name="preprocessing">The preprocessing.</param>
        /// <param name="fileType">JavaScript of Stylesheets</param>
        /// <returns>a value indicating whether the operation was successful</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private ContentItem BundleFiles(IEnumerable<InputSpec> inputSpecs, string outputFile, PreprocessingConfig preprocessing, FileTypes fileType)
        {
            // now we have the input prepared, so use Assembler activity to create the one file to use as input (if we were't assembling, we'd need to grab all) 
            // we are bundling either JS or CSS files -- for JS files we want to append semicolons between them and use single-line comments; for CSS file we don't.
            var assemblerActivity = new AssemblerActivity(this.context)
                {
                    PreprocessingConfig = preprocessing,
                    AddSemicolons = fileType == FileTypes.JS
                };

            foreach (var inputSpec in inputSpecs)
            {
                assemblerActivity.Inputs.Add(inputSpec);
            }

            assemblerActivity.OutputFile = outputFile;

            try
            {
                return assemblerActivity.Execute(ContentItemType.Value);
            }
            catch (Exception ex)
            {
                // catch/record/display error.
                this.HandleError(ex);
            }

            return null;
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
