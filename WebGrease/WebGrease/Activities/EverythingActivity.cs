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
                result = this.ExecuteCss(
                    this.context.Configuration.CssFileSets, 
                    this.context.Configuration.SourceDirectory, 
                    this.context.Configuration.DestinationDirectory, 
                    this.context.Configuration.ConfigType, 
                    this.context.Configuration.ImageDirectories, 
                    this.context.Configuration.ImageExtensions);
            }

            return result;
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

        /// <summary>Execute the css pipeline.</summary>
        /// <param name="cssFileSets">The css File Sets.</param>
        /// <param name="sourceDirectory">The source Directory.</param>
        /// <param name="destinationDirectory">The destination Directory.</param>
        /// <param name="configType">The config Type.</param>
        /// <param name="imageDirectories">The image Directories.</param>
        /// <param name="imageExtensions">The image Extensions.</param>
        /// <returns>If it was successfull</returns>
        private bool ExecuteCss(IList<CssFileSet> cssFileSets, string sourceDirectory, string destinationDirectory, string configType, IList<string> imageDirectories, IList<string> imageExtensions)
        {
            var cssLogPath = Path.Combine(this.context.Configuration.LogsDirectory, Strings.CssLogFile);
            var cssHashedOutputPath = Path.Combine(destinationDirectory, CssDestinationDirectoryName);
            var imageHashedOutputPath = Path.Combine(destinationDirectory, ImagesDestinationDirectoryName);

            if (!cssFileSets.Any())
            {
                return true;
            }

            return this.context.Section(
                new[] { SectionIdParts.EverythingActivity, SectionIdParts.Css },
                new { cssFileSets, sourceDirectory, destinationDirectory, configType, imageExtensions, imageDirectories },
                true,
                cssCacheSection =>
                {
                    var success = true;

                    this.context.Log.Information("Begin CSS file pipeline.");

                    var imageHasher = GetFileHasher(
                            this.context,
                            Path.Combine(destinationDirectory, ImagesDestinationDirectoryName),
                            this.imagesLogFile,
                            FileTypes.Image,
                            destinationDirectory,
                            "../../", // The hashed image path relative to the css path.
                            imageExtensions);

                    var cssHasher = GetFileHasher(this.context, cssHashedOutputPath, cssLogPath, FileTypes.StyleSheet, this.context.Configuration.ApplicationRootDirectory);

                    foreach (var cssFileSet in cssFileSets)
                    {
                        success &= this.ExecuteCssFileSet(configType, imageDirectories, imageExtensions, cssFileSet, cssHasher, imageHasher, imageHashedOutputPath);
                    }

                    if (success)
                    {
                        imageHasher.Save();
                        cssHasher.Save();
                        cssCacheSection.Save();
                    }

                    return success;
                });
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
            return this.context.Section(
                new[] { SectionIdParts.CssFileSet },
                new { cssFileSet, configType },
                true,
                cssFileSetCacheSection =>
                    {
                        var cssMinifier = this.CreateCssMinifier(cssFileSet, cssHasher, imageHasher, imageExtensions, imageDirectories, imagesDestinationDirectory);
                        var outputFile = Path.Combine(this.staticAssemblerDirectory, cssFileSet.Output);
                        var inputFiles = this.Bundle(cssFileSet, outputFile, FileTypes.StyleSheet, configType);
                        if (inputFiles == null)
                        {
                            return false;
                        }

                        // localization
                        this.context.Log.Information("Resolving tokens and performing localization.");

                        // Resolve resources
                        var localeResources = GetLocaleResources(cssFileSet, this.context, FileTypes.StyleSheet);
                        var themeResources = GetThemeResources(cssFileSet, this.context, FileTypes.StyleSheet);

                        // Localize the css
                        var success = this.LocalizeCss(
                            inputFiles,
                            cssFileSet.Locales,
                            localeResources,
                            cssFileSet.Themes,
                            themeResources,
                            localizedContentItem =>
                                {
                                    this.context.Log.Information("Minifying css files, and spriting background images.");
                                    var minifyResults = this.MinifyCss(localizedContentItem, cssMinifier);

                                    if (!minifyResults)
                                    {
                                        // minification failed.
                                        this.context.Log.Error(null, "There were errors while minifying the css files.");
                                        return false;
                                    }

                                    return true;
                                });

                        if (!success)
                        {
                            // localization failed for this batch
                            this.context.Log.Error(null, "There were errors encountered while resolving tokens.");
                        }

                        return success;
                    });
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
            var cssConfig = WebGreaseConfiguration.GetNamedConfig(cssFileSet.Minification, this.context.Configuration.ConfigType);
            var spritingConfig = WebGreaseConfiguration.GetNamedConfig(cssFileSet.ImageSpriting, this.context.Configuration.ConfigType);
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

        /// <summary>Execute the javascript pipeline.</summary>
        /// <returns>If it was successfull</returns>
        private bool ExecuteJS()
        {
            var jsLogPath = Path.Combine(this.context.Configuration.LogsDirectory, Strings.JsLogFile);
            var jsHashedOutputPath = Path.Combine(this.context.Configuration.DestinationDirectory, JsDestinationDirectoryName);

            if (!this.context.Configuration.JSFileSets.Any())
            {
                return true;
            }

            var jsCacheSection = this.context.Cache.BeginSection(
                SectionIdParts.EverythingActivity + "." + SectionIdParts.Js,
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

                this.context.Measure.Start(SectionIdParts.EverythingActivity, SectionIdParts.Js);
                try
                {
                    var encounteredError = false;

                    var jsHasher = GetFileHasher(this.context, jsHashedOutputPath, jsLogPath, FileTypes.JavaScript, this.context.Configuration.ApplicationRootDirectory);

                    // process each js file set.
                    foreach (var jsFileSet in this.context.Configuration.JSFileSets)
                    {
                        var jsFileSetCacheSection = this.context.Cache.BeginSection(
                            SectionIdParts.JsFileSet,
                            new
                            {
                                jsFileSet,
                                this.context.Configuration.ConfigType
                            });

                        try
                        {
                            if (jsFileSetCacheSection.CanBeSkipped())
                            {
                                var endResults = jsFileSetCacheSection.GetCachedContentItems(CacheFileCategories.MinifyJsResult);
                                jsHasher.AppendToWorkLog(endResults);
                                continue;
                            }

                            this.context.Measure.Start(SectionIdParts.JsFileSet);

                            try
                            {
                                var outputFile = Path.Combine(this.staticAssemblerDirectory, jsFileSet.Output);

                                // bundling
                                var bundledFiles = this.Bundle(jsFileSet, outputFile, FileTypes.JavaScript, this.context.Configuration.ConfigType);

                                if (bundledFiles == null)
                                {
                                    encounteredError = true;
                                    continue;
                                }

                                // resolve the resources
                                var localeResources = GetLocaleResources(jsFileSet, this.context, FileTypes.JavaScript);

                                // localize
                                this.context.Log.Information("Resolving tokens and performing localization.");
                                var localizedJsFiles = this.LocalizeJs(bundledFiles, jsFileSet.Locales, localeResources);
                                if (localizedJsFiles == null)
                                {
                                    this.context.Log.Error(null, "There were errors encountered while resolving tokens.");
                                    encounteredError = true;
                                    continue;
                                }

                                this.context.Log.Information("Minimizing javascript files");

                                var jsFileSetResults = this.MinifyJs(
                                        localizedJsFiles,
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
                                    jsFileSetCacheSection.Save();
                                }
                            }
                            finally
                            {
                                this.context.Measure.End(SectionIdParts.JsFileSet);
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
                        jsCacheSection.Save();
                    }

                    return !encounteredError;
                }
                finally
                {
                    this.context.Measure.End(SectionIdParts.EverythingActivity, SectionIdParts.Js);
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
        private IEnumerable<ContentItem> Bundle(IFileSet fileSet, string outputFile, FileTypes fileType, string configType)
        {
            var bundleConfig = WebGreaseConfiguration.GetNamedConfig(fileSet.Bundling, configType);

            if (bundleConfig.ShouldBundleFiles)
            {
                this.context.Log.Information("Bundling files.");
                var resultFile = this.BundleFiles(fileSet.InputSpecs, outputFile, fileSet.Preprocessing, fileType);
                if (resultFile == null)
                {
                    // bundling failed
                    this.context.Log.Error(null, "There were errors while bundling files.");
                    return null;
                }

                // input for the next step is the output file from bundling
                return new[] { resultFile };
            }

            if (fileSet.Preprocessing.Enabled)
            {
                // bundling calls the preprocessor, so we need to do it seperately if there was no bundling.
                return this.PreprocessFiles(this.preprocessingTempDirectory, fileSet.InputSpecs, fileSet.Preprocessing);
            }

            fileSet.InputSpecs.ForEach(this.context.Cache.CurrentCacheSection.AddSourceDependency);
            return fileSet.InputSpecs
                .GetFiles(this.context.Configuration.SourceDirectory)
                .Select(f => ContentItem.FromFile(f, f, this.context.Configuration.SourceDirectory));
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

            var themeFilePrefix = !inputFile.Theme.IsNullOrWhitespace() ? inputFile.Theme + "_" : string.Empty;
            var fileName = Path.Combine(inputFile.Locale ?? string.Empty, CssDestinationDirectoryName, themeFilePrefix + Path.GetFileNameWithoutExtension(inputFile.RelativeContentPath) + "." + Strings.Css);
            this.context.Log.Information("Css Minify start: " + fileName);

            // TODO:RTUIT: Make the scan file in memory as well.
            var scanFilePath = Path.Combine(this.toolsTempDirectory, Strings.Css, Path.GetFileNameWithoutExtension(inputFile.RelativeContentPath) + "_" + inputFile.Locale + "_" + inputFile.Theme + ".scan." + Strings.Css);

            minifier.ImageAssembleScanDestinationFile = scanFilePath;
            minifier.SourceFile = fileName;
            minifier.DestinationFile = fileName;

            try
            {
                // execute the minifier on the css.
                minifier.Execute(inputFile);
            }
            catch (Exception ex)
            {
                successful = false;
                AggregateException aggEx;

                if (ex.InnerException != null && (aggEx = ex.InnerException as AggregateException) != null)
                {
                    // antlr can throw a blob of errors, so they need to be deduped to get the real set of errors
                    var errors = aggEx.CreateBuildErrors(fileName);
                    foreach (var error in errors)
                    {
                        this.HandleError(error);
                    }
                }
                else
                {
                    // Catch, record and display error
                    this.HandleError(ex, fileName);
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
                var outputFile = Path.Combine(inputFile.Locale ?? string.Empty, JsDestinationDirectoryName, Path.GetFileNameWithoutExtension(inputFile.RelativeContentPath) + "." + Strings.JS);
                this.context.Log.Information("Js Minify start: " + outputFile);

                minifier.DestinationFile = outputFile;
                try
                {
                    minifier.Execute(inputFile);
                }
                catch (Exception ex)
                {
                    this.HandleError(ex, outputFile);
                    success = false;
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
        /// <param name="locales">A collection of locale codes to localize for</param>
        /// <param name="localeResources">The locale resources</param>
        /// <param name="themes">A collection of theme names to base resources on.</param>
        /// <param name="themeResources">The theme resources</param>
        /// <param name="localizedItemAction">The item action</param>
        /// <returns>True if it succeeded, false if it did not</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Handles all errors")]
        private bool LocalizeCss(IEnumerable<ContentItem> cssInputItems, IEnumerable<string> locales, IDictionary<string, IDictionary<string, string>> localeResources, IEnumerable<string> themes, IDictionary<string, IDictionary<string, string>> themeResources, Func<ContentItem, bool> localizedItemAction)
        {
            var success = true;

            if (!locales.Any())
            {
                locales = new[] { Strings.DefaultLocale };
                localeResources[Strings.DefaultLocale] = new Dictionary<string, string>();
            }

            foreach (var cssInputItem in cssInputItems)
            {
                try
                {
                    success &= CssLocalizationActivity.LocalizeAndTheme(this.context, cssInputItem, locales, localeResources, themes, themeResources, localizedItemAction);
                }
                catch (Exception ex)
                {
                    this.HandleError(ex, cssInputItem.RelativeContentPath);
                    success = false;
                }
            }

            return success;
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
                    AddSemicolons = fileType == FileTypes.JavaScript
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
