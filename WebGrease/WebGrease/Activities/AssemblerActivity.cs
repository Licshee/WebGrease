// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AssemblerActivity.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The assembler activity.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Configuration;

    using WebGrease.Common;
    using WebGrease.Extensions;
    using WebGrease.Preprocessing;

    /// <summary>The assembler activity.</summary>
    internal sealed class AssemblerActivity
    {
        /// <summary>Initializes a new instance of the <see cref="AssemblerActivity"/> class.</summary>
        public AssemblerActivity()
        {
            this.Inputs = new List<InputSpec>();
            this.logInformation = (s1 => { });
            this.logError = ((s1, s2, s3) => { });
            this.logExtendedError = ((s1, s2, s3, s4, s5, s6, s7, s8, s9) => { });
        }

        private bool endedInSemicolon;

        // regular expression to match a string ending with a semicolon optionally followed by any amount of multiline whitespace
        private static Regex endsWithSemicolon = new Regex(@";\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        internal Action<string> logInformation;

        internal LogError logError;

        internal LogExtendedError logExtendedError;

        /// <summary>Gets the list of inputs which need to be assembled.</summary>
        internal IList<InputSpec> Inputs { get; private set; }

        /// <summary>Gets the output file.</summary>
        internal string OutputFile { get; set; }

        /// <summary>Gets the output file.</summary>
        internal PreprocessingConfig PreprocessingConfig { get; set; }

        /// <summary>Gets or sets a flag indicating whether to append semicolons between bundled files that don't already end in them</summary>
        internal bool AddSemicolons { get; set; }

        /// <summary>Gets or sets a flag indicating whether the single-line comment format can be used</summary>
        internal bool CanUseSingleLineComment { get; set; }

        /// <summary>The execute method for the activity under question.</summary>
        internal void Execute()
        {
            if (string.IsNullOrWhiteSpace(this.OutputFile))
            {
                throw new ArgumentException("AssemblerActivity - The output file path cannot be null or whitespace.");
            }

            try
            {
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
                    logInformation("Start bundling output file: {0}".InvariantFormat(this.OutputFile));
                    foreach (var input in this.Inputs.Where(_ => _ != null && !string.IsNullOrWhiteSpace(_.Path)))
                    {
                        // File Input
                        if (File.Exists(input.Path))
                        {
                            logInformation("- {0}".InvariantFormat(input.Path));
                            AppendFile(writer, input.Path, this.PreprocessingConfig);
                            continue;
                        }

                        // Directory Input
                        if (Directory.Exists(input.Path))
                        {
                            logInformation("Folder: {0}, Pattern: {1}, Options: {2}".InvariantFormat(input.Path, input.SearchPattern, input.SearchOption));
                            // Intentionally using Enum.Parse to throw an exception if bad string is passed for search option
                            foreach (var file in Directory.EnumerateFiles(input.Path, string.IsNullOrWhiteSpace(input.SearchPattern) ? "*.*" : input.SearchPattern, input.SearchOption).OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
                            {
                                logInformation("- {0}".InvariantFormat(file));
                                AppendFile(writer, file, this.PreprocessingConfig);
                            }

                            continue;
                        }

                        if (!input.IsOptional)
                        {
                            throw new FileNotFoundException("Could not find the file to assemble: " + input.Path, input.Path);
                        }
                        logInformation("End bundling output file: {0}".InvariantFormat(this.OutputFile));
                    }
                }
            }
            catch (Exception exception)
            {
                throw new WorkflowException("AssemblerActivity - Error happened while executing the assembler activity", exception);
            }
        }

        /// <summary>The append file.</summary>
        /// <param name="writer">The writer.</param>
        /// <param name="input">The input.</param>
        /// <param name="preprocessingConfig">The configuration for the preprocessing.</param>
        private void AppendFile(TextWriter writer, string input, PreprocessingConfig preprocessingConfig = null)
        {
            // if we want to separate files with semicolons and the previous file didn't have one, add one now
            if (this.AddSemicolons && !this.endedInSemicolon)
            {
                writer.Write(';');
            }

            // Add the file source comment
            writer.WriteLine();
            if (this.CanUseSingleLineComment)
            {
                writer.WriteLine("///#SOURCE 1 1 {0}".InvariantFormat(input));
            }
            else
            {
                writer.WriteLine("/*/#SOURCE 1 1 {0} */".InvariantFormat(input));
            }

            // Add the css/javascript code
            var content = File.ReadAllText(input, Encoding.UTF8);

            // Executing any applicable preprocessors from the list of configured preprocessors on the file content
            if (preprocessingConfig != null && preprocessingConfig.Enabled)
            {
                content = PreprocessingManager.Instance.Process(content, input, preprocessingConfig, logInformation, logError, logExtendedError);
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
                this.endedInSemicolon = endsWithSemicolon.IsMatch(content);
            }
        }
    }
}
