// ----------------------------------------------------------------------------------------------------
// <copyright file="AssemblerActivity.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// <summary>
//   The assembler activity.
// </summary>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease.Activities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Configuration;

    using WebGrease.Extensions;

    /// <summary>The assembler activity.</summary>
    internal sealed class AssemblerActivity
    {
        /// <summary>The context.</summary>
        private readonly IWebGreaseContext context;

        /// <summary>Initializes a new instance of the <see cref="AssemblerActivity"/> class.</summary>
        /// <param name="context">The context.</param>
        public AssemblerActivity(IWebGreaseContext context)
        {
            this.context = context;
            this.Inputs = new List<InputSpec>();
        }

        private bool endedInSemicolon;

        // regular expression to match a string ending with a semicolon optionally followed by any amount of multiline whitespace
        private static readonly Regex EndsWithSemicolon = new Regex(@";\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        
        /// <summary>Gets the list of inputs which need to be assembled.</summary>
        internal List<InputSpec> Inputs { get; private set; }

        /// <summary>Gets or sets the output file.</summary>
        internal string OutputFile { get; set; }

        /// <summary>Determines whether the input is original source or already processed.</summary>
        public bool InputIsOriginalSource { get; set; }

        /// <summary>Gets or sets the output file.</summary>
        internal PreprocessingConfig PreprocessingConfig { get; set; }

        /// <summary>Gets or sets a flag indicating whether to append semicolons between bundled files that don't already end in them</summary>
        internal bool AddSemicolons { get; set; }

        /// <summary>The execute method for the activity under question.</summary>
        internal void Execute()
        {
            if (string.IsNullOrWhiteSpace(this.OutputFile))
            {
                throw new ArgumentException("AssemblerActivity - The output file path cannot be null or whitespace.");
            }

            var assembleType = Path.GetExtension(this.OutputFile).Trim('.');

            this.context.Measure.Start(TimeMeasureNames.AssemblerActivity, assembleType);
            var cacheSection = this.context.Cache.BeginSection(TimeMeasureNames.AssemblerActivity, new { this.Inputs, PreprocessingConfig, AddSemicolons });
            try
            {
                // Add source inputs
                this.Inputs.ForEach(context.Cache.CurrentCacheSection.AddSourceDependency);

                // Create if the directory does not exist.
                var outputDirectory = Path.GetDirectoryName(this.OutputFile);
                if (!string.IsNullOrWhiteSpace(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // set the semicolon flag to true so the first file doesn't get a semicolon added before it. 
                // IF we are interesting in adding semicolons between bundled files (eg: JavaScript), then this flag 
                // will get set after outputting each file, depending on whether or not that file ends in a semicolon.
                // the NEXT file will look at the flag and add one if the previous one didn't end in a semicolon.
                this.endedInSemicolon = true;

                using (var writer = new StreamWriter(this.OutputFile, false, Encoding.UTF8))
                {
                    this.context.Log.Information("Start bundling output file: {0}".InvariantFormat(this.OutputFile));
                    foreach (var file in this.Inputs.GetFiles(context.Configuration.SourceDirectory, this.context.Log))
                    {
                        this.AppendFile(writer, file, this.PreprocessingConfig);
                    }
                    this.context.Log.Information("End bundling output file: {0}".InvariantFormat(this.OutputFile));
                }

                cacheSection.AddResultFile(this.OutputFile, "bundle");
            }
            catch (Exception exception)
            {
                throw new WorkflowException(
                    "AssemblerActivity - Error happened while executing the assembler activity", exception);
            }
            finally
            {
                cacheSection.EndSection();
                this.context.Measure.End(TimeMeasureNames.AssemblerActivity, assembleType);
            }
        }

        /// <summary>The append file.</summary>
        /// <param name="writer">The writer.</param>
        /// <param name="input">The input.</param>
        /// <param name="preprocessingConfig">The configuration for the preprocessing.</param>
        private void AppendFile(TextWriter writer, string input, PreprocessingConfig preprocessingConfig = null)
        {
            // Add a newline to make sure what comes next doesn't get mistakenly attached to the end of
            // a single-line comment or anything. add two so we get an easy-to-read separation between files
            // for debugging purposes.
            writer.WriteLine();
            writer.WriteLine();

            // if we want to separate files with semicolons and the previous file didn't have one, add one now
            if (this.AddSemicolons && !this.endedInSemicolon)
            {
                writer.Write(';');
            }

            writer.WriteLine("/* {0} */".InvariantFormat(input));
            writer.WriteLine();

            // Add the css/javascript code
            var content = File.ReadAllText(input, Encoding.UTF8);

            // Executing any applicable preprocessors from the list of configured preprocessors on the file content
            if (preprocessingConfig != null && preprocessingConfig.Enabled)
            {
                content = this.context.Preprocessing.Process(content, input, preprocessingConfig);
                if (content == null)
                {
                    throw new WorkflowException("Could not assembly the file {0} because one of the preprocessors threw an error.".InvariantFormat(input));
                }
            }

            writer.Write(content);
            writer.WriteLine();

            // don't even bother checking for a semicolon if we aren't interested in adding one
            if (AddSemicolons)
            {
                this.endedInSemicolon = EndsWithSemicolon.IsMatch(content);
            }
        }
    }
}
