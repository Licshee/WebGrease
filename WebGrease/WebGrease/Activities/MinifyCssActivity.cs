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
        private static readonly Regex UrlHashAllRegexPattern = new Regex(@"url\s*\(\s*(?<quote>[""']?)hash://(?<url>.*?)\k<quote>\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex UrlHashRegexPattern = new Regex(@"(?<type>hash)(?:\((?<url>[^)]*))\)|(?<type>url)\((?<quote>[""']?)\s*([-\\:/.\w]+\.[\w]+)\s*\k<quote>\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>The context.</summary>
        private readonly IWebGreaseContext context;

        /// <summary>The image assembly scan visitor.</summary>
        private ImageAssemblyScanVisitor imageAssemblyScanVisitor;

        /// <summary>The image assembly update visitor.</summary>
        private ImageAssemblyUpdateVisitor imageAssemblyUpdateVisitor;

        private IEnumerable<ResultFile> availableSourceImages;

        /// <summary>Initializes a new instance of the <see cref="MinifyCssActivity"/> class.</summary>
        /// <param name="context"></param>
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

        /// <summary>Gets or sets Source File.</summary>
        internal string SourceFile { get; set; }

        /// <summary>Gets or sets Destination File.</summary>
        internal string DestinationFile { get; set; }

        /// <summary>Gets or sets a value indicating whether css should exclude properties marked with "Exclude".</summary>
        internal bool ShouldExcludeProperties { get; set; }

        /// <summary>Gets or sets a value indicating whether css should be validated for lower case.</summary>
        internal bool ShouldValidateForLowerCase { get; set; }

        /// <summary>Gets or sets a value indicating whether css should be optimized for colors, float, duplicate selectors etc.</summary>
        internal bool ShouldOptimize { get; set; }

        /// <summary>Gets or sets whether to assemble CSS background Images and update the coordinates.</summary>
        internal bool ShouldAssembleBackgroundImages { get; set; }

        /// <summary>Gets or sets a value indicating whether ShouldMinify.</summary>
        internal bool ShouldMinify { get; set; }

        /// <summary>Gets or sets a value indicating whether it should hash the images.</summary>
        internal bool ShouldHashImages { get; set; }

        /// <summary>Gets or sets the image hasher.</summary>
        public FileHasherActivity ImageHasher { get; set; }

        /// <summary>Gets or sets the css hasher.</summary>
        public FileHasherActivity CssHasher { get; set; }

        /// <summary>Gets HackSelectors.</summary>
        internal HashSet<string> HackSelectors { get; set; }

        /// <summary>Gets BannedSelectors.</summary>
        internal HashSet<string> BannedSelectors { get; set; }

        /// <summary>Gets OutputUnit (Default: px, other possible values: rem/em etc..).</summary>
        public string OutputUnit { get; set; }

        /// <summary>Gets OutputUnitFactor (Default: 1, example value for 10px based REM: 0.625</summary>
        public double OutputUnitFactor { get; set; }

        /// <summary>Gets or sets a value indicating whether to ignore images that have a background-size property set to non-default ('auto' or 'auto auto').</summary>
        public bool IgnoreImagesWithNonDefaultBackgroundSize { get; set; }

        /// <summary>Gets or sets Image Assembly Scan Output.</summary>
        /// <remarks>Optional - Needed only when image spriting is needed.</remarks>
        internal string ImageAssembleScanDestinationFile { get; set; }

        /// <summary>Gets or sets the image output directory.</summary>
        internal string ImagesOutputDirectory { get; set; }

        /// <summary>Gets ImageAssembleReferencesToIgnore.</summary>
        internal HashSet<string> ImageAssembleReferencesToIgnore { get; set; }

        /// <summary>Gets or sets the image assembly padding.</summary>
        internal string ImageAssemblyPadding { get; set; }

        /// <summary>Gets or sets Image Assembly additional buckets.</summary>
        /// <remarks>Optional - Needed only when image spriting is needed.</remarks>
        internal IList<ImageAssemblyScanInput> AdditionalImageAssemblyBuckets { get; set; }

        /// <summary>Gets or sets the exception, if any, returned from a parsing attempt.</summary>
        internal Exception ParserException { get; set; }

        /// <summary>Gets or sets the output path.</summary>
        public string OutputPath { get; set; }

        public IList<string> ImageDirectories { get; set; }

        public IList<string> ImageExtensions { get; set; }

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

            this.context.Measure.Start(TimeMeasureNames.MinifyCssActivity);
            string css;

            try
            {
                this.context.Measure.Start(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Parse);
                AstNode stylesheetNode = CssParser.Parse(cssContent, shouldLog);
                css = ApplyConfiguredVisitors(stylesheetNode);
                this.context.Measure.End(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Parse);
            }
            catch (Exception exception)
            {
                // give back the original unmodified
                css = cssContent;
                this.ParserException = exception;
            }
            finally
            {
                this.context.Measure.End(TimeMeasureNames.MinifyCssActivity);
            }

            return css;
        }

        /// <summary>Executes the task in a build style workflow, i.e. with a file path in and destination file set for out.</summary>
        internal void Execute()
        {
            var shouldHashImages = this.ShouldHashImages && this.ImageHasher != null;
            if (shouldHashImages)
            {
                this.availableSourceImages = this.context.GetAvailableFiles(this.context.Configuration.SourceDirectory, this.ImageDirectories, this.ImageExtensions, FileTypes.Image);
            }

            if (string.IsNullOrWhiteSpace(this.SourceFile))
            {
                throw new ArgumentException("MinifyCssActivity - The source file cannot be null or whitespace.");
            }

            if (!File.Exists(this.SourceFile))
            {
                throw new FileNotFoundException("MinifyCssActivity - The source file cannot be found.", this.SourceFile);
            }

            if (string.IsNullOrWhiteSpace(this.DestinationFile))
            {
                throw new ArgumentException("MinifyCssActivity - The destination file cannot be null or whitespace.");
            }

            this.context.Measure.Start(TimeMeasureNames.MinifyCssActivity);
            var cacheSection = this.context.Cache.BeginSection(
                "minifycss",
                new FileInfo(this.SourceFile),
                this.GetVarBySettings());

            try
            {
                if (this.TryRestoreFromCache(cacheSection))
                {
                    return;
                }

                // Get the content from file.
                var cssContent = File.ReadAllText(this.SourceFile);

                if (shouldHashImages)
                {
                    // Pre hash images, replace all hash( with hash:// to make it valid css.
                    cssContent = this.PreHashImages(cssContent, cacheSection);
                }

                // Load the Css parser and stylesheet Ast
                AstNode stylesheetNode;
                this.context.Measure.Start(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Parse);
                try
                {
                    stylesheetNode = CssParser.Parse(cssContent, false);
                }
                finally
                {
                    this.context.Measure.End(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Parse);
                }

                // Apply all configured visitors, including, validating, optimizing, minifying and spriting.
                var css = this.ApplyConfiguredVisitors(stylesheetNode);

                if (shouldHashImages)
                {
                    // Hash all images that have not been sprited.
                    css = this.HashImages(css, cacheSection);
                }

                if (this.CssHasher != null)
                {
                    // Write the result to disk using hashing.
                    var result = this.CssHasher.Hash(ResultFile.FromContent(css, FileTypes.StyleSheet, this.DestinationFile, this.OutputPath));
                    cacheSection.AddEndResultFile(result, CacheKeys.MinifiedCssResultCacheKey);
                }
                else
                {
                    // Write to the destination file on disk.
                    FileHelper.WriteFile(this.DestinationFile, css, Encoding.UTF8);
                    cacheSection.AddResultFile(this.DestinationFile, CacheKeys.MinifiedCssResultCacheKey, this.OutputPath);
                }

                cacheSection.Store();
            }
            catch (Exception exception)
            {
                throw new WorkflowException(
                    "MinifyCssActivity - Error happened while executing the css pipeline activity.", exception);
            }
            finally
            {
                cacheSection.EndSection();
                this.context.Measure.End(TimeMeasureNames.MinifyCssActivity);
            }
        }

        private bool TryRestoreFromCache(ICacheSection cacheSection)
        {
            if (cacheSection.CanBeRestoredFromCache())
            {
                if (this.CssHasher != null)
                {
                    // Restore hashed css output
                    var resultFile = cacheSection.RestoreFiles(CacheKeys.MinifiedCssResultCacheKey, this.context.Configuration.DestinationDirectory).First();
                    this.CssHasher.AppendToWorkLog(resultFile, this.DestinationFile);
                }
                else
                {
                    // Restore css output
                    cacheSection.RestoreFile(CacheKeys.MinifiedCssResultCacheKey, this.DestinationFile);
                }

                if (this.ShouldAssembleBackgroundImages)
                {
                    // Restore sprited images
                    cacheSection.RestoreFiles(CacheKeys.HashedSpriteImageCacheKey, this.context.Configuration.DestinationDirectory, false);
                }

                if (this.ShouldHashImages && this.ImageHasher != null)
                {
                    // Restore non sprited hashed images.
                    var resultFiles = cacheSection.RestoreFiles(CacheKeys.HashedImageCacheKey, this.context.Configuration.DestinationDirectory, false);
                    this.ImageHasher.AppendToWorkLog(resultFiles);
                }

                return true;
            }
            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Css image requires lowercase.")]
        private string PreHashImages(string cssContent, ICacheSection cacheSection)
        {
            this.context.Measure.Start(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.ImageHash);
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
                this.context.Measure.End(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.ImageHash);
            }
        }

        private string HashImages(string cssContent, ICacheSection cacheSection)
        {
            this.context.Measure.Start(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.ImageHash);
            try
            {
                var sourceDirectory = this.context.Configuration.SourceDirectory;

                var hashUrlsInCss = UrlHashAllRegexPattern.Matches(cssContent)
                    .OfType<Match>()
                    .Select(m => m.Groups["url"].Value)
                    .Distinct();

                var imagesToHash = new List<ResultFile>();
                foreach (string hashUrl in hashUrlsInCss)
                {
                    var normalizedHashUrl = hashUrl.NormalizeUrl();
                    var sources = this.availableSourceImages
                        .Where(rf => rf.Path == normalizedHashUrl)
                        .Select(rf => rf.OriginalPath)
                        .ToArray();

                    if (sources.Count() > 1)
                    {
                        throw new BuildWorkflowException("More then one possible source image found for url: {0}".InvariantFormat(hashUrl));
                    }

                    if (!sources.Any())
                    {
                        throw new BuildWorkflowException("Could not find a macthing source image for url: {0}".InvariantFormat(hashUrl));
                    }

                    var source = sources.FirstOrDefault();
                    imagesToHash.Add(ResultFile.FromFile(source, FileTypes.Image, source, this.OutputPath));
                }

                var hashedImages = this.ImageHasher.Hash(imagesToHash);
                cssContent = UrlHashAllRegexPattern
                    .Replace(
                    cssContent, 
                    match =>
                    {
                        var normalizedHashUrl = match.Groups["url"].Value.NormalizeUrl();
                        var hashedUrl = hashedImages.Where(hi => hi.OriginalPath.MakeRelativeToDirectory(sourceDirectory).Equals(normalizedHashUrl, StringComparison.OrdinalIgnoreCase)).Select(rf => rf.Path).FirstOrDefault();
                        if (string.IsNullOrWhiteSpace(hashedUrl))
                        {
                            throw new BuildWorkflowException("Could not locate the hashed image for css: {0} and url {1}".InvariantFormat(match.Value, match.Groups["url"].Value));
                        }

                        return "url(" + Path.Combine(ImageHasher.BasePrefixToAddToOutputPath, hashedUrl.MakeRelativeToDirectory(ImageHasher.BasePrefixToRemoveFromOutputPathInLog).Replace('\\', '/')) + ")";
                    });

                // Add the image as end result
                hashedImages.ForEach(hf => cacheSection.AddEndResultFile(hf, CacheKeys.HashedImageCacheKey));

                return cssContent;
            }
            finally
            {
                this.context.Measure.End(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.ImageHash);
            }
        }

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
                this.AdditionalImageAssemblyBuckets,
                this.OutputUnit,
                this.OutputUnitFactor,
                this.ImageAssemblyPadding
            };
        }

        /// <summary>
        /// Calls each configured visitor and returns printed results when finished.
        /// </summary>
        /// <param name="stylesheetNode">The node for vistors to visit.</param>
        /// <returns>Processed node either minified or pretty printed.</returns>
        private string ApplyConfiguredVisitors(AstNode stylesheetNode)
        {
            this.context.Measure.Start(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Validate);
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
                this.context.Measure.End(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Validate);
            }

            // Step # 5 - Run the Css optimization visitors
            if (this.ShouldOptimize)
            {
                this.context.Measure.Start(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Optimize);
                try
                {
                    stylesheetNode = stylesheetNode.Accept(new OptimizationVisitor());
                    stylesheetNode = stylesheetNode.Accept(new ColorOptimizationVisitor());
                    stylesheetNode = stylesheetNode.Accept(new FloatOptimizationVisitor());
                }
                finally
                {
                    this.context.Measure.End(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Optimize);
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
                this.context.Measure.Start(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.PrintCss);
                return this.ShouldMinify ? stylesheetNode.MinifyPrint() : stylesheetNode.PrettyPrint();
            }
            finally
            {
                this.context.Measure.End(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.PrintCss);

            }
        }

        /// <summary>
        /// Assembles the background images
        /// </summary>
        /// <param name="stylesheetNode">the style sheet node</param>
        /// <returns>The stylesheet node with the sprited images aplied.</returns>
        private AstNode SpriteBackgroundImages(AstNode stylesheetNode)
        {
            this.context.Measure.Start(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Sprite);
            try
            {
                // Step # 6 - Execute the pipeline for image assembly scan
                stylesheetNode = this.ExecuteImageAssemblyScan(stylesheetNode);

                // Step # 7 - Execute the pipeline for image assembly tool
                var spriteLogFiles = new List<string>();
                for (var count = 0; count < this.imageAssemblyScanVisitor.ImageAssemblyScanOutputs.Count; count++)
                {
                    string spriteLogFile;
                    var scanOutput = this.imageAssemblyScanVisitor.ImageAssemblyScanOutputs[count];
                    if (count == 0)
                    {
                        // Default Bucket
                        spriteLogFile = this.ImageAssembleScanDestinationFile + Strings.ScanLogExtension;
                    }
                    else
                    {
                        // Extra Buckets say "Lazy"
                        spriteLogFile = this.ImageAssembleScanDestinationFile + scanOutput.ImageAssemblyScanInput.BucketName
                                        + Strings.ScanLogExtension;
                    }

                    if (File.Exists(spriteLogFile))
                    {
                        spriteLogFiles.Add(spriteLogFile);

                        var cacheSection = this.context.Cache.BeginSection(
                            "minifycss.sprite",
                            new { relativeSpriteCacheKey = this.GetRelativeSpriteCacheKey(scanOutput) });
                        try
                        {
                            if (cacheSection.CanBeRestoredFromCache())
                            {
                                cacheSection.RestoreFile(CacheKeys.SpriteLogFileCacheKey, spriteLogFile);
                                cacheSection.RestoreFiles(CacheKeys.HashedSpriteImageCacheKey, this.context.Configuration.DestinationDirectory, false);
                                continue;
                            }

                            this.context.Measure.Start(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Sprite, TimeMeasureNames.Assembly);
                            try
                            {
                                this.ExecuteImageAssembly(scanOutput.ImageReferencesToAssemble, spriteLogFile);
                            }
                            finally
                            {
                                this.context.Measure.End(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Sprite, TimeMeasureNames.Assembly);
                            }

                            if (File.Exists(spriteLogFile))
                            {
                                cacheSection.AddResultFile(spriteLogFile, CacheKeys.SpriteLogFileCacheKey);
                                foreach (var spritedFile in XDocument.Load(spriteLogFile).Elements("images").Elements("output").Select(e => (string)e.Attribute("file")))
                                {
                                    cacheSection.AddEndResultFile(spritedFile, CacheKeys.HashedSpriteImageCacheKey);
                                }
                            }

                            cacheSection.Store();
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
                this.context.Measure.End(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Sprite);
            }
        }

        private string GetRelativeSpriteCacheKey(ImageAssemblyScanOutput scanOutput)
        {
            return string.Join(
                ">",
                scanOutput.ImageReferencesToAssemble.Select(ir =>
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
                this.availableSourceImages);

            this.imageAssemblyScanVisitor.Context = context;

            this.context.Measure.Start("Scan");
            stylesheetNode = stylesheetNode.Accept(this.imageAssemblyScanVisitor);
            this.context.Measure.End("Scan");

            // Save the Scan Css (Single file per css)
            this.imageAssemblyScanVisitor.ImageAssemblyAnalysisLog.Save(this.ImageAssembleScanDestinationFile + Strings.ScanLogExtension);

            return stylesheetNode;
        }

        /// <summary>Executes the image assembly update visitor</summary>
        /// <param name="stylesheetNode">The stylesheet Ast node</param>
        /// <param name="spriteLogFiles"></param>
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

            var shouldHashImages = (this.ShouldHashImages && this.ImageHasher != null);
            this.imageAssemblyUpdateVisitor = new ImageAssemblyUpdateVisitor(
                this.SourceFile,
                spriteLogFiles,
                specificStyleSheet == null ? 1d : specificStyleSheet.Dpi.GetValueOrDefault(1d),
                this.OutputUnit,
                this.OutputUnitFactor,
                shouldHashImages ? this.ImageHasher.BasePrefixToRemoveFromInputPathInLog : null,
                shouldHashImages ? this.ImageHasher.BasePrefixToRemoveFromOutputPathInLog : null,
                shouldHashImages ? this.ImageHasher.BasePrefixToAddToOutputPath : null);

            this.imageAssemblyUpdateVisitor.Context = this.context;

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
