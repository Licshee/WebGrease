// ----------------------------------------------------------------------------------------------------
// <copyright file="MinifyJSActivity.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// <summary>
//   This task will call the minifier with settings from the args, and output the files to the specified directory
// </summary>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease.Activities
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Common;
    using Microsoft.Ajax.Utilities;

    using WebGrease.Extensions;

    /// <summary>This task will call the minifier with settings from the args, and output the files to the specified directory</summary>
    internal sealed class MinifyJSActivity
    {
        /// <summary>The context.</summary>
        private readonly IWebGreaseContext context;

        /// <summary>Initializes a new instance of the <see cref="MinifyJSActivity"/> class.</summary>
        /// <param name="context">The context.</param>
        public MinifyJSActivity(IWebGreaseContext context)
        {
            this.context = context;
        }

        /// <summary>Gets or sets SourceFile.</summary>
        internal string SourceFile { private get; set; }

        /// <summary>Gets or sets DestinationFile.</summary>
        internal string DestinationFile { private get; set; }

        /// <summary>Gets or sets the args to use beyond the default settings</summary>
        /// <value>String containing args</value>
        internal string MinifyArgs { private get; set; }

        /// <summary>Gets or sets the args given to minifier for CSL analyze step. These are just more switches for AjaxMin, but can come from different places in the build.</summary>
        /// <value>String containging args, e.g. "-WARN:4".</value>
        internal string AnalyzeArgs { private get; set; }

        /// <summary>Gets or sets a value indicating whether to analyze files (will use AnalyzeArgs when so).</summary>
        /// <value>True if the files should be analyzed.</value>
        internal bool ShouldAnalyze { private get; set; }

        /// <summary>Gets or sets a value indicating whether to compress files (will use MinifyArgs when so).</summary>
        /// <value>True if the files should be minified.</value>
        internal bool ShouldMinify { private get; set; }

        /// <summary>Gets or sets the file hasher.</summary>
        internal FileHasherActivity FileHasher { private get; set; }

        /// <summary>When overridden in a derived class, executes the task.</summary>
        /// <param name="contentItem">The result File.</param>
        internal void Execute(ContentItem contentItem = null)
        {
            var destinationDirectory = this.context.Configuration.DestinationDirectory;

            if ((contentItem == null) && string.IsNullOrWhiteSpace(this.SourceFile))
            {
                throw new ArgumentException("MinifyJSActivity - The source file cannot be null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(this.DestinationFile))
            {
                throw new ArgumentException("MinifyJSActivity - The destination file cannot be null or whitespace.");
            }

            if (contentItem == null)
            {
                contentItem = ContentItem.FromFile(this.SourceFile, this.SourceFile.MakeRelativeToDirectory(destinationDirectory));
            }

            this.context.SectionedAction(SectionIdParts.MinifyJsActivity)
                .CanBeCached(contentItem, new { this.ShouldAnalyze, this.ShouldMinify, this.AnalyzeArgs })
                .RestoreFromCacheAction(cacheSection =>
                {
                    var resultFile = cacheSection.GetCachedContentItems(CacheFileCategories.MinifyJsResult).FirstOrDefault();
                    if (resultFile == null)
                    {
                        return false;
                    }

                    if (this.FileHasher != null)
                    {
                        resultFile.WriteToHashedPath(this.context.Configuration.DestinationDirectory);
                        this.FileHasher.AppendToWorkLog(resultFile);
                    }
                    else
                    {
                        resultFile.WriteToContentPath(this.context.Configuration.DestinationDirectory);
                    }

                    return true;
                }).Execute(cacheSection =>
                {
                    var minifier = new Minifier { FileName = this.DestinationFile };
                    var minifierSettings = this.GetMinifierSettings(minifier);
                    var output = minifier.MinifyJavaScript(contentItem.Content, minifierSettings.JSSettings);

                    this.HandleMinifierErrors(minifier);

                    var relativeDestinationFile = Path.IsPathRooted(this.DestinationFile)
                        ? this.DestinationFile.MakeRelativeToDirectory(destinationDirectory)
                        : this.DestinationFile;

                    if (this.FileHasher != null)
                    {
                        // Write the result to the hard drive with hashing.
                        var result = this.FileHasher.Hash(ContentItem.FromContent(output, relativeDestinationFile));
                        cacheSection.AddResult(result, CacheFileCategories.MinifyJsResult, isEndResult: true);
                    }
                    else
                    {
                        // Write to disk
                        FileHelper.WriteFile(this.DestinationFile, output);
                        cacheSection.AddResult(ContentItem.FromFile(this.DestinationFile, relativeDestinationFile), CacheFileCategories.MinifyJsResult);
                    }

                    return true;
                });
        }

        /// <summary>Handles the minifier errors.</summary>
        /// <param name="minifier">The minifier.</param>
        private void HandleMinifierErrors(Minifier minifier)
        {
            // throw if this file has errors, but show all that are found
            if (minifier.ErrorList != null && minifier.ErrorList.Count > 0)
            {
                string exceptionMessage;
                if (this.context.Log.HasExtendedErrorHandler)
                {
                    // log each message individually so we can click through into the source
                    foreach (var errorMessage in minifier.ErrorList)
                    {
                        var errorHandler = errorMessage.IsError ? (LogExtendedError)this.context.Log.Error : this.context.Log.Warning;
                        errorHandler(
                            errorMessage.Subcategory,
                            errorMessage.ErrorCode,
                            errorMessage.HelpKeyword,
                            errorMessage.File,
                            errorMessage.StartLine,
                            errorMessage.StartColumn,
                            errorMessage.EndLine,
                            errorMessage.EndColumn,
                            errorMessage.Message);
                    }

                    exceptionMessage = "Error minifying the JS";
                }
                else
                {
                    // no logging method passed to us -- combine it all into one big ugly string
                    // and throw it in the exception.
                    var errorMessageForException = new StringBuilder();
                    foreach (var errorMessage in minifier.ErrorList)
                    {
                        errorMessageForException.AppendLine(errorMessage.ToString());
                    }

                    exceptionMessage = errorMessageForException.ToString();
                }

                throw new BuildWorkflowException(
                    exceptionMessage, "MinifyJSActivity", ErrorCode.Default, null, this.SourceFile, 0, 0, 0, 0, null);
            }
        }

        /// <summary>Gets the minifier settings.</summary>
        /// <param name="minifier">The minifier.</param>
        /// <returns>The minifier settings.</returns>
        private SwitchParser GetMinifierSettings(Minifier minifier)
        {
            // get the starting point default settings for the JavaScript minification,
            // depending on whether to ShouldMinify flag is set. If it is, just use
            // the default settings as the base. Otherwise start off with a set of switches
            // that do a minimum of alteration of the AST after parsing.
            CodeSettings defaultJSSettings;
            if (this.ShouldMinify)
            {
                // default minification settings (except make sure the file is properly terminated
                // in case it gets concatenated with other JS files later on)
                defaultJSSettings = new CodeSettings
                    {
                        TermSemicolons = true
                    };
            }
            else
            {
                // set a bunch of switches to skip most of the minification of the code
                defaultJSSettings = new CodeSettings
                    {
                        OutputMode = OutputMode.MultipleLines,
                        PreserveFunctionNames = true,
                        CollapseToLiteral = false,
                        LocalRenaming = LocalRenaming.KeepAll,
                        ReorderScopeDeclarations = false,
                        RemoveFunctionExpressionNames = false,
                        RemoveUnneededCode = false,
                        StripDebugStatements = false,
                        EvalLiteralExpressions = false,
                        TermSemicolons = true,
                        KillSwitch = -1
                    };
            }

            // we will parse a bunch of switches and apply them on top of the
            // default settings. If we want to analyze, use the analyze options
            // followed by the overrides. Otherwise just use the overrides.
            var args = this.ShouldAnalyze
                ? this.AnalyzeArgs + ' ' + this.MinifyArgs
                : this.MinifyArgs;

            // create a switch parser that starts with the default settings
            // (ignore the CSS settings) and parse the switches on top of them
            var switchParser = new SwitchParser(defaultJSSettings, null);
            switchParser.Parse(args);

            minifier.WarningLevel = switchParser.WarningLevel;
            return switchParser;
        }
    }
}