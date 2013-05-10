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
    using System.IO;
    using System.Linq;
    using System.Text;
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
        private static readonly Regex UrlHashAllRegexPattern = new Regex(@"url\s*\(\s*(?<quote>[""']?)hash://(?<url>.*?)\k<quote>\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>The url hash regex pattern.</summary>
        private static readonly Regex UrlHashRegexPattern = new Regex(@"(?<type>hash)(?:\((?<url>[^)]*))\)|(?<type>url)\((?<quote>[""']?)\s*([-\\:/.\w]+\.[\w]+)\s*\k<quote>\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

            this.context.Measure.Start(SectionIdParts.MinifyCssActivity);
            string css;

            try
            {
                this.context.Measure.Start(SectionIdParts.MinifyCssActivity, SectionIdParts.Parse);
                AstNode stylesheetNode = CssParser.Parse(cssContent, shouldLog);
                css = this.ApplyConfiguredVisitors(stylesheetNode);
                this.context.Measure.End(SectionIdParts.MinifyCssActivity, SectionIdParts.Parse);
            }
            catch (Exception exception)
            {
                // give back the original unmodified
                css = cssContent;
                this.ParserException = exception;
            }
            finally
            {
                this.context.Measure.End(SectionIdParts.MinifyCssActivity);
            }

            return css;
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
            this.context.Measure.Start(SectionIdParts.MinifyCssActivity);
            var cacheSection = this.context.Cache.BeginSection(
                "minifycss",
                contentItem,
                this.GetVarBySettings());

            try
            {
                if (this.TryRestoreFromCache(cacheSection, shouldHashCss, shouldHashImages))
                {
                    return;
                }

                // Get the content from file.
                var cssContent = contentItem.Content;

                if (shouldHashImages)
                {
                    // Pre hash images, replace all hash( with hash:// to make it valid css.
                    cssContent = this.PreHashImages(cssContent, cacheSection);
                }

                // Load the Css parser and stylesheet Ast
                AstNode stylesheetNode;
                this.context.Measure.Start(SectionIdParts.MinifyCssActivity, SectionIdParts.Parse);
                try
                {
                    stylesheetNode = CssParser.Parse(cssContent, false);
                }
                finally
                {
                    this.context.Measure.End(SectionIdParts.MinifyCssActivity, SectionIdParts.Parse);
                }

                // Apply all configured visitors, including, validating, optimizing, minifying and spriting.
                var css = this.ApplyConfiguredVisitors(stylesheetNode);

                if (shouldHashImages)
                {
                    // Hash all images that have not been sprited.
                    css = this.HashImages(css, cacheSection);
                }

                var destinationFile = this.DestinationFile;
                var relativeDestinationFile =
                        Path.IsPathRooted(destinationFile)
                        ? destinationFile.MakeRelativeToDirectory(this.context.Configuration.DestinationDirectory)
                        : destinationFile;

                if (shouldHashCss)
                {
                    // Write the result to disk using hashing.
                    var result = this.CssHasher.Hash(ContentItem.FromContent(css, relativeDestinationFile));
                    cacheSection.AddResult(result, CacheFileCategories.MinifiedCssResult, true);
                }
                else
                {
                    // Write to the destination file on disk.
                    FileHelper.WriteFile(this.DestinationFile, css);
                    cacheSection.AddResult(ContentItem.FromContent(css, relativeDestinationFile), CacheFileCategories.MinifiedCssResult, true);
                }

                cacheSection.Save();
            }
            catch (Exception exception)
            {
                throw new WorkflowException(
                    "MinifyCssActivity - Error happened while executing the css pipeline activity.", exception);
            }
            finally
            {
                cacheSection.EndSection();
                this.context.Measure.End(SectionIdParts.MinifyCssActivity);
            }
        }

        /// <summary>Tries to restore the sprited image from cache.</summary>
        /// <param name="cacheSection">The cache section.</param>
        /// <param name="spriteLogFile">The sprite log file.</param>
        /// <param name="destinationDirectory">The destination directory.</param>
        /// <returns>If it was able to restore.</returns>
        private static bool TryRestoreSpritedImageFromCache(ICacheSection cacheSection, string spriteLogFile, string destinationDirectory)
        {
            if (cacheSection.CanBeRestoredFromCache())
            {
                var spriteLogFileContentItem = cacheSection.GetCachedContentItem(CacheFileCategories.SpriteLogFile);
                spriteLogFileContentItem.WriteTo(spriteLogFile);

                RestoreSpritedImages(cacheSection, destinationDirectory);
                return true;
            }

            return false;
        }

        /// <summary>Restores sprited images.</summary>
        /// <param name="cacheSection">The cache section.</param>
        /// <param name="destinationDirectory">The destination directory.</param>
        private static void RestoreSpritedImages(ICacheSection cacheSection, string destinationDirectory)
        {
            var spritedImageContentItems = cacheSection.GetCachedContentItems(CacheFileCategories.HashedSpriteImage);
            spritedImageContentItems.ForEach(sici => sici.WriteToContentPath(destinationDirectory));
        }

        /// <summary>Try and restore from cache.</summary>
        /// <param name="cacheSection">The cache section.</param>
        /// <param name="shouldHashCss">The should Hash Css.</param>
        /// <param name="shouldHashImages">The should Hash Images.</param>
        /// <returns>If it can restore.</returns>
        private bool TryRestoreFromCache(ICacheSection cacheSection, bool shouldHashCss, bool shouldHashImages)
        {
            if (cacheSection.CanBeRestoredFromCache())
            {
                var cssContentItem = cacheSection.GetCachedContentItem(CacheFileCategories.MinifiedCssResult);
                if (shouldHashCss)
                {
                    // Restore hashed css output
                    this.CssHasher.AppendToWorkLog(cssContentItem);
                    cssContentItem.WriteToHashedPath(this.context.Configuration.DestinationDirectory);
                }
                else
                {
                    // Restore css output
                    cssContentItem.WriteToContentPath(this.context.Configuration.DestinationDirectory, true);
                }

                if (this.ShouldAssembleBackgroundImages)
                {
                    // Restore sprited images
                    RestoreSpritedImages(cacheSection, this.context.Configuration.DestinationDirectory);
                }

                if (shouldHashImages)
                {
                    // Restore sprited images
                    var hashedImageContentItems = cacheSection.GetCachedContentItems(CacheFileCategories.HashedImage);
                    hashedImageContentItems.ForEach(sici => sici.WriteToHashedPath(this.context.Configuration.DestinationDirectory));
                    this.ImageHasher.AppendToWorkLog(hashedImageContentItems);
                }

                return true;
            }

            return false;
        }

        /// <summary>The pre hash images.</summary>
        /// <param name="cssContent">The css content.</param>
        /// <param name="cacheSection">The cache section.</param>
        /// <returns>The <see cref="string"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Css image requires lowercase.")]
        private string PreHashImages(string cssContent, ICacheSection cacheSection)
        {
            this.context.Measure.Start(SectionIdParts.MinifyCssActivity, SectionIdParts.ImageHash);
            try
            {
                cssContent = UrlHashRegexPattern.Replace(
                    cssContent,
                    match =>
                    {
                        var url = match.Groups["url"].Value.ToLowerInvariant();
                        cacheSection.AddSourceDependency(Path.Combine(this.context.Configuration.SourceDirectory, url.NormalizeUrl()));

                        if (string.IsNullOrWhiteSpace(match.Groups["url"].Value))
                        {
                            return match.Value;
                        }

                        var result = "hash://" + url;
                        if (match.Groups["type"].Value == "url")
                        {
                            result = "url('" + result + "')";
                        }

                        return result;
                    });
                return cssContent;
            }
            finally
            {
                this.context.Measure.End(SectionIdParts.MinifyCssActivity, SectionIdParts.ImageHash);
            }
        }

        /// <summary>Hash the images.</summary>
        /// <param name="cssContent">The css content.</param>
        /// <param name="cacheSection">The cache section.</param>
        /// <returns>The css with hashed images.</returns>
        private string HashImages(string cssContent, ICacheSection cacheSection)
        {
            this.context.Measure.Start(SectionIdParts.MinifyCssActivity, SectionIdParts.ImageHash);
            try
            {
                var urlsToHash = UrlHashAllRegexPattern.Matches(cssContent)
                    .OfType<Match>()
                    .Select(m => m.Groups["url"].Value)
                    .Distinct();

                var contentImagesToHash = new List<ContentItem>();
                foreach (string hashUrl in urlsToHash)
                {
                    var normalizedHashUrl = hashUrl.NormalizeUrl();
                    var imageContentFile = this.availableSourceImages.ContainsKey(normalizedHashUrl)
                        ? this.availableSourceImages[normalizedHashUrl]
                        : null;

                    if (imageContentFile == null)
                    {
                        throw new BuildWorkflowException("Could not find a macthing source image for url: {0}".InvariantFormat(hashUrl));
                    }

                    contentImagesToHash.Add(ContentItem.FromFile(imageContentFile, normalizedHashUrl));
                }

                var hashedImages = this.ImageHasher.Hash(contentImagesToHash);
                cssContent = UrlHashAllRegexPattern
                    .Replace(
                    cssContent,
                    match =>
                    {
                        var normalizedHashUrl = match.Groups["url"].Value.NormalizeUrl();
                        var hashedUrl = hashedImages.Where(hi => hi.RelativeContentPath.Equals(normalizedHashUrl, StringComparison.OrdinalIgnoreCase)).Select(rf => rf.RelativeHashedContentPath).FirstOrDefault();
                        if (string.IsNullOrWhiteSpace(hashedUrl))
                        {
                            throw new BuildWorkflowException("Could not locate the hashed image for css: {0} and url {1}".InvariantFormat(match.Value, match.Groups["url"].Value));
                        }

                        return "url(" + Path.Combine(ImageHasher.BasePrefixToAddToOutputPath, hashedUrl.Replace('\\', '/')) + ")";
                    });

                // Add the image as end result
                hashedImages.ForEach(hf => cacheSection.AddResult(hf, CacheFileCategories.HashedImage, true));

                return cssContent;
            }
            finally
            {
                this.context.Measure.End(SectionIdParts.MinifyCssActivity, SectionIdParts.ImageHash);
            }
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
            this.context.Measure.Start(SectionIdParts.MinifyCssActivity, SectionIdParts.Validate);
            try
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
            }
            finally
            {
                this.context.Measure.End(SectionIdParts.MinifyCssActivity, SectionIdParts.Validate);
            }

            // Step # 5 - Run the Css optimization visitors
            if (this.ShouldOptimize)
            {
                this.context.Measure.Start(SectionIdParts.MinifyCssActivity, SectionIdParts.Optimize);
                try
                {
                    stylesheetNode = stylesheetNode.Accept(new OptimizationVisitor());
                    stylesheetNode = stylesheetNode.Accept(new ColorOptimizationVisitor());
                    stylesheetNode = stylesheetNode.Accept(new FloatOptimizationVisitor());
                }
                finally
                {
                    this.context.Measure.End(SectionIdParts.MinifyCssActivity, SectionIdParts.Optimize);
                }
            }

            // The image assembly is a 3 step process:
            // 1. Scan the Css followed by Pretty Print
            // 2. Run the image assembly tool
            // 3. Update the Css with generated images followed by Pretty Print
            if (this.ShouldAssembleBackgroundImages)
            {
                stylesheetNode = this.SpriteBackgroundImages(stylesheetNode);
            }

            try
            {
                this.context.Measure.Start(SectionIdParts.MinifyCssActivity, SectionIdParts.PrintCss);
                return this.ShouldMinify ? stylesheetNode.MinifyPrint() : stylesheetNode.PrettyPrint();
            }
            finally
            {
                this.context.Measure.End(SectionIdParts.MinifyCssActivity, SectionIdParts.PrintCss);
            }
        }

        /// <summary>
        /// Assembles the background images
        /// </summary>
        /// <param name="stylesheetNode">the style sheet node</param>
        /// <returns>The stylesheet node with the sprited images aplied.</returns>
        private AstNode SpriteBackgroundImages(AstNode stylesheetNode)
        {
            this.context.Measure.Start(SectionIdParts.MinifyCssActivity, SectionIdParts.Sprite);
            try
            {
                // Step # 6 - Execute the pipeline for image assembly scan
                stylesheetNode = this.ExecuteImageAssemblyScan(stylesheetNode);

                // Step # 7 - Execute the pipeline for image assembly tool
                var spriteLogFiles = new List<string>();
                for (var count = 0; count < this.imageAssemblyScanVisitor.ImageAssemblyScanOutputs.Count; count++)
                {
                    var scanOutput = this.imageAssemblyScanVisitor.ImageAssemblyScanOutputs[count];

                    var spriteLogFile = this.GetSpriteLogFile(count, scanOutput);

                    if (File.Exists(spriteLogFile))
                    {
                        spriteLogFiles.Add(spriteLogFile);

                        var cacheSection = this.context.Cache.BeginSection(
                            "minifycss.sprite",
                            new { relativeSpriteCacheKey = this.GetRelativeSpriteCacheKey(scanOutput.ImageReferencesToAssemble) });
                        try
                        {
                            var destinationDirectory = this.context.Configuration.DestinationDirectory;

                            if (TryRestoreSpritedImageFromCache(cacheSection, spriteLogFile, destinationDirectory))
                            {
                                continue;
                            }

                            this.context.Measure.Start(SectionIdParts.MinifyCssActivity, SectionIdParts.Sprite, SectionIdParts.Assembly);
                            try
                            {
                                this.ExecuteImageAssembly(scanOutput.ImageReferencesToAssemble, spriteLogFile);
                            }
                            finally
                            {
                                this.context.Measure.End(SectionIdParts.MinifyCssActivity, SectionIdParts.Sprite, SectionIdParts.Assembly);
                            }

                            if (File.Exists(spriteLogFile))
                            {
                                cacheSection.AddResult(ContentItem.FromContent(spriteLogFile, spriteLogFile.MakeRelativeToDirectory(destinationDirectory)), CacheFileCategories.SpriteLogFile);
                                foreach (var spritedFile in XDocument.Load(spriteLogFile).Elements("images").Elements("output").Select(e => (string)e.Attribute("file")))
                                {
                                    cacheSection.AddResult(ContentItem.FromFile(spritedFile, spritedFile.MakeRelativeToDirectory(destinationDirectory)), CacheFileCategories.HashedSpriteImage);
                                }
                            }

                            cacheSection.Save();
                        }
                        finally
                        {
                            cacheSection.EndSection();
                        }
                    }
                }

                if (spriteLogFiles.Any())
                {
                    // Step # 8 - Execute the pipeline for image assembly update
                    stylesheetNode = this.ExecuteImageAssemblyUpdate(stylesheetNode, spriteLogFiles);
                }

                return stylesheetNode;
            }
            finally
            {
                this.context.Measure.End(SectionIdParts.MinifyCssActivity, SectionIdParts.Sprite);
            }
        }

        /// <summary>Gets the sprite log file from the scan output.</summary>
        /// <param name="count">The count.</param>
        /// <param name="scanOutput">The scan output.</param>
        /// <returns>The <see cref="string"/>.</returns>
        private string GetSpriteLogFile(int count, ImageAssemblyScanOutput scanOutput)
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
                availableSourceImages);

            stylesheetNode = stylesheetNode.Accept(this.imageAssemblyUpdateVisitor);

            // Return the updated Ast
            return stylesheetNode;
        }

        /// <summary>Execute the image assembly tool on the image references found after scan</summary>
        /// <param name="imageReferences">The image references</param>
        /// <param name="destinationLogFile">The name of the log file that ImageAssembleTool should generate</param>
        private void ExecuteImageAssembly(ICollection<InputImage> imageReferences, string destinationLogFile)
        {
            if (imageReferences == null || imageReferences.Count == 0)
            {
                return;
            }

            Directory.CreateDirectory(this.ImagesOutputDirectory);
            ImageAssembleGenerator.AssembleImages(imageReferences.ToSafeReadOnlyCollection(), SpritePackingType.Vertical, this.ImagesOutputDirectory, destinationLogFile, string.Empty, true, this.context);
        }
    }
}
