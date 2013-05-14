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
    using System.Xml.Linq;

    using Common;
    using Css;
    using Css.Ast;
    using Css.Extensions;
    using Css.ImageAssemblyAnalysis;
    using Css.Visitor;
    using ImageAssemble;

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

        /// <summary>The image assembly scan visitor.</summary>
        private ImageAssemblyScanVisitor imageAssemblyScanVisitor;

        /// <summary>The image assembly update visitor.</summary>
        private ImageAssemblyUpdateVisitor imageAssemblyUpdateVisitor;

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

        /// <summary>Gets or sets the image hasher.</summary>
        internal FileHasherActivity ImageHasher { private get; set; }

        /// <summary>Gets or sets the css hasher.</summary>
        internal FileHasherActivity CssHasher { private get; set; }

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

        /// <summary>Gets or sets a value indicating whether it should hash the images.</summary>
        internal bool ShouldHashImages { private get; set; }

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

        /// <summary>
        /// Minifies the css content given, using current activity settings
        /// </summary>
        /// <param name="cssContent">Css to minify</param>
        /// <param name="shouldLog">whether the Css parser should try to log</param>
        /// <returns>Processed css, if possible, or umodified css if there were any errors.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "this is a 'never fail ever' case for public runtime use."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "MinifyCssActivity", Justification = "Spelt as desired")]
        internal string Execute(string cssContent, bool shouldLog = false)
        {
            if (string.IsNullOrWhiteSpace(cssContent))
            {
                return cssContent;
            }

            return this.context.SectionedAction(SectionIdParts.MinifyCssActivity, SectionIdParts.Parse).Execute(() => 
            {
                try
                {
                    AstNode stylesheetNode = CssParser.Parse(cssContent, shouldLog);
                    return this.ApplyConfiguredVisitors(stylesheetNode);
                }
                catch (Exception exception)
                {
                    this.ParserException = exception;

                    // give back the original unmodified
                    return cssContent;
                }
            });
        }

        /// <summary>Executes the task in a build style workflow, i.e. with a file path in and destination file set for out.</summary>
        /// <param name="contentItem">The content Item.</param>
        internal void Execute(ContentItem contentItem = null)
        {
            var shouldHashImages = this.ShouldHashImages && this.ImageHasher != null;
            if (shouldHashImages)
            {
                this.availableSourceImages = this.context.GetAvailableFiles(this.context.Configuration.SourceDirectory, this.ImageDirectories, this.ImageExtensions, FileTypes.Image);
            }

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
                contentItem = ContentItem.FromFile(this.SourceFile, this.SourceFile.MakeRelativeToDirectory(this.context.Configuration.SourceDirectory));
            }

            var shouldHashCss = this.CssHasher != null;
            this.context
                .SectionedAction(SectionIdParts.MinifyCssActivity)
                .CanBeCached(contentItem, this.GetVarBySettings())
                .RestoreFromCacheAction(cacheSection =>
                    RestoreAllFromCache(cacheSection, shouldHashCss, shouldHashImages, this.context.Configuration.DestinationDirectory, this.ImageHasher, this.CssHasher, this.ShouldAssembleBackgroundImages))
                .Execute(cacheSection =>
                    this.MinifyCssContentItem(contentItem, shouldHashImages, cacheSection, shouldHashCss, this.DestinationFile));
        }

        /// <summary>The restore all from cache.</summary>
        /// <param name="cacheSection">The cache section.</param>
        /// <param name="shouldHashCss">The should hash css.</param>
        /// <param name="shouldHashImages">The should hash images.</param>
        /// <param name="destinationDirectory">The destination Directory.</param>
        /// <param name="imageHasher">The image Hasher.</param>
        /// <param name="cssHasher">The css hasher</param>
        /// <param name="shouldAssembleBackgroundImages">The should Assemble Background Images.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private static bool RestoreAllFromCache(ICacheSection cacheSection, bool shouldHashCss, bool shouldHashImages, string destinationDirectory, FileHasherActivity imageHasher, FileHasherActivity cssHasher, bool shouldAssembleBackgroundImages)
        {
            var cssContentItem = cacheSection.GetCachedContentItems(CacheFileCategories.MinifiedCssResult).FirstOrDefault();
            if (cssContentItem == null)
            {
                return false;
            }

            if (shouldHashCss)
            {
                // Restore hashed css output
                cssHasher.AppendToWorkLog(cssContentItem);
                cssContentItem.WriteToHashedPath(destinationDirectory);
            }
            else
            {
                // Restore css output
                cssContentItem.WriteToContentPath(destinationDirectory, true);
            }

            if (shouldAssembleBackgroundImages)
            {
                // Restore sprited images
                var spritedImageContentItems = cacheSection.GetCachedContentItems(CacheFileCategories.HashedSpriteImage);
                spritedImageContentItems.ForEach(sici => sici.WriteToContentPath(destinationDirectory));
            }

            if (shouldHashImages)
            {
                // Restore sprited images
                var hashedImageContentItems = cacheSection.GetCachedContentItems(CacheFileCategories.HashedImage);
                hashedImageContentItems.ForEach(sici => sici.WriteToHashedPath(destinationDirectory));
                imageHasher.AppendToWorkLog(hashedImageContentItems);
            }

            return true;
        }

        /// <summary>The pre hash images.</summary>
        /// <param name="context">The context.</param>
        /// <param name="cssContent">The css content.</param>
        /// <returns>The <see cref="string"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Css image requires lowercase.")]
        private static string PreHashImages(IWebGreaseContext context, string cssContent)
        {
            // TODO:RTUIT: Optimize to not use regex, or enabled the parser to support hash( and uppercase in the url
            return
                context.SectionedAction(SectionIdParts.MinifyCssActivity, SectionIdParts.ImageHash)
                       .Execute(() => UrlHashRegexPattern.Replace(cssContent, m => "url('hash://" + m.Groups["url"].Value.ToLowerInvariant() + "')"));
        }

        /// <summary>The minify css content item.</summary>
        /// <param name="contentItem">The content item.</param>
        /// <param name="shouldHashImages">The should hash images.</param>
        /// <param name="cacheSection">The cache section.</param>
        /// <param name="shouldHashCss">The should hash css.</param>
        /// <param name="destinationFile">The destination file.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private bool MinifyCssContentItem(ContentItem contentItem, bool shouldHashImages, ICacheSection cacheSection, bool shouldHashCss, string destinationFile)
        {
            // Get the content from file.
            var cssContent = contentItem.Content;

            if (shouldHashImages)
            {
                // Pre hash images, replace all hash( with hash:// to make it valid css.
                cssContent = PreHashImages(this.context, cssContent);
            }

            // Load the Css parser and stylesheet Ast
            var stylesheetNode = this.context.SectionedAction(SectionIdParts.MinifyCssActivity, SectionIdParts.Parse)
                .Execute(() => CssParser.Parse(cssContent, false));

            // Apply all configured visitors, including, validating, optimizing, minifying and spriting.
            var css = this.ApplyConfiguredVisitors(stylesheetNode);

            if (shouldHashImages)
            {
                // Hash all images that have not been sprited.
                css = this.HashImages(css, cacheSection);
            }

            var relativeDestinationFile = Path.IsPathRooted(destinationFile)
                                              ? destinationFile.MakeRelativeToDirectory(this.context.Configuration.DestinationDirectory)
                                              : destinationFile;

            if (shouldHashCss)
            {
                // Write the result to disk using hashing.
                var hashResult = this.CssHasher.Hash(ContentItem.FromContent(css, relativeDestinationFile));
                cacheSection.AddResult(hashResult, CacheFileCategories.MinifiedCssResult, true);
            }
            else
            {
                // Write to the destination file on disk.
                FileHelper.WriteFile(this.DestinationFile, css);
                cacheSection.AddResult(ContentItem.FromContent(css, relativeDestinationFile), CacheFileCategories.MinifiedCssResult, true);
            }

            return true;
        }
        
        /// <summary>Hash the images.</summary>
        /// <param name="cssContent">The css content.</param>
        /// <param name="cacheSection">The cache section.</param>
        /// <returns>The css with hashed images.</returns>
        private string HashImages(string cssContent, ICacheSection cacheSection)
        {
            return this.context.SectionedAction(SectionIdParts.MinifyCssActivity, SectionIdParts.ImageHash)
                .Execute(() =>
                    {
                        var contentImagesToHash = new HashSet<string>();
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
                                        imageContentItem = this.ImageHasher.Hash(imageContentItem);
                                        cacheSection.AddSourceDependency(imageContentFile);
                                        cacheSection.AddResult(imageContentItem, CacheFileCategories.HashedImage, true);

                                        imageContentFile =
                                            Path.Combine(
                                                ImageHasher.BasePrefixToAddToOutputPath ?? Path.AltDirectorySeparatorChar.ToString(CultureInfo.InvariantCulture),
                                                imageContentItem.RelativeHashedContentPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                                        hashedImages.Add(normalizedHashUrl, imageContentFile);
                                    }
                                    else
                                    {
                                        imageContentFile = hashedImages[normalizedHashUrl];
                                    }

                                    return "url(" + imageContentFile + ")";
                                });

                        return cssContent;
                    });
        }

        /// <summary>Gets the unique object for all the settings, used for determining unique cache id.</summary>
        /// <returns>The settings object.</returns>
        private object GetVarBySettings()
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
                HashCss = this.CssHasher == null,
                HashImages = this.ImageHasher == null,
                this.ShouldHashImages
            };
        }

        /// <summary>
        /// Calls each configured visitor and returns printed results when finished.
        /// </summary>
        /// <param name="stylesheetNode">The node for vistors to visit.</param>
        /// <returns>Processed node either minified or pretty printed.</returns>
        private string ApplyConfiguredVisitors(AstNode stylesheetNode)
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

            // The image assembly is a 3 step process:
            // 1. Scan the Css followed by Pretty Print
            // 2. Run the image assembly tool
            // 3. Update the Css with generated images followed by Pretty Print
            if (this.ShouldAssembleBackgroundImages)
            {
                stylesheetNode = this.SpriteBackgroundImages(stylesheetNode);
            }

            return this.context.SectionedAction(SectionIdParts.MinifyCssActivity, SectionIdParts.PrintCss).Execute(() => 
                this.ShouldMinify ? stylesheetNode.MinifyPrint() : stylesheetNode.PrettyPrint());
        }

        /// <summary>
        /// Assembles the background images
        /// </summary>
        /// <param name="stylesheetNode">the style sheet node</param>
        /// <returns>The stylesheet node with the sprited images aplied.</returns>
        private AstNode SpriteBackgroundImages(AstNode stylesheetNode)
        {
            return this.context.SectionedAction(SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting).Execute(() =>
            {
                // Step # 6 - Execute the pipeline for image assembly scan
                stylesheetNode = this.ExecuteImageAssemblyScan(stylesheetNode);

                // Step # 7 - Execute the pipeline for image assembly tool
                var spriteLogFiles = new List<string>();
                for (var count = 0; count < this.imageAssemblyScanVisitor.ImageAssemblyScanOutputs.Count; count++)
                {
                    var scanOutput = this.imageAssemblyScanVisitor.ImageAssemblyScanOutputs[count];
                    var spriteLogFile = this.GetSpriteLogFileName(count, scanOutput);

                    if (File.Exists(spriteLogFile))
                    {
                        this.SpriteImageFromLog(scanOutput, spriteLogFile);
                        spriteLogFiles.Add(spriteLogFile);
                    }
                }

                if (spriteLogFiles.Any())
                {
                    // Step # 8 - Execute the pipeline for image assembly update
                    stylesheetNode = this.ExecuteImageAssemblyUpdate(stylesheetNode, spriteLogFiles);
                }

                return stylesheetNode;
            });
        }

        /// <summary>The sprite image.</summary>
        /// <param name="scanOutput">The scan output.</param>
        /// <param name="spriteLogFile">The sprite log file.</param>
        private void SpriteImageFromLog(ImageAssemblyScanOutput scanOutput, string spriteLogFile)
        {
            var imageReferencesToAssemble = scanOutput.ImageReferencesToAssemble;
            this.context.SectionedAction(SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting, SectionIdParts.Assembly)
                .CanBeCached(this.GetRelativeSpriteCacheKey(imageReferencesToAssemble))
                .RestoreFromCacheAction(cacheSection =>
                {
                    // restore log file, is required by next step in applying sprites to the css.
                    var destinationDirectory = this.context.Configuration.DestinationDirectory;
                    var spriteLogFileContentItem = cacheSection.GetCachedContentItem(CacheFileCategories.SpriteLogFile);
                    spriteLogFileContentItem.WriteTo(spriteLogFile);

                    // Restore images
                    var spritedImageContentItems = cacheSection.GetCachedContentItems(CacheFileCategories.HashedSpriteImage);
                    spritedImageContentItems.ForEach(sici => sici.WriteToContentPath(destinationDirectory));

                    return true;
                })
                .Execute(cacheSection =>
                {
                    if (imageReferencesToAssemble == null || imageReferencesToAssemble.Count == 0)
                    {
                        return false;
                    }

                    if (!Directory.Exists(this.ImagesOutputDirectory))
                    {
                        Directory.CreateDirectory(this.ImagesOutputDirectory);
                    }

                    ImageAssembleGenerator.AssembleImages(
                        imageReferencesToAssemble.ToSafeReadOnlyCollection(),
                        SpritePackingType.Vertical,
                        this.ImagesOutputDirectory,
                        spriteLogFile,
                        string.Empty,
                        true,
                        this.context);

                    string destinationDirectory = this.context.Configuration.DestinationDirectory;
                    cacheSection.AddResult(
                        ContentItem.FromContent(spriteLogFile, spriteLogFile.MakeRelativeToDirectory(destinationDirectory)),
                        CacheFileCategories.SpriteLogFile);

                    var spriteLogDocument = XDocument.Load(spriteLogFile);
                    foreach (var spritedFile in spriteLogDocument.Elements("images").Elements("output").Select(e => (string)e.Attribute("file")))
                    {
                        cacheSection.AddResult(
                            ContentItem.FromFile(spritedFile, spritedFile.MakeRelativeToDirectory(destinationDirectory)),
                            CacheFileCategories.HashedSpriteImage);
                    }

                    return true;
                });
        }

        /// <summary>Gets the sprite log file from the scan output.</summary>
        /// <param name="count">The count.</param>
        /// <param name="scanOutput">The scan output.</param>
        /// <returns>The <see cref="string"/>.</returns>
        private string GetSpriteLogFileName(int count, ImageAssemblyScanOutput scanOutput)
        {
            string spriteLogFile;
            if (count == 0)
            {
                // Default Bucket
                spriteLogFile = this.ImageAssembleScanDestinationFile + Strings.ScanLogExtension;
            }
            else
            {
                // Extra Buckets say "Lazy"
                spriteLogFile = this.ImageAssembleScanDestinationFile + scanOutput.ImageAssemblyScanInput.BucketName + Strings.ScanLogExtension;
            }

            return spriteLogFile;
        }

        /// <summary>The get relative sprite cache key.</summary>
        /// <param name="imageReferencesToAssemble">The image references to assemble</param>
        /// <returns>The unique cache key.</returns>
        private string GetRelativeSpriteCacheKey(IList<InputImage> imageReferencesToAssemble)
        {
            return string.Join(
                ">",
                imageReferencesToAssemble.Select(ir =>
                        "{0}|{1}|{2}".InvariantFormat(
                            this.context.MakeRelative(ir.ImagePath),
                            ir.Position,
                            string.Join(":", ir.DuplicateImagePaths.Select(dip => this.context.MakeRelative(dip))))));
        }

        /// <summary>Scans the css for the image path references</summary>
        /// <param name="stylesheetNode">The stylesheet node</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        private AstNode ExecuteImageAssemblyScan(AstNode stylesheetNode)
        {
            this.imageAssemblyScanVisitor = new ImageAssemblyScanVisitor(
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

            stylesheetNode = stylesheetNode.Accept(this.imageAssemblyScanVisitor);

            // Save the Scan Css (Single file per css)
            this.imageAssemblyScanVisitor.ImageAssemblyAnalysisLog.Save(this.ImageAssembleScanDestinationFile + Strings.ScanLogExtension);

            return stylesheetNode;
        }

        /// <summary>Executes the image assembly update visitor</summary>
        /// <param name="stylesheetNode">The stylesheet Ast node</param>
        /// <param name="spriteLogFiles">The sprite Log Files.</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        private AstNode ExecuteImageAssemblyUpdate(AstNode stylesheetNode, IEnumerable<string> spriteLogFiles)
        {
            var specificStyleSheet = stylesheetNode as StyleSheetNode;

            foreach (var spriteLogFile in spriteLogFiles)
            {
                if (!File.Exists(spriteLogFile))
                {
                    throw new FileNotFoundException(spriteLogFile);
                }
            }

            var shouldHashImages = this.ShouldHashImages && this.ImageHasher != null;
            this.imageAssemblyUpdateVisitor = new ImageAssemblyUpdateVisitor(
                this.SourceFile,
                spriteLogFiles,
                specificStyleSheet == null ? 1d : specificStyleSheet.Dpi.GetValueOrDefault(1d),
                this.OutputUnit,
                this.OutputUnitFactor,
                shouldHashImages ? this.ImageHasher.BasePrefixToRemoveFromOutputPathInLog : null,
                shouldHashImages ? this.ImageHasher.BasePrefixToAddToOutputPath : null,
                this.availableSourceImages);

            stylesheetNode = stylesheetNode.Accept(this.imageAssemblyUpdateVisitor);

            // Return the updated Ast
            return stylesheetNode;
        }
    }
}
