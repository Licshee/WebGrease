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
            this.ExecuteHashImages();
            return this.Execute(this.context.Configuration.CssFileSets.OfType<IFileSet>().Concat(this.context.Configuration.JSFileSets));
        }

        /// <summary>The main execution point, executes for all given file sets.</summary>
        /// <param name="fileSets">The file Types.</param>
        /// <param name="fileType">The file type</param>
        /// <returns>If it failed or succeeded.</returns>
        internal bool Execute(IEnumerable<IFileSet> fileSets, FileTypes fileType = FileTypes.All)
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

            if (fileType.HasFlag(FileTypes.Image))
            {
                this.ExecuteHashImages();
            }

            return success;
        }

        /// <summary>Hashes images defined in the configuration as to be hashed.</summary>
        internal void ExecuteHashImages()
        {
            if (this.context.Configuration.ImageDirectoriesToHash.Any())
            {
                var imageHasher = this.GetImageFileHasher(this.context.Configuration.DestinationDirectory, this.context.Configuration.ImageExtensions);
                HashImages(this.context, imageHasher, this.context.Configuration.ImageDirectoriesToHash, this.context.Configuration.ImageExtensions);
                imageHasher.Save();
            }
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
            EnsureLogFile(cssHasher, cacheSection.GetCachedContentItems(CacheFileCategories.HashedMinifiedCssResult));
            EnsureLogFile(imageHasher, cacheSection.GetCachedContentItems(CacheFileCategories.HashedImage));
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

        /// <summary>The ensure js log file.</summary>
        /// <param name="jsHasher">The js hasher.</param>
        /// <param name="cacheSection">The cache section.</param>
        private static void EnsureJsLogFile(FileHasherActivity jsHasher, ICacheSection cacheSection)
        {
            EnsureLogFile(jsHasher, cacheSection.GetCachedContentItems(CacheFileCategories.HashedMinifiedJsResult));
        }

        /// <summary>The ensure log file.</summary>
        /// <param name="hasher">The hasher.</param>
        /// <param name="contentItems">The content items.</param>
        private static void EnsureLogFile(FileHasherActivity hasher, IEnumerable<ContentItem> contentItems)
        {
            hasher.AppendToWorkLog(contentItems);
        }

        /// <summary>The hash images.</summary>
        /// <param name="context">The context.</param>
        /// <param name="imageHasher">The image hasher.</param>
        /// <param name="imageDirectoriesToHash">The image directories to hash.</param>
        /// <param name="imageExtensions">The image extensions.</param>
        private static void HashImages(IWebGreaseContext context, FileHasherActivity imageHasher, IEnumerable<string> imageDirectoriesToHash, IEnumerable<string> imageExtensions)
        {
            context
                .SectionedAction(SectionIdParts.ImageHash)
                .MakeCachable(new { imageDirectoriesToHash, imageExtensions })
                .RestoreFromCacheAction(cacheSection =>
                {
                    var contentItems = cacheSection.GetCachedContentItems(CacheFileCategories.HashedImage);
                    contentItems.ForEach(ci => ci.WriteToRelativeHashedPath(context.Configuration.DestinationDirectory));
                    EnsureLogFile(imageHasher, contentItems);
                    return true;
                })
                .WhenSkipped(cacheSection => EnsureLogFile(imageHasher, cacheSection.GetCachedContentItems(CacheFileCategories.HashedImage)))
                .Execute(cacheSection =>
                {
                    var directoryInputSpecs = imageDirectoriesToHash.Select(imageDirectoryToHash => new InputSpec { Path = imageDirectoryToHash, IsOptional = true, SearchPattern = "*.*", SearchOption = SearchOption.AllDirectories });
                    directoryInputSpecs.ForEach(cacheSection.AddSourceDependency);

                    var imagesToHash = context.GetAvailableFiles(context.Configuration.SourceDirectory, imageDirectoriesToHash, imageExtensions, FileTypes.Image);
                    foreach (var imageToHash in imagesToHash)
                    {
                        var imageContentItem = ContentItem.FromFile(imageToHash.Value, imageToHash.Key);
                        var hashedContentItem = imageHasher.Hash(imageContentItem);
                        cacheSection.AddResult(hashedContentItem, CacheFileCategories.HashedImage, true);
                    }

                    return true;
                });
        }

        /// <summary>Gets the destination file paths.</summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="destinationDirectoryName">The destination directory name.</param>
        /// <param name="destinationExtension">The destination Extension.</param>
        /// <returns>The destination files as a colon seperated list.</returns>
        private IEnumerable<string> GetDestinationFilePaths(ContentItem inputFile, string destinationDirectoryName, string destinationExtension)
        {
            if (inputFile.Pivots == null || !inputFile.Pivots.Any())
            {
                return new[] { GetContentPivotDestinationFilePath(inputFile.RelativeContentPath, destinationDirectoryName, destinationExtension) };
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

            return fileNames;
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

            var imageHasher = this.GetImageFileHasher(destinationDirectory, imageExtensions);
            var cssHasher = GetFileHasher(this.context, cssHashedOutputPath, cssLogPath, FileTypes.CSS, this.context.Configuration.ApplicationRootDirectory);

            var totalSuccess = this.context.SectionedAction(SectionIdParts.EverythingActivity, SectionIdParts.Css)
                .MakeCachable(new { cssFileSets, sourceDirectory, destinationDirectory, configType, imageExtensions, imageDirectories }, true)
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

        private FileHasherActivity GetImageFileHasher(string destinationDirectory, IList<string> imageExtensions)
        {
            return GetFileHasher(
                this.context,
                Path.Combine(destinationDirectory, ImagesDestinationDirectoryName),
                this.imagesLogFile,
                FileTypes.Image,
                destinationDirectory,
                "../../", // The hashed image path relative to the css path.
                imageExtensions);
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
            var cssSpritingConfig = cssFileSet.ImageSpriting.GetNamedConfig(configType);
            var cssMinificationConfig = cssFileSet.Minification.GetNamedConfig(configType);
            var cssFileSetVarBySettings = new
            {
                configType,
                ImageSpriting = cssSpritingConfig,
                Global = cssFileSet.GlobalConfig,
                Bundling = cssFileSet.Bundling.GetNamedConfig(configType),
                Minification = cssMinificationConfig,
                Preprocessing = cssFileSet.Preprocessing.GetNamedConfig(configType),
                cssFileSet.Locales,
                cssFileSet.Themes
            };

            return this.context
                .SectionedAction(SectionIdParts.CssFileSet)
                .MakeCachable(cssFileSet, cssFileSetVarBySettings, true)
                .WhenSkipped(cacheSection => EnsureCssLogFile(cssHasher, imageHasher, cacheSection))
                .RestoreFromCacheAction(cacheSection =>
                {
                    cssFileSet.LoadedConfigurationFiles.ForEach(cacheSection.AddSourceDependency);

                    var hashedContentItems = cacheSection.GetCachedContentItems(CacheFileCategories.HashedMinifiedCssResult);
                    hashedContentItems.ForEach(ci => ci.WriteToRelativeHashedPath(this.context.Configuration.DestinationDirectory));
                    EnsureLogFile(cssHasher, hashedContentItems);

                    var hashedImages = cacheSection.GetCachedContentItems(CacheFileCategories.HashedImage);
                    hashedImages.ForEach(ci => ci.WriteToRelativeHashedPath(this.context.Configuration.DestinationDirectory));
                    EnsureLogFile(imageHasher, hashedImages);

                    var hashedSprites = cacheSection.GetCachedContentItems(CacheFileCategories.HashedSpriteImage);
                    hashedSprites.ForEach(ci => ci.WriteToContentPath(this.context.Configuration.DestinationDirectory));

                    return hashedContentItems.Any();
                })
                .Execute(cacheSection =>
                {
                    var cssMinifier = this.CreateCssMinifier(imageHasher, imageExtensions, imageDirectories, imagesDestinationDirectory, cssMinificationConfig, cssSpritingConfig);
                    var outputFile = Path.Combine(this.staticAssemblerDirectory, cssFileSet.Output);
                    var inputFiles = this.Bundle(cssFileSet, outputFile, FileTypes.CSS, configType, cssMinifier.ShouldMinify);
                    if (inputFiles == null)
                    {
                        return false;
                    }

                    // localization
                    this.context.Log.Information(ResourceStrings.ResolvingTokensAndPerformingLocalization);

                    // Resolve resources
                    var localeResources = GetLocaleResources(cssFileSet, this.context, FileTypes.CSS);
                    var themeResources = GetThemeResources(cssFileSet, this.context, FileTypes.CSS);

                    // Localize the css
                    var localizedAndThemedCssItems = this.LocalizeAndThemeCss(
                        inputFiles,
                        localeResources,
                        themeResources,
                        cssMinifier.ShouldMinify);

                    this.context.Log.Information(ResourceStrings.MinifyingCssFilesAndSpritingBackgroundImages);
                    var localizeSuccess = localizedAndThemedCssItems.All(l => l != null);

                    if (!localizeSuccess)
                    {
                        // localization failed for this batch
                        this.context.Log.Error(null, ResourceStrings.ThereWereErrorsWhileMinifyingTheCssFiles);
                        return false;
                    }

                    var minifiedCssItems = this.MinifyCss(localizedAndThemedCssItems, cssMinifier, imageHasher, cssSpritingConfig.WriteLogFile);

                    if (minifiedCssItems.Any(i => i == null))
                    {
                        // localization failed for this batch
                        this.context.Log.Error(null, ResourceStrings.ThereWereErrorsWhileMinifyingTheCssFiles);
                        return false;
                    }

                    var hashedCssItems = this.HashContentItems(cssHasher, minifiedCssItems.Select(i => i.Css), CssDestinationDirectoryName, Strings.Css);
                    hashedCssItems.ForEach(hi => cacheSection.AddResult(hi, CacheFileCategories.HashedMinifiedCssResult));

                    var hashedImageItems = minifiedCssItems.SelectMany(mci => mci.HashedImages);
                    hashedImageItems.ForEach(hi => cacheSection.AddResult(hi, CacheFileCategories.HashedImage));

                    return true;
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
                .MakeCachable(varBySettings, true)
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
                .MakeCachable(jsFileSet, new { configType }, true)
                .RestoreFromCacheAction(cacheSection =>
                {
                    var hashedContentItems = cacheSection.GetCachedContentItems(CacheFileCategories.HashedMinifiedJsResult);
                    hashedContentItems.ForEach(ci => ci.WriteToRelativeHashedPath(this.context.Configuration.DestinationDirectory));
                    EnsureLogFile(jsHasher, hashedContentItems);
                    return hashedContentItems.Any();
                })
                .WhenSkipped(cacheSection => EnsureJsLogFile(jsHasher, cacheSection))
                .Execute(cacheSection =>
                    {
                        jsFileSet.LoadedConfigurationFiles.ForEach(cacheSection.AddSourceDependency);
                        var jsMinificationConfig = jsFileSet.Minification.GetNamedConfig(configType);
                        var outputFile = Path.Combine(this.staticAssemblerDirectory, jsFileSet.Output);

                        // bundling
                        var bundledFiles = this.Bundle(jsFileSet, outputFile, FileTypes.JS, configType, jsMinificationConfig.ShouldMinify);

                        if (bundledFiles == null)
                        {
                            return false;
                        }

                        // resolve the resources
                        var localeResources = GetLocaleResources(jsFileSet, this.context, FileTypes.JS);

                        // localize
                        this.context.Log.Information(ResourceStrings.ResolvingTokensAndPerformingLocalization);
                        var localizedJsFiles = this.LocalizeJs(bundledFiles, jsFileSet.Locales, localeResources);
                        if (localizedJsFiles == null)
                        {
                            this.context.Log.Error(null, "There were errors encountered while resolving tokens.");
                            return false;
                        }

                        this.context.Log.Information("Minimizing javascript files");

                        var minifiedContentItems = this.MinifyJs(
                            localizedJsFiles,
                            jsMinificationConfig,
                            jsFileSet.Validation.GetNamedConfig(configType));

                        if (minifiedContentItems.Any(ci => ci == null))
                        {
                            this.context.Log.Error(
                                null, "There were errors encountered while minimizing javascript files.");

                            return false;
                        }

                        var hashedContentItems = this.HashContentItems(jsHasher, minifiedContentItems, JsDestinationDirectoryName, Strings.JS);
                        hashedContentItems.ForEach(ci => cacheSection.AddResult(ci, CacheFileCategories.HashedMinifiedJsResult, true));

                        return true;
                    });
        }

        /// <summary>The hash content items.</summary>
        /// <param name="hasher">The hasher.</param>
        /// <param name="contentItems">The content items.</param>
        /// <param name="destinationDirectoryName">The destination directory name.</param>
        /// <param name="destinationExtension">The destination extension.</param>
        /// <returns>The hashed content items</returns>
        private IEnumerable<ContentItem> HashContentItems(FileHasherActivity hasher, IEnumerable<ContentItem> contentItems, string destinationDirectoryName, string destinationExtension)
        {
            var hashedFiles = new List<ContentItem>();
            foreach (var contentItem in contentItems)
            {
                var destinationFilePaths = this.GetDestinationFilePaths(contentItem, destinationDirectoryName, destinationExtension);
                hashedFiles.AddRange(hasher.Hash(contentItem, destinationFilePaths));
            }

            return hashedFiles;
        }

        /// <summary>Executes bundling.</summary>
        /// <param name="fileSet">The file set.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="fileType">The file type.</param>
        /// <param name="configType">The config type.</param>
        /// <param name="minimalOutput">Is the ggoal to have the most minimal output (true skips lots of comments)</param>
        /// <returns>The resulting files.</returns>
        private IEnumerable<ContentItem> Bundle(IFileSet fileSet, string outputFile, FileTypes fileType, string configType, bool minimalOutput)
        {
            var bundleConfig = fileSet.Bundling.GetNamedConfig(configType);
            var preprocessingConfig = fileSet.Preprocessing.GetNamedConfig(this.context.Configuration.ConfigType);

            if (bundleConfig.ShouldBundleFiles)
            {
                this.context.Log.Information(ResourceStrings.BundlingFiles);
                var resultFile = this.BundleFiles(fileSet.InputSpecs, outputFile, preprocessingConfig, fileType, minimalOutput || bundleConfig.MinimalOutput);
                if (resultFile == null)
                {
                    // bundling failed
                    this.context.Log.Error(null, ResourceStrings.ThereWereErrorsWhileBundlingFiles);
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
        /// <param name="imageHasher">The image hasher.</param>
        /// <param name="imageExtensions">The image Extensions.</param>
        /// <param name="imageDirectories">The image Directories.</param>
        /// <param name="imagesDestinationDirectory">The images Destination Directory.</param>
        /// <param name="minificationConfig">The minification Config.</param>
        /// <param name="spritingConfig">The spriting Config.</param>
        /// <returns>The <see cref="MinifyCssActivity"/>.</returns>
        private MinifyCssActivity CreateCssMinifier(FileHasherActivity imageHasher, IList<string> imageExtensions, IList<string> imageDirectories, string imagesDestinationDirectory, CssMinificationConfig minificationConfig, CssSpritingConfig spritingConfig)
        {
            var cssMinifier = new MinifyCssActivity(this.context)
                                  {
                                      ShouldAssembleBackgroundImages = spritingConfig.ShouldAutoSprite,
                                      ShouldMinify = minificationConfig.ShouldMinify,
                                      ShouldMergeMediaQueries = minificationConfig.ShouldMergeMediaQueries,
                                      ShouldOptimize = minificationConfig.ShouldMinify || minificationConfig.ShouldOptimize,
                                      ShouldValidateForLowerCase = minificationConfig.ShouldValidateLowerCase,
                                      ShouldExcludeProperties = minificationConfig.ShouldExcludeProperties,
                                      ImageExtensions = imageExtensions,
                                      ImageDirectories = imageDirectories,
                                      BannedSelectors = new HashSet<string>(minificationConfig.RemoveSelectors.ToArray()),
                                      HackSelectors = new HashSet<string>(minificationConfig.ForbiddenSelectors.ToArray()),
                                      ImageAssembleReferencesToIgnore = new HashSet<string>(spritingConfig.ImagesToIgnore.ToArray()),
                                      ImageAssemblyPadding = spritingConfig.ImagePadding,
                                      ErrorOnInvalidSprite = spritingConfig.ErrorOnInvalidSprite,
                                      OutputUnit = spritingConfig.OutputUnit,
                                      OutputUnitFactor = spritingConfig.OutputUnitFactor,
                                      ImagesOutputDirectory = imagesDestinationDirectory,
                                      IgnoreImagesWithNonDefaultBackgroundSize = spritingConfig.IgnoreImagesWithNonDefaultBackgroundSize,
                                      ImageBasePrefixToRemoveFromOutputPathInLog = imageHasher != null ? imageHasher.BasePrefixToRemoveFromOutputPathInLog : null,
                                      ImageBasePrefixToAddToOutputPath = imageHasher != null ? imageHasher.BasePrefixToAddToOutputPath : null,
                                      ForcedSpritingImageType = spritingConfig.ForceImageType
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
        /// <param name="inputCssItems">The css content input item.</param>
        /// <param name="minifier">The css minifier</param>
        /// <param name="imageHasher">The image Hasher.</param>
        /// <param name="writeSpriteLogFile">If set to tue, it will write a log file for each sprite.</param>
        /// <returns>True is successfull false if not.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private IEnumerable<MinifyCssResult> MinifyCss(IEnumerable<ContentItem> inputCssItems, MinifyCssActivity minifier, FileHasherActivity imageHasher, bool writeSpriteLogFile)
        {
            var results = new List<MinifyCssResult>();
            foreach (var inputFile in inputCssItems)
            {
                var sourceFile = inputFile.RelativeContentPath;
                var destinationFile = inputFile.RelativeContentPath;

                var pivots = inputFile.Pivots.Select(p => p.ToString());
                var pivotFileExtensions = string.Join(".", pivots);
                this.context.Log.Information("Css Minify start: {0} : {1}".InvariantFormat(destinationFile, pivotFileExtensions));

                minifier.SourceFile = sourceFile;
                minifier.DestinationFile = destinationFile;

                if (writeSpriteLogFile)
                {
                    var firstPivot = pivots.FirstOrDefault();
                    if (firstPivot != null)
                    {
                        firstPivot = "." + firstPivot;
                    }

                    minifier.ImageSpritingLogPath = Path.Combine(this.context.Configuration.ReportPath, destinationFile + firstPivot + ".spritingLog.xml");
                }

                try
                {
                    // execute the minifier on the css.
                    results.Add(minifier.Process(inputFile, imageHasher));
                }
                catch (Exception ex)
                {
                    results.Add(null);
                    AggregateException aggEx;

                    if ((aggEx = ex as AggregateException) != null || ((ex.InnerException != null) && (aggEx = ex.InnerException as AggregateException) != null))
                    {
                        // antlr can throw a blob of errors, so they need to be deduped to get the real set of errors
                        var errors = aggEx.CreateBuildErrors(sourceFile);
                        foreach (var error in errors)
                        {
                            this.HandleError(inputFile, error, sourceFile);
                        }
                    }
                    else
                    {
                        // Catch, record and display error
                        this.HandleError(inputFile, ex, sourceFile);
                    }
                }
            }

            return results;
        }

        /// <summary>Minify js activity</summary>
        /// <param name="inputFiles">path to localized js files to be minified</param>
        /// <param name="jsConfig">The js Config.</param>
        /// <param name="jsValidateConfig">The js Validate Config.</param>
        /// <returns>True if successfull false if not.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private IEnumerable<ContentItem> MinifyJs(IEnumerable<ContentItem> inputFiles, JsMinificationConfig jsConfig, JSValidationConfig jsValidateConfig)
        {
            var results = new List<ContentItem>();
            var minifier = new MinifyJSActivity(this.context)
            {
                ShouldMinify = jsConfig.ShouldMinify,
                ShouldAnalyze = jsValidateConfig.ShouldAnalyze,
                AnalyzeArgs = jsValidateConfig.AnalyzeArguments
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

            foreach (var inputFile in inputFiles)
            {
                var sourceFile = inputFile.RelativeContentPath;
                if (!inputFile.Pivots.All(this.context.TemporaryIgnore))
                {
                    this.context.Log.Information("Js Minify start: {0}{1}".InvariantFormat(sourceFile, string.Join(string.Empty, inputFile.Pivots.Select(p => p.ToString()))));
                    try
                    {
                        results.Add(minifier.Minify(inputFile));
                    }
                    catch (Exception ex)
                    {
                        results.Add(null);
                        this.HandleError(inputFile, ex, sourceFile);
                    }
                }
            }

            return results;
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
                    this.HandleError(jsFile, ex, jsFile.RelativeContentPath);
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
                    this.HandleError(cssInputItem, ex, cssInputItem.RelativeContentPath);
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
        /// <param name="minimalOutput">Is the ggoal to have the most minimal output (true skips lots of comments)</param>
        /// <returns>a value indicating whether the operation was successful</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Need to catch all in order to log errors.")]
        private ContentItem BundleFiles(IEnumerable<InputSpec> inputSpecs, string outputFile, PreprocessingConfig preprocessing, FileTypes fileType, bool minimalOutput)
        {
            // now we have the input prepared, so use Assembler activity to create the one file to use as input (if we were't assembling, we'd need to grab all) 
            // we are bundling either JS or CSS files -- for JS files we want to append semicolons between them and use single-line comments; for CSS file we don't.
            var assemblerActivity = new AssemblerActivity(this.context)
                {
                    PreprocessingConfig = preprocessing,
                    AddSemicolons = fileType == FileTypes.JS,
                    MinimalOutput = minimalOutput
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
                this.HandleError(null, ex);
            }

            return null;
        }

        /// <summary>
        /// general handler for errors
        /// </summary>
        /// <param name="contentItem">The content item.</param>
        /// <param name="ex">exception caught</param>
        /// <param name="file">File being processed that caused the error.</param>
        /// <param name="message">message to be shown (instead of Exception.Message)</param>
        private void HandleError(ContentItem contentItem, Exception ex, string file = null, string message = null)
        {
            if (ex.InnerException is BuildWorkflowException)
            {
                ex = ex.InnerException;
            }

            var bwe = ex as BuildWorkflowException;
            if (contentItem != null && bwe != null)
            {
                bwe.File = this.context.EnsureErrorFileOnDisk(bwe.File, contentItem);
                ex = bwe;
            }

            if (!string.IsNullOrWhiteSpace(file) && (bwe == null || bwe.File.IsNullOrWhitespace()))
            {
                this.context.Log.Error(null, string.Format(CultureInfo.InvariantCulture, ResourceStrings.ErrorsInFileFormat, file), file);
            }

            this.context.Log.Error(ex, message);
        }
    }
}
