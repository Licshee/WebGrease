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
    using System.Text;
    using Common;
    using Microsoft.Ajax.Utilities;

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
        internal string SourceFile { get; set; }

        /// <summary>Gets or sets DestinationFile.</summary>
        internal string DestinationFile { get; set; }

        /// <summary>
        /// Gets or sets the args to use beyond the default settings
        /// </summary>
        /// <value>String containing args</value>
        internal string MinifyArgs { get; set; }

        /// <summary>
        /// Gets or sets the args given to minifier for CSL analyze step. These are just more switches for AjaxMin, but can come from different places in the build.
        /// </summary>
        /// <value>String containging args, e.g. "-WARN:4".</value>
        internal string AnalyzeArgs { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to analyze files (will use AnalyzeArgs when so).
        /// </summary>
        /// <value>True if the files should be analyzed.</value>
        internal bool ShouldAnalyze { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to compress files (will use MinifyArgs when so).
        /// </summary>
        /// <value>True if the files should be minified.</value>
        internal bool ShouldMinify { get; set; }

        /// <summary>When overridden in a derived class, executes the task.</summary>
        internal void Execute()
        {
            if (string.IsNullOrWhiteSpace(this.SourceFile))
            {
                throw new ArgumentException("MinifyJSActivity - The source file cannot be null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(this.DestinationFile))
            {
                throw new ArgumentException("MinifyJSActivity - The destination file cannot be null or whitespace.");
            }

            try
            {
                this.context.Measure.Start(TimeMeasureNames.MinifyJsActivity);

                // Initialize the minifier
                var minifier = new Minifier { FileName = this.SourceFile };
                var minifierSettings = this.GetMinifierSettings(minifier);

                var output = minifier.MinifyJavaScript(File.ReadAllText(this.SourceFile), minifierSettings.JSSettings);

                // throw if this file has errors, but show all that are found
                if (minifier.ErrorList != null && minifier.ErrorList.Count > 0)
                {
                    string exceptionMessage;
                    if (this.context.Log.HasExtendedErrorHandler)
                    {
                        // log each message individually so we can click through into the source
                        foreach (var errorMessage in minifier.ErrorList)
                        {
                            this.context.Log.ExtendedError(
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

                // Write to disk
                FileHelper.WriteFile(
                    this.DestinationFile, output, CreateOutputEncoding(minifierSettings.EncodingOutputName));
            }
            catch (Exception exception)
            {
                throw new WorkflowException(
                    "MinifyJSActivity - Error happened while executing the minify JS activity", exception);
            }
            finally
            {
                this.context.Measure.End(TimeMeasureNames.MinifyJsActivity);
            }
        }

        /// <summary>
        /// create an output encoding from the given encoding name (if any) with
        /// the appropriate fallback encoder.
        /// </summary>
        /// <param name="encodingOutputName">encoding name, use UTF-8 as the default</param>
        /// <returns>encoding object for JavaScript output</returns>
        private static Encoding CreateOutputEncoding(string encodingOutputName)
        {
            Encoding encodingOutput;
            var encoderFallback = new JSEncoderFallback();
            if (string.IsNullOrWhiteSpace(encodingOutputName))
            {
                // clone the UTF-8 encoder so we can change the fallback handler
                encodingOutput = (Encoding)Encoding.UTF8.Clone();
                encodingOutput.EncoderFallback = encoderFallback;
            }
            else
            {
                try
                {
                    // try to create an encoding from the encoding argument
                    encodingOutput = Encoding.GetEncoding(encodingOutputName, encoderFallback, new DecoderReplacementFallback("?"));
                }
                catch (ArgumentException e)
                {
                    throw new WorkflowException("Invalid output encoding name: {0}".FormatInvariant(encodingOutputName), e);
                }
            }

            return encodingOutput;
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