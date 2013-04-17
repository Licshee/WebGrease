// ----------------------------------------------------------------------------------------------------
// <copyright file="PreprocessingManager.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease.Preprocessing
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using WebGrease.Configuration;
    using WebGrease.Extensions;

    /// <summary>
    /// This is the proprocessing manager, it manages all the loaded preprocessors, and the calls to them
    /// </summary>
    public class PreprocessingManager
    {
        /// <summary>The context.</summary>
        private readonly IWebGreaseContext context;

        #region Fields

        /// <summary>The registered preprocessing engines.</summary>
        [ImportMany(typeof(IPreprocessingEngine))]
        private readonly IList<IPreprocessingEngine> registeredPreprocessingEngines = new List<IPreprocessingEngine>();

        #endregion

        #region Constructors and Destructors

        /// <summary>Initializes a new instance of the <see cref="PreprocessingManager"/> class. 
        /// Initializes a new instance of the <see cref="PreprocessingManager"/>.
        /// Can only be called from within the class by the Singleton construction.</summary>
        /// <param name="context">The context.</param>
        internal PreprocessingManager(IWebGreaseContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            this.context = context;
            this.Initialize(context.Configuration.PreprocessingPluginPath);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>This will call any of the registered preprocessor plugins, that are named in the provided preprocessing config, in the order they are configured.
        /// It will only call the preprocessor if it reports it can handle the filetype. (Using Canprocess).
        /// It will loop through all of them and then return the processed file.
        /// A null value returned by the preprocessors indicates an exceptipon has occurred, and we should break of processing.
        /// The plugins themselves will report the detailed error through the logError and logExtendedError actions.</summary>
        /// <param name="fileContent">The file content.</param>
        /// <param name="fullFileName">The full filename.</param>
        /// <param name="preprocessConfig">The preprocessing config.</param>
        /// <returns>If no preprocessors are found, the passed in file contents.
        /// Or the result of the pre processors, or null if there was an error while calling the preprocessors.</returns>
        public string Process(string fileContent, string fullFileName, PreprocessingConfig preprocessConfig)
        {
            this.context.Measure.Start(TimeMeasureNames.Preprocessing, TimeMeasureNames.Process);
            try
            {
                // Select all the registered preprocessors that are named in the configguration in the order in which they appear in the config.
                this.context.Log.Information("Registered preprocessors to use: {0}".InvariantFormat(string.Join(";", preprocessConfig.PreprocessingEngines)));
                var preprocessorsToUse = preprocessConfig.PreprocessingEngines.SelectMany(ppe => this.registeredPreprocessingEngines.Where(rppe => rppe.Name.Equals(ppe)));

                // Loop through each available engine that was also configured
                foreach (var preprocessingEngine in preprocessorsToUse)
                {
                    // Check if the engine can process the file
                    if (preprocessingEngine.CanProcess(fullFileName, preprocessConfig))
                    {
                        this.context.Log.Information("preprocessing with: {0}".InvariantFormat(preprocessingEngine.Name));

                        // Get the new content
                        var newContent = preprocessingEngine.Process(fileContent, fullFileName, preprocessConfig);

                        // Only if the content is not null (empty is allowed) do we actually use the results.
                        fileContent = newContent;

                        if (fileContent == null)
                        {
                            return null;
                        }
                    }
                }

                return fileContent;
            }
            finally
            {
                this.context.Measure.Start(TimeMeasureNames.Preprocessing, TimeMeasureNames.Process);
            }
        }

        #endregion

        /// <summary>Initialized method to be called once in each application session by the caller planning to use the preprocessors.
        /// This will try to load the plugins from the plugin folder, and try to initialize each of them.
        /// Will report progess to the log information action.</summary>
        /// <param name="pluginPath">The plugin path</param>
        private void Initialize(string pluginPath)
        {
            this.context.Measure.Start(TimeMeasureNames.Preprocessing, TimeMeasureNames.Initialize);
            this.context.Log.Information("preprocessing initialize start; from plugin path: {0}".InvariantFormat(pluginPath));

            // If no plugin path was provided, we use the assembly path.
            if (string.IsNullOrWhiteSpace(pluginPath))
            {
                var assemblyFilInfo = new FileInfo(Assembly.GetCallingAssembly().FullName);
                pluginPath = assemblyFilInfo.DirectoryName;
            }

            if (!string.IsNullOrWhiteSpace(pluginPath))
            {
                if (!Directory.Exists(pluginPath))
                {
                    this.context.Log.Error(
                        new DirectoryNotFoundException(pluginPath),
                        "Could not find the plugin path {0}".InvariantFormat(pluginPath));
                    return;
                }

                // And now use MEF to load all possible plugins.
                this.context.Log.Information("preprocessing plugin path: {0}".InvariantFormat(pluginPath));
                using (var addInCatalog = new AggregateCatalog())
                {
                    addInCatalog.Catalogs.Add(new DirectoryCatalog(pluginPath));

                    using (var addInContainer = new CompositionContainer(addInCatalog))
                    {
                        // Fill the imports of this object
                        try
                        {
                            addInContainer.ComposeParts(this);
                        }
                        catch (CompositionException compositionException)
                        {
                            Console.WriteLine(compositionException.ToString());
                        }

                        foreach (var registeredPreprocessingEngine in this.registeredPreprocessingEngines)
                        {
                            this.context.Log.Information(
                                "preprocessing engine found: {0}".InvariantFormat(registeredPreprocessingEngine.Name));
                        }
                    }
                }

                // Loop through each available engine and initialize it.
                foreach (var preprocessingEngine in this.registeredPreprocessingEngines)
                {
                    preprocessingEngine.Initialize(this.context);
                }
            }

            this.context.Log.Information("preprocessing initialize end;");
            this.context.Measure.End(TimeMeasureNames.Preprocessing, TimeMeasureNames.Initialize);
        }
    }
}