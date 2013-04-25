// ----------------------------------------------------------------------------------------------------
// <copyright file="BundleActivity.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// <summary>
//   This activity will load all the preprocessors try and execute if they are configured and do whatever bundling is configured.
// </summary>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease.Activities
{
    using System;
    using System.IO;
    using System.Linq;

    using WebGrease.Configuration;
    using WebGrease.Extensions;

    /// <summary>
    /// This activity will load all the preprocessors try and execute if they are configured and do whatever bundling is configured.
    /// </summary>
    internal class BundleActivity
    {
        /// <summary>The context.</summary>
        private readonly WebGreaseContext context;

        /// <summary>Initializes a new instance of the <see cref="BundleActivity"/> class.</summary>
        /// <param name="webGreaseContext">The web grease context.</param>
        public BundleActivity(WebGreaseContext webGreaseContext)
        {
            this.context = webGreaseContext;
        }

        /// <summary>
        /// The will execute the Activity
        /// </summary>
        /// <returns>If the execution was successfull.</returns>
        internal bool Execute()
        {
            var assembler = new AssemblerActivity(this.context);

            // CSS processing pipeline per file set in the config
            var cssFileSets = this.context.Configuration.CssFileSets.Where(file => file.InputSpecs.Any() && !file.Output.IsNullOrWhitespace());

            if (cssFileSets.Any())
            {
                this.context.Log.Information("Begin CSS bundle pipeline.");
                foreach (var fileSet in cssFileSets)
                {
                    var setConfig = WebGreaseConfiguration.GetNamedConfig(fileSet.Bundling, this.context.Configuration.ConfigType);

                    if (setConfig.ShouldBundleFiles)
                    {
                        // for each file set (that isn't empty of inputs)
                        // bundle the files, however this can only be done on filesets that have an output value of a file (ie: has an extension)
                        var outputfile = Path.Combine(this.context.Configuration.DestinationDirectory, fileSet.Output);

                        if (Path.GetExtension(outputfile).IsNullOrWhitespace())
                        {
                            Console.WriteLine(ResourceStrings.InvalidBundlingOutputFile, outputfile);
                            continue;
                        }

                        assembler.OutputFile = outputfile;
                        assembler.Inputs.Clear();
                        assembler.PreprocessingConfig = fileSet.Preprocessing;

                        foreach (var inputSpec in fileSet.InputSpecs)
                        {
                            assembler.Inputs.Add(inputSpec);
                        }

                        assembler.Execute();
                    }
                }

                this.context.Log.Information("End Css bundle pipeline.");
            }

            var jsFileSets = this.context.Configuration.JSFileSets.Where(file => file.InputSpecs.Any() && !file.Output.IsNullOrWhitespace());
            if (jsFileSets.Any())
            {
                this.context.Log.Information("Begin JS bundle pipeline.");
                foreach (var fileSet in jsFileSets)
                {
                    var setConfig = WebGreaseConfiguration.GetNamedConfig(fileSet.Bundling, this.context.Configuration.ConfigType);

                    if (setConfig.ShouldBundleFiles)
                    {
                        // for each file set (that isn't empty of inputs)
                        // bundle the files, however this can only be done on filesets that have an output value of a file (ie: has an extension)
                        var outputfile = Path.Combine(this.context.Configuration.DestinationDirectory, fileSet.Output);

                        if (Path.GetExtension(outputfile).IsNullOrWhitespace())
                        {
                            Console.WriteLine(ResourceStrings.InvalidBundlingOutputFile, outputfile);
                            continue;
                        }

                        assembler.OutputFile = outputfile;
                        assembler.Inputs.Clear();
                        assembler.PreprocessingConfig = fileSet.Preprocessing;

                        foreach (var inputSpec in fileSet.InputSpecs)
                        {
                            assembler.Inputs.Add(inputSpec);
                        }

                        assembler.Execute();
                    }
                }

                this.context.Log.Information("End JS bundle pipeline.");
            }

            return true;
        }
    }
}