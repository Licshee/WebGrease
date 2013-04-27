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
        private const string MinifiedCssResultCacheKey = "MinifyCssResultCacheKey";

        private const string HashedImageCacheKey = "HashedImageCacheKey";

        /// <summary>The context.</summary>
        private readonly IWebGreaseContext context;

        /// <summary>The image assembly scan visitor.</summary>
        private ImageAssemblyScanVisitor _imageAssemblyScanVisitor;

        /// <summary>The image assembly update visitor.</summary>
        private ImageAssemblyUpdateVisitor _imageAssemblyUpdateVisitor;

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

        /// <summary>Gets or sets Image Assembly Update Output.</summary>
        /// <remarks>Optional - Needed only when image spriting is needed.</remarks>
        internal string ImageAssembleUpdateDestinationFile { get; set; }

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

        /// <summary>Gets or sets the path to the hashed images file</summary>
        internal string HashedImagesLogFile { get; set; }

        /// <summary>Gets or sets the output path.</summary>
        public string OutputPath { get; set; }

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
                this.SourceFile, 
                new {
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
                });

            try
            {
                if (cacheSection.IsValid())
                {
                    cacheSection.RestoreFile(MinifiedCssResultCacheKey, this.DestinationFile, true);
                    if (this.ShouldAssembleBackgroundImages)
                    {
                        cacheSection.RestoreFiles(HashedImageCacheKey, this.ImagesOutputDirectory, false);
                    }

                    return;
                }

                // Load the Css parser and stylesheet Ast
                this.context.Measure.Start(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Parse);
                AstNode stylesheetNode = CssParser.Parse(new FileInfo(this.SourceFile), false);
                this.context.Measure.End(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Parse);

                var css = ApplyConfiguredVisitors(stylesheetNode);

                // Step # 9 - Write the destination file to hard drive
                FileHelper.WriteFile(this.DestinationFile, css, Encoding.UTF8);

                cacheSection.AddResultFile(this.DestinationFile, MinifiedCssResultCacheKey, this.OutputPath);
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
                stylesheetNode = this.AssembleBackgroundImages(stylesheetNode);
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
        private AstNode AssembleBackgroundImages(AstNode stylesheetNode)
        {
            this.context.Measure.Start(TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Sprite);
            try
            {
                // Step # 6 - Execute the pipeline for image assembly scan
                stylesheetNode = this.ExecuteImageAssemblyScan(stylesheetNode);

                // Step # 7 - Execute the pipeline for image assembly tool
                var spriteLogFiles = new List<string>();
                for (var count = 0; count < this._imageAssemblyScanVisitor.ImageAssemblyScanOutputs.Count; count++)
                {
                    string spriteLogFile;
                    var scanOutput = this._imageAssemblyScanVisitor.ImageAssemblyScanOutputs[count];
                    if (count == 0)
                    {
                        // Default Bucket
                        spriteLogFile = this.ImageAssembleScanDestinationFile + Strings.XmlExtension;
                    }
                    else
                    {
                        // Extra Buckets say "Lazy"
                        spriteLogFile = this.ImageAssembleScanDestinationFile + scanOutput.ImageAssemblyScanInput.BucketName
                                        + Strings.XmlExtension;
                    }

                    spriteLogFiles.Add(spriteLogFile);

                    var cacheSection = this.context.Cache.BeginSection(
                        "minifycss.sprite", 
                        new { relativeSpriteCacheKey = this.GetRelativeSpriteCacheKey(scanOutput) });
                    try
                    {
                        if (cacheSection.IsValid())
                        {
                            cacheSection.RestoreFiles(HashedImageCacheKey, this.ImagesOutputDirectory, false);
                            continue;
                        }

                        this.ExecuteImageAssembly(scanOutput.ImageReferencesToAssemble, spriteLogFile);

                        if (File.Exists(spriteLogFile))
                        {
                            foreach (var spritedFile in XDocument.Load(spriteLogFile).Elements("images").Elements("output").Select(e => (string)e.Attribute("file")))
                            {
                                cacheSection.AddResultFile(spritedFile, HashedImageCacheKey, this.ImagesOutputDirectory);
                            }
                        }
                    }
                    finally
                    {
                        cacheSection.EndSection();
                    }
                }

                // Step # 8 - Execute the pipeline for image assembly update
                stylesheetNode = this.ExecuteImageAssemblyUpdate(stylesheetNode, spriteLogFiles);

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
            // convert the source file paths to hashed file paths
            var hashedImagesToIgnore = new HashSet<string>();
            var renamedFiles = RenamedFilesLogs.LoadHashedImagesLogs(this.HashedImagesLogFile);
            foreach (var sourcePath in this.ImageAssembleReferencesToIgnore)
            {
                var hashedPath = renamedFiles.FindHashPath(sourcePath);
                if (!string.IsNullOrWhiteSpace(hashedPath))
                {
                    hashedImagesToIgnore.Add(hashedPath);
                }
            }

            this._imageAssemblyScanVisitor = new ImageAssemblyScanVisitor(this.SourceFile, hashedImagesToIgnore, this.AdditionalImageAssemblyBuckets, this.IgnoreImagesWithNonDefaultBackgroundSize, this.OutputUnit, this.OutputUnitFactor);
            stylesheetNode = stylesheetNode.Accept(this._imageAssemblyScanVisitor);

            // Save the Pretty Print Css
            // TODO: RTUIT: Why do we save the pretty print? Do we need it? Maybe only use in #DEBUG? Have found no difference/errors when commenting this out.
            // FileHelper.WriteFile(this.ImageAssembleUpdateDestinationFile, stylesheetNode.PrettyPrint(), Encoding.UTF8);

            // Save the Scan Css (Single file per css)
            this._imageAssemblyScanVisitor.ImageAssemblyAnalysisLog.Save(this.ImageAssembleScanDestinationFile + Strings.ScanLogExtension);

            return stylesheetNode;
        }

        /// <summary>Executes the image assembly update visitor</summary>
        /// <param name="stylesheetNode">The stylesheet Ast node</param>
        /// <param name="spriteLogFiles"></param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        private AstNode ExecuteImageAssemblyUpdate(AstNode stylesheetNode, IEnumerable<string> spriteLogFiles)
        {
            var specificStyleSheet = stylesheetNode as StyleSheetNode;
            this._imageAssemblyUpdateVisitor = new ImageAssemblyUpdateVisitor(
                this.SourceFile,
                spriteLogFiles,
                specificStyleSheet == null ? 1d : specificStyleSheet.Dpi.GetValueOrDefault(1d),
                this.OutputUnit,
                this.OutputUnitFactor);
            stylesheetNode = stylesheetNode.Accept(this._imageAssemblyUpdateVisitor);

            // Save the Pretty Print Css
            FileHelper.WriteFile(this.ImageAssembleUpdateDestinationFile, stylesheetNode.PrettyPrint(), Encoding.UTF8);

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
