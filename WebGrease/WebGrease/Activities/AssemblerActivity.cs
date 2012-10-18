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

        internal Action<string> logInformation;

        internal LogError logError;

        internal LogExtendedError logExtendedError;

        /// <summary>Gets the list of inputs which need to be assembled.</summary>
        internal IList<InputSpec> Inputs { get; private set; }

        /// <summary>Gets the output file.</summary>
        internal string OutputFile { get; set; }

        /// <summary>Gets the output file.</summary>
        internal PreprocessingConfig PreprocessingConfig { get; set; }

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
            // Add the comment
            writer.WriteLine();
            writer.WriteLine("/* {0} */".InvariantFormat(input));
            writer.WriteLine();

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
        }
    }
}
