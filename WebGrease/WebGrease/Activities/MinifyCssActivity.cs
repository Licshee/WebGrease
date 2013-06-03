// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MinifyCssActivity.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Implements the multiple steps in Css pipeline
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Common;
    using Css;
    using Css.Ast;
    using Css.Extensions;
    using Css.ImageAssemblyAnalysis;
    using Css.Visitor;
    using ImageAssemble;

    using WebGrease.Css.ImageAssemblyAnalysis.LogModel;
    using WebGrease.Extensions;

    /// <summary>Implements the multiple steps in Css pipeline</summary>
    internal sealed class MinifyCssActivity
    {
        /// <summary>The url hash all regex pattern.</summary>
        private static readonly Regex UrlHashAllRegexPattern = new Regex(@"url\((?<quote>[""']?)hash://(?<url>.*?)\k<quote>\)", RegexOptions.Compiled);

        /// <summary>The url hash regex pattern.</summary>
        private static readonly Regex UrlHashRegexPattern = new Regex(@"url\((?<quote>[""']?)(?:hash\((?<url>[^)]*))\)\k<quote>\)", RegexOptions.Compiled);

        /// <summary>The context.</summary>
        private readonly IWebGreaseContext context;

        /// <summary>The available source images.</summary>
        private IDictionary<string, string> availableSourceImages;

        /// <summary>Initializes a new instance of the <see cref="MinifyCssActivity"/> class.</summary>
        /// <param name="context">The context.</param>
        internal MinifyCssActivity(IWebGreaseContext context)
        {
            this.context = context;
            this.HackSelectors = new HashSet<string>();
            this.BannedSelectors = new HashSet<string>();
            this.ShouldExcludeProperties = true;
            this.ShouldValidateForLowerCase = false;
            this.ShouldOptimize = true;
            this.ShouldAssembleBackgroundImages = true;
            this.AdditionalImageAssemblyBuckets = new List<ImageAssemblyScanInput>();
            this.ImageAssembleReferencesToIgnore = new HashSet<string>();
            this.OutputUnitFactor = 1;
        }

        /// <summary>Gets or sets the additional image assembly buckets.</summary>
        internal List<ImageAssemblyScanInput> AdditionalImageAssemblyBuckets { get; set; }

        /// <summary>Gets or sets the image base prefix to remove from output path in log.</summary>
        internal string ImageBasePrefixToRemoveFromOutputPathInLog { get; set; }

        /// <summary>Gets or sets the image base prefix to add to output path.</summary>
        internal string ImageBasePrefixToAddToOutputPath { get; set; }

        /// <summary>Gets OutputUnit (Default: px, other possible values: rem/em etc..).</summary>
        internal string OutputUnit { private get; set; }

        /// <summary>Gets OutputUnitFactor (Default: 1, example value for 10px based REM: 0.625</summary>
        internal double OutputUnitFactor { private get; set; }

        /// <summary>Gets or sets a value indicating whether to ignore images that have a background-size property set to non-default ('auto' or 'auto auto').</summary>
        internal bool IgnoreImagesWithNonDefaultBackgroundSize { private get; set; }

        /// <summary>Gets or sets the image directories.</summary>
        internal IList<string> ImageDirectories { private get; set; }

        /// <summary>Gets or sets the image extensions.</summary>
        internal IList<string> ImageExtensions { private get; set; }

        /// <summary>Gets or sets Source File.</summary>
        internal string SourceFile { private get; set; }

        /// <summary>Gets or sets Destination File.</summary>
        internal string DestinationFile { get; set; }

        /// <summary>Gets or sets a value indicating whether css should exclude properties marked with "Exclude".</summary>
        internal bool ShouldExcludeProperties { get; set; }

        /// <summary>Gets or sets a value indicating whether css should be validated for lower case.</summary>
        internal bool ShouldValidateForLowerCase { get; set; }

        /// <summary>Gets or sets a value indicating whether css should be optimized for colors, float, duplicate selectors etc.</summary>
        internal bool ShouldOptimize { private get; set; }

        /// <summary>Gets or sets whether to assemble CSS background Images and update the coordinates.</summary>
        internal bool ShouldAssembleBackgroundImages { private get; set; }

        /// <summary>Gets or sets a value indicating whether ShouldMinify.</summary>
        internal bool ShouldMinify { get; set; }

        /// <summary>Gets HackSelectors.</summary>
        internal HashSet<string> HackSelectors { get; set; }

        /// <summary>Gets BannedSelectors.</summary>
        internal HashSet<string> BannedSelectors { get; set; }

        /// <summary>Gets or sets Image Assembly Scan Output.</summary>
        /// <remarks>Optional - Needed only when image spriting is needed.</remarks>
        internal string ImageAssembleScanDestinationFile { get; set; }

        /// <summary>Gets or sets the image output directory.</summary>
        internal string ImagesOutputDirectory { private get; set; }

        /// <summary>Gets ImageAssembleReferencesToIgnore.</summary>
        internal HashSet<string> ImageAssembleReferencesToIgnore { get; set; }

        /// <summary>Gets or sets the image assembly padding.</summary>
        internal string ImageAssemblyPadding { private get; set; }

        /// <summary>Gets the exception, if any, returned from a parsing attempt.</summary>
        internal Exception ParserException { get; private set; }

        /// <summary>The process.</summary>
        /// <param name="contentItem">The content item.</param>
        /// <param name="imageHasher">The image hasher.</param>
        /// <returns>The <see cref="MinifyCssResult"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Catch all by default")]
        internal MinifyCssResult Process(ContentItem contentItem, FileHasherActivity imageHasher = null)
        {
            if (imageHasher != null)
            {
                this.availableSourceImages = this.context.GetAvailableFiles(this.context.Configuration.SourceDirectory, this.ImageDirectories, this.ImageExtensions, FileTypes.Image);
            }

            ContentItem minifiedContentItem = null;
            var hashedImageContentItems = new List<ContentItem>();
            var spritedImageContentItems = new List<ContentItem>();
            this.context
                .SectionedAction(SectionIdParts.MinifyCssActivity)
                .CanBeCached(contentItem, this.GetVarBySettings(imageHasher))
                .RestoreFromCacheAction(cacheSection =>
                {
                    minifiedContentItem = cacheSection.GetCachedContentItem(CacheFileCategories.MinifiedCssResult, this.DestinationFile, null, contentItem.Pivots);
                    hashedImageContentItems.AddRange(cacheSection.GetCachedContentItems(CacheFileCategories.HashedImage));
                    spritedImageContentItems.AddRange(cacheSection.GetCachedContentItems(CacheFileCategories.HashedSpriteImage));

                    return minifiedContentItem != null;
                })
                .Execute(cacheSection =>
                    {
                        var cssContent = contentItem.Content;

                        if (imageHasher != null)
                        {
                            // Pre hash images, replace all hash( with hash:// to make it valid css.
                            cssContent = PreHashImages(this.context, cssContent);
                        }

                        // Load the Css parser and stylesheet Ast
                        var stylesheetNode = this.context.SectionedAction(SectionIdParts.MinifyCssActivity, SectionIdParts.Parse)
                            .Execute(() => CssParser.Parse(cssContent, false));

                        // Apply all configured visitors, including, validating, optimizing, minifying and spriting.
                        var visitorResult = this.ApplyConfiguredVisitors(stylesheetNode);
                        var processedCssContent = visitorResult.Item1;
                        spritedImageContentItems.AddRange(visitorResult.Item2);

                        if (imageHasher != null)
                        {
                            var hashResult = this.HashImages(processedCssContent, imageHasher, cacheSection);
                            processedCssContent = hashResult.Item1;
                            hashedImageContentItems.AddRange(hashResult.Item2);
                        }

                        minifiedContentItem = ContentItem.FromContent(processedCssContent, this.DestinationFile, null, contentItem.Pivots != null ? contentItem.Pivots.ToArray() : null);
                        cacheSection.AddResult(minifiedContentItem, CacheFileCategories.MinifiedCssResult);
                        return minifiedContentItem != null;
                    });

            return new MinifyCssResult(minifiedContentItem, spritedImageContentItems, hashedImageContentItems);
        }

        /// <summary>Executes the task in a build style workflow, i.e. with a file path in and destination file set for out.</summary>
        /// <param name="contentItem">The content Item.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Needs refactoring into multiple classes")]
        internal void Execute(ContentItem contentItem = null)
        {
            if (contentItem == null)
            {
                if (string.IsNullOrWhiteSpace(this.SourceFile))
                {
                    throw new ArgumentException("MinifyCssActivity - The source file cannot be null or whitespace.");
                }

                if (!File.Exists(this.SourceFile))
                {
                    throw new FileNotFoundException("MinifyCssActivity - The source file cannot be found.", this.SourceFile);
                }
            }

            if (string.IsNullOrWhiteSpace(this.DestinationFile))
            {
                throw new ArgumentException("MinifyCssActivity - The destination file cannot be null or whitespace.");
            }

            if (contentItem == null)
            {
                contentItem = ContentItem.FromFile(this.SourceFile, Path.IsPathRooted(this.SourceFile) ? this.SourceFile.MakeRelativeToDirectory(this.context.Configuration.SourceDirectory) : this.SourceFile);
            }

            try
            {
                var minifyresult = this.Process(contentItem);

                if (minifyresult.Css != null)
                {
                    minifyresult.Css.WriteTo(this.DestinationFile);
                }

                if (minifyresult.SpritedImages != null && minifyresult.SpritedImages.Any())
                {
                    foreach (var spritedImage in minifyresult.SpritedImages)
                    {
                        spritedImage.WriteToContentPath(this.context.Configuration.DestinationDirectory);
                    }
                }

                if (minifyresult.HashedImages != null && minifyresult.HashedImages.Any())
                {
                    foreach (var hashedImage in minifyresult.HashedImages)
                    {
                        hashedImage.WriteToRelativeHashedPath(this.context.Configuration.DestinationDirectory);
                    }
                }
            }
            catch (Exception exception)
            {
                this.ParserException = exception;
                throw;
            }
        }

        /// <summary>The pre hash images.</summary>
        /// <param name="context">The context.</param>
        /// <param name="cssContent">The css content.</param>
        /// <returns>The <see cref="string"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Css image requires lowercase.")]
        private static string PreHashImages(IWebGreaseContext context, string cssContent)
        {
            // TODO:RTUIT: Optimize to not use regex, or enable the parser to support hash( and uppercase in the url
            return
                context.SectionedAction(SectionIdParts.MinifyCssActivity, SectionIdParts.ImageHash)
                       .Execute(() => UrlHashRegexPattern.Replace(cssContent, m => "url('hash://" + m.Groups["url"].Value.ToLowerInvariant() + "')"));
        }

        /// <summary>Hash the images.</summary>
        /// <param name="cssContent">The css content.</param>
        /// <param name="imageHasher">The image Hasher.</param>
        /// <param name="cacheSection">The cache section.</param>
        /// <returns>The css with hashed images.</returns>
        private Tuple<string, IEnumerable<ContentItem>> HashImages(string cssContent, FileHasherActivity imageHasher, ICacheSection cacheSection)
        {
            return this.context.SectionedAction(SectionIdParts.MinifyCssActivity, SectionIdParts.ImageHash)
                .Execute(() =>
                    {
                        var contentImagesToHash = new HashSet<string>();
                        var hashedContentItems = new List<ContentItem>();
                        var hashedImages = new Dictionary<string, string>();
                        cssContent = UrlHashAllRegexPattern.Replace(
                            cssContent,
                            match =>
                            {
                                var urlToHash = match.Groups["url"].Value;
                                var normalizedHashUrl = urlToHash.NormalizeUrl();
                                var imageContentFile = this.availableSourceImages.ContainsKey(normalizedHashUrl)
                                                           ? this.availableSourceImages[normalizedHashUrl]
                                                           : null;

                                if (imageContentFile == null)
                                {
                                    throw new BuildWorkflowException("Could not find a macthing source image for url: {0}".InvariantFormat(urlToHash));
                                }

                                if (contentImagesToHash.Add(normalizedHashUrl))
                                {
                                    // Add the image as end result
                                    var imageContentItem = ContentItem.FromFile(imageContentFile, normalizedHashUrl);
                                    imageContentItem = imageHasher.Hash(imageContentItem);
                                    cacheSection.AddSourceDependency(imageContentFile);
                                    hashedContentItems.Add(imageContentItem);

                                    imageContentFile =
                                        Path.Combine(
                                            imageHasher.BasePrefixToAddToOutputPath ?? Path.AltDirectorySeparatorChar.ToString(CultureInfo.InvariantCulture),
                                            imageContentItem.RelativeHashedContentPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                                    hashedImages.Add(normalizedHashUrl, imageContentFile);
                                }
                                else
                                {
                                    imageContentFile = hashedImages[normalizedHashUrl];
                                }

                                return "url(" + imageContentFile + ")";
                            });

                        return Tuple.Create(cssContent, (IEnumerable<ContentItem>)hashedContentItems);
                    });
        }

        /// <summary>Gets the unique object for all the settings, used for determining unique cache id.</summary>
        /// <param name="imageHasher">The image Hasher.</param>
        /// <returns>The settings object.</returns>
        private object GetVarBySettings(FileHasherActivity imageHasher)
        {
            return new
            {
                this.ShouldExcludeProperties,
                this.ShouldValidateForLowerCase,
                this.ShouldOptimize,
                this.ShouldAssembleBackgroundImages,
                this.ShouldMinify,
                this.IgnoreImagesWithNonDefaultBackgroundSize,
                this.HackSelectors,
                this.BannedSelectors,
                this.ImageAssembleReferencesToIgnore,
                this.OutputUnit,
                this.OutputUnitFactor,
                this.ImageAssemblyPadding,
                HashImages = imageHasher == null
            };
        }

        /// <summary>
        /// Calls each configured visitor and returns printed results when finished.
        /// </summary>
        /// <param name="stylesheetNode">The node for vistors to visit.</param>
        /// <returns>Processed node either minified or pretty printed.</returns>
        private Tuple<string, IEnumerable<ContentItem>> ApplyConfiguredVisitors(AstNode stylesheetNode)
        {
            this.context.SectionedAction(SectionIdParts.MinifyCssActivity, SectionIdParts.Validate).Execute(() =>
            {
                // Step # 1 - Remove the Css properties from Ast which need to be excluded (Bridging)
                if (this.ShouldExcludeProperties)
                {
                    stylesheetNode = stylesheetNode.Accept(new ExcludePropertyVisitor());
                }

                // Step # 2 - Validate for lower case
                if (this.ShouldValidateForLowerCase)
                {
                    stylesheetNode = stylesheetNode.Accept(new ValidateLowercaseVisitor());
                }

                // Step # 3 - Validate for Css hacks which don't work cross browser
                if (this.HackSelectors != null && this.HackSelectors.Any())
                {
                    stylesheetNode = stylesheetNode.Accept(new SelectorValidationOptimizationVisitor(this.HackSelectors, false, true));
                }

                // Step # 4 - Remove any banned selectors which are exposed for page efficiency
                if (this.BannedSelectors != null && this.BannedSelectors.Any())
                {
                    stylesheetNode = stylesheetNode.Accept(new SelectorValidationOptimizationVisitor(this.BannedSelectors, false, false));
                }
            });

            // Step # 5 - Run the Css optimization visitors
            if (this.ShouldOptimize)
            {
                this.context.SectionedAction(SectionIdParts.MinifyCssActivity, SectionIdParts.Optimize).Execute(() =>
                {
                    stylesheetNode = stylesheetNode.Accept(new OptimizationVisitor());
                    stylesheetNode = stylesheetNode.Accept(new ColorOptimizationVisitor());
                    stylesheetNode = stylesheetNode.Accept(new FloatOptimizationVisitor());
                });
            }

            var spritedImageContentItems = new List<ContentItem>();

            // The image assembly is a 3 step process:
            // 1. Scan the Css followed by Pretty Print
            // 2. Run the image assembly tool
            // 3. Update the Css with generated images followed by Pretty Print
            if (this.ShouldAssembleBackgroundImages)
            {
                var spritingResult = this.SpriteBackgroundImages(stylesheetNode);
                stylesheetNode = spritingResult.Item1;
                spritedImageContentItems.AddRange(spritingResult.Item2);
            }

            var processedCss = this.context.SectionedAction(SectionIdParts.MinifyCssActivity, SectionIdParts.PrintCss).Execute(() => this.ShouldMinify ? stylesheetNode.MinifyPrint() : stylesheetNode.PrettyPrint());
            return Tuple.Create(processedCss, (IEnumerable<ContentItem>)spritedImageContentItems);
        }

        /// <summary>
        /// Assembles the background images
        /// </summary>
        /// <param name="stylesheetNode">the style sheet node</param>
        /// <returns>The stylesheet node with the sprited images aplied.</returns>
        private Tuple<AstNode, IEnumerable<ContentItem>> SpriteBackgroundImages(AstNode stylesheetNode)
        {
            return this.context.SectionedAction(SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting).Execute(() =>
            {
                var results = new List<ContentItem>();

                // Execute the pipeline for image assembly scan
                var scanLog = this.ExecuteImageAssemblyScan(stylesheetNode);

                // Execute the pipeline for image assembly tool
                var imageMaps = new List<ImageLog>();
                var count = 0;
                foreach (var scanOutput in scanLog.ImageAssemblyScanOutputs)
                {
                    var spriteResult = this.SpriteImageFromLog(scanOutput, this.ImageAssembleScanDestinationFile + (count == 0 ? string.Empty : "." + count) + ".xml");
                    if (spriteResult != null && spriteResult.Item1 != null)
                    {
                        imageMaps.Add(spriteResult.Item1);
                        count++;
                    }
                }

                // Step # 8 - Execute the pipeline for image assembly update
                stylesheetNode = this.ExecuteImageAssemblyUpdate(stylesheetNode, imageMaps);

                return Tuple.Create(stylesheetNode, (IEnumerable<ContentItem>)results);
            });
        }

        /// <summary>The sprite image.</summary>
        /// <param name="scanOutput">The scan Output.</param>
        /// <param name="mapXmlFile">The map xml file</param>
        /// <returns>The <see cref="ImageLog"/>.</returns>
        private Tuple<ImageLog, List<ContentItem>> SpriteImageFromLog(ImageAssemblyScanOutput scanOutput, string mapXmlFile)
        {
            if (scanOutput == null || !scanOutput.ImageReferencesToAssemble.Any())
            {
                return null;
            }

            ImageLog imageLog = null;
            var imageReferencesToAssemble = scanOutput.ImageReferencesToAssemble;
            if (imageReferencesToAssemble == null || imageReferencesToAssemble.Count == 0)
            {
                return null;
            }

            var results = new List<ContentItem>();

            var success = this.context.SectionedAction(SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting, SectionIdParts.Assembly)
                .CanBeCached(this.GetRelativeSpriteCacheKey(imageReferencesToAssemble))
                .RestoreFromCacheAction(cacheSection =>
                {
                    // restore log file, is required by next step in applying sprites to the css.
                    var destinationDirectory = this.context.Configuration.DestinationDirectory;

                    var spriteLogFileContentItem = cacheSection.GetCachedContentItems(CacheFileCategories.SpriteLogFile).FirstOrDefault();
                    if (spriteLogFileContentItem == null)
                    {
                        return true;
                    }

                    if (!string.IsNullOrWhiteSpace(this.ImageAssembleScanDestinationFile))
                    {
                        var spriteLogFileXmlContentItem = cacheSection.GetCachedContentItems(CacheFileCategories.SpriteLogFileXml).FirstOrDefault();
                        if (spriteLogFileXmlContentItem != null)
                        {
                            spriteLogFileXmlContentItem.WriteTo(mapXmlFile);
                        }
                    }

                    imageLog = spriteLogFileContentItem.Content.FromJson<ImageLog>(true);

                    // Restore images
                    var spritedImageContentItems = cacheSection.GetCachedContentItems(CacheFileCategories.HashedSpriteImage);
                    spritedImageContentItems.ForEach(sici => sici.WriteToContentPath(destinationDirectory));
                    results.AddRange(spritedImageContentItems);

                    return true;
                })
                .Execute(cacheSection =>
                {
                    if (!Directory.Exists(this.ImagesOutputDirectory))
                    {
                        Directory.CreateDirectory(this.ImagesOutputDirectory);
                    }

                    var imageMap = ImageAssembleGenerator.AssembleImages(
                        imageReferencesToAssemble.ToSafeReadOnlyCollection(),
                        SpritePackingType.Vertical,
                        this.ImagesOutputDirectory,
                        string.Empty,
                        true,
                        this.context);

                    if (imageMap == null || imageMap.Document == null)
                    {
                        return true;
                    }

                    var destinationDirectory = this.context.Configuration.DestinationDirectory;
                    if (!string.IsNullOrWhiteSpace(this.ImageAssembleScanDestinationFile))
                    {
                        var scanXml = imageMap.Document.ToString();
                        FileHelper.WriteFile(mapXmlFile, scanXml);
                        cacheSection.AddResult(
                            ContentItem.FromFile(mapXmlFile),
                            CacheFileCategories.SpriteLogFileXml);
                    }

                    imageLog = new ImageLog(imageMap.Document);

                    cacheSection.AddResult(
                        ContentItem.FromContent(imageLog.ToJson(true)),
                        CacheFileCategories.SpriteLogFile);

                    foreach (var spritedFile in imageLog.InputImages.Select(il => il.OutputFilePath))
                    {
                        var spritedImageContentItem = ContentItem.FromFile(spritedFile, spritedFile.MakeRelativeToDirectory(destinationDirectory));
                        results.Add(spritedImageContentItem);
                        cacheSection.AddResult(
                            spritedImageContentItem,
                            CacheFileCategories.HashedSpriteImage);
                    }

                    return true;
                });

            return
                success
                ? Tuple.Create(imageLog, results)
                : null;
        }

        /// <summary>The get relative sprite cache key.</summary>
        /// <param name="imageReferencesToAssemble">The image references to assemble</param>
        /// <returns>The unique cache key.</returns>
        private string GetRelativeSpriteCacheKey(IEnumerable<InputImage> imageReferencesToAssemble)
        {
            return string.Join(
                ">",
                imageReferencesToAssemble.Select(ir =>
                        "{0}|{1}|{2}".InvariantFormat(
                            this.context.MakeRelativeToApplicationRoot(ir.ImagePath),
                            ir.Position,
                            string.Join(":", ir.DuplicateImagePaths.Select(dip => this.context.MakeRelativeToApplicationRoot(dip))))));
        }

        /// <summary>Scans the css for the image path references</summary>
        /// <param name="stylesheetNode">The stylesheet node</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        private ImageAssemblyScanVisitor ExecuteImageAssemblyScan(AstNode stylesheetNode)
        {
            var imageAssemblyScanVisitor = new ImageAssemblyScanVisitor(
                this.SourceFile,
                this.ImageAssembleReferencesToIgnore,
                this.AdditionalImageAssemblyBuckets,
                this.IgnoreImagesWithNonDefaultBackgroundSize,
                this.OutputUnit,
                this.OutputUnitFactor,
                this.availableSourceImages)
            {
                Context = this.context
            };

            // Scan log visitor should and does not change the stylesheet, no need to use the result.
            stylesheetNode.Accept(imageAssemblyScanVisitor);

            // return the can log results.
            return imageAssemblyScanVisitor;
        }

        /// <summary>Executes the image assembly update visitor</summary>
        /// <param name="stylesheetNode">The stylesheet Ast node</param>
        /// <param name="imageLogs">The sprite Log Files.</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        private AstNode ExecuteImageAssemblyUpdate(AstNode stylesheetNode, IEnumerable<ImageLog> imageLogs)
        {
            var specificStyleSheet = stylesheetNode as StyleSheetNode;

            var imageAssemblyUpdateVisitor = new ImageAssemblyUpdateVisitor(
                this.SourceFile,
                imageLogs,
                specificStyleSheet == null ? 1d : specificStyleSheet.Dpi.GetValueOrDefault(1d),
                this.OutputUnit,
                this.OutputUnitFactor,
                this.ImageBasePrefixToRemoveFromOutputPathInLog,
                this.ImageBasePrefixToAddToOutputPath,
                this.availableSourceImages);

            stylesheetNode = stylesheetNode.Accept(imageAssemblyUpdateVisitor);

            // Return the updated Ast
            return stylesheetNode;
        }
    }
}
