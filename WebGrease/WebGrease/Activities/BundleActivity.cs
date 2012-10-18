namespace WebGrease.Activities
{
    using System;
    using System.IO;
    using System.Linq;

    using WebGrease.Configuration;
    using WebGrease.Extensions;
    using WebGrease.Preprocessing;

    /// <summary>
    /// This activity will load all the preprocessors try and execute if they are configured and do whatever bundling is configured.
    /// </summary>
    internal class BundleActivity
    {
        private readonly WebGreaseConfiguration config;

        private readonly Action<string> logInformation;

        private readonly LogError logError;

        private readonly LogExtendedError logExtendedError;

        private readonly string configType;

        private readonly string pluginPath;

        /// <summary>Initializes a new instance of the <see cref="BundleActivity"/> class.</summary>
        /// <param name="config">The config to execute on.</param>
        /// <param name="logInformation">The logInformation object.</param>
        /// <param name="logError">The log error delegate.</param>
        /// <param name="logExtendedError">The log extended delegate.</param>
        /// <param name="configType">The config type.</param>
        /// <param name="pluginPath">(Optional) The plugin path</param>
        public BundleActivity(WebGreaseConfiguration config, Action<string> logInformation = null, LogError logError = null, LogExtendedError logExtendedError = null, string configType = null, string pluginPath = null)
        {
            this.config = config;
            this.logInformation = logInformation ?? ((s1) => { });
            this.logError = logError ?? ((s1, s2, s3) => { });
            this.logExtendedError = logExtendedError ?? ((s1, s2, s3, s4, s5, s6, s7, s8, s9) => { });
            this.configType = configType;
            this.pluginPath = pluginPath;
        }

        /// <summary>
        /// The will execute the Activity
        /// </summary>
        /// <returns>If the execution was successfull.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "logInformation", Justification = "RTUIT: Tbd")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "logError", Justification = "RTUIT: Tbd")]
        internal bool Execute()
        {
            // Initialize the preprocessors
            PreprocessingManager.Instance.Initialize(this.logInformation, this.logError, this.pluginPath);

            var assembler = new AssemblerActivity()
                {
                    logInformation = this.logInformation,
                    logError = this.logError,
                    logExtendedError = this.logExtendedError,
                };

            // CSS processing pipeline per file set in the config

            var cssFileSets = config.CssFileSets.Where(file => file.InputSpecs.Any() && !file.Output.IsNullOrWhitespace());

            if (cssFileSets.Any())
            {
                this.logInformation("Begin CSS bundle pipeline.");
                foreach (var fileSet in cssFileSets)
                {
                    var setConfig = WebGreaseConfiguration.GetNamedConfig(fileSet.Bundling, configType);

                    if (setConfig.ShouldBundleFiles)
                    {
                        // for each file set (that isn't empty of inputs)
                        // bundle the files, however this can only be done on filesets that have an output value of a file (ie: has an extension)
                        var outputfile = Path.Combine(config.DestinationDirectory, fileSet.Output);

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
                this.logInformation("End Css bundle pipeline.");
            }

            var jsFileSets = config.JSFileSets.Where(file => file.InputSpecs.Any() && !file.Output.IsNullOrWhitespace());
            if (jsFileSets.Any())
            {
                this.logInformation("Begin JS bundle pipeline.");
                foreach (var fileSet in jsFileSets)
                {
                    var setConfig = WebGreaseConfiguration.GetNamedConfig(fileSet.Bundling, configType);

                    if (setConfig.ShouldBundleFiles)
                    {
                        // for each file set (that isn't empty of inputs)
                        // bundle the files, however this can only be done on filesets that have an output value of a file (ie: has an extension)
                        var outputfile = Path.Combine(config.DestinationDirectory, fileSet.Output);

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
                this.logInformation("End JS bundle pipeline.");
            }

            return true;
        }
    }
}