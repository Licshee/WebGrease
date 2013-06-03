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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using WebGrease.Configuration;
    using WebGrease.Css.Extensions;
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

        /// <summary>The will execute the Activity</summary>
        /// <param name="fileSets">The file Sets.</param>
        /// <returns>If the execution was successfull.</returns>
        internal bool Execute(IEnumerable<IFileSet> fileSets)
        {
            var assembler = new AssemblerActivity(this.context);
            this.BundleFileSets(assembler, fileSets.OfType<JSFileSet>(), FileTypes.JS);
            this.BundleFileSets(assembler, fileSets.OfType<CssFileSet>(), FileTypes.CSS);
            return true;
        }

        /// <summary>The bundle file sets.</summary>
        /// <param name="assembler">The assembler.</param>
        /// <param name="fileSets">The file sets.</param>
        /// <param name="fileType">The file type.</param>
        private void BundleFileSets(AssemblerActivity assembler, IEnumerable<IFileSet> fileSets, FileTypes fileType)
        {
            if (fileSets.Any())
            {
                var varBySettings = new { fileSets, fileType, this.context.Configuration.ConfigType, this.context.Configuration.SourceDirectory, this.context.Configuration.DestinationDirectory };
                this.context.SectionedAction(SectionIdParts.BundleActivity, fileType.ToString())
                    .CanBeCached(varBySettings)
                    .RestoreFromCacheAction(this.RestoreBundleFromCache)
                    .Execute(cacheSection =>
                    {
                        this.context.Log.Information("Begin " + fileType + " bundle pipeline");
                        this.Bundle(assembler, fileSets, fileType);
                        this.context.Log.Information("End " + fileType + " bundle pipeline");
                        return true;
                    });
            }
        }

        /// <summary>The restore bundle from cache.</summary>
        /// <param name="cacheSection">The cache section.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private bool RestoreBundleFromCache(ICacheSection cacheSection)
        {
            var endResults = cacheSection.GetCachedContentItems(CacheFileCategories.AssemblerResult, true);
            endResults.ForEach(er => er.WriteToContentPath(this.context.Configuration.DestinationDirectory));
            return true;
        }

        /// <summary>The bundle.</summary>
        /// <param name="assembler">The assembler.</param>
        /// <param name="fileSets">The file sets.</param>
        /// <param name="fileType">The file type</param>
        private void Bundle(AssemblerActivity assembler, IEnumerable<IFileSet> fileSets, FileTypes fileType)
        {
            // processing pipeline per file set in the config
            foreach (var fileSet in fileSets)
            {
                var configType = this.context.Configuration.ConfigType;
                var bundleConfig = fileSet.Bundling.GetNamedConfig(configType);
                if (bundleConfig.ShouldBundleFiles)
                {
                    // for each file set (that isn't empty of inputs)
                    // bundle the files, however this can only be done on filesets that have an output value of a file (ie: has an extension)
                    var outputFile = Path.Combine(this.context.Configuration.DestinationDirectory, fileSet.Output);

                    this.context.SectionedAction(SectionIdParts.BundleActivity, fileType.ToString(), SectionIdParts.Process)
                        .CanBeCached(fileSet, bundleConfig, true)
                        .RestoreFromCacheAction(this.RestoreBundleFromCache)
                        .Execute(fileSetCacheSection =>
                        {
                            fileSet.LoadedConfigurationFiles.ForEach(fileSetCacheSection.AddSourceDependency);

                            if (Path.GetExtension(outputFile).IsNullOrWhitespace())
                            {
                                Console.WriteLine(ResourceStrings.InvalidBundlingOutputFile, outputFile);
                                return true;
                            }

                            var preprocessingConfig = fileSet.Preprocessing.GetNamedConfig(configType);
                            assembler.OutputFile = outputFile;
                            assembler.Inputs.Clear();
                            assembler.PreprocessingConfig = preprocessingConfig;
                            assembler.Inputs.AddRange(fileSet.InputSpecs);
                            assembler.MinimalOutput = bundleConfig.MinimalOutput;
                            var contentItem = assembler.Execute();
                            fileSetCacheSection.AddResult(contentItem, CacheFileCategories.AssemblerResult, true);

                            return true;
                        });
                }
            }
        }
    }
}