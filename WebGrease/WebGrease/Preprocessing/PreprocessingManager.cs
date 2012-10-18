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
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using WebGrease.Activities;
    using WebGrease.Configuration;
    using WebGrease.Extensions;

    /// <summary>
    /// This is the proprocessing manager, it manages all the loaded preprocessors, and the calls to them
    /// </summary>
    public class PreprocessingManager
    {
        #region Static Fields

        /// <summary>
        /// The Lazy initialization singleton.
        /// </summary>
        private static readonly Lazy<PreprocessingManager> Singleton = new Lazy<PreprocessingManager>(() => new PreprocessingManager(), true);

        #endregion

        #region Fields
        
        /// <summary>
        /// The lock object for initialization.
        /// </summary>
        private readonly object initializedLock = new object();

        [ImportMany(typeof(IPreprocessingEngine))]
        private readonly IList<IPreprocessingEngine> registeredPreprocessingEngines = new List<IPreprocessingEngine>();

        /// <summary>
        /// True when initialized, otherwise false.
        /// </summary>
        private bool initialized;

        /// <summary>
        /// The directory where the plugin assemblies reside.
        /// </summary>
        private string plugInDirectory;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PreprocessingManager"/>.
        /// Can only be called from within the class by the Singleton construction.
        /// </summary>
        private PreprocessingManager()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Singleton method of the single instance of the preprocessing manager.
        /// </summary>
        public static PreprocessingManager Instance
        {
            get
            {
                return Singleton.Value;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Initialized method to be called once in each application session by the caller planning to use the preprocessors.
        /// This will try to load the plugins from the plugin folder, and try to initialize each of them.
        /// Will report progess to the log information action.
        /// </summary>
        /// <param name="logInformation"></param>
        /// <param name="logError"></param>
        /// <param name="plugInPath"></param>
        public void Initialize(Action<string> logInformation = null, LogError logError = null, string plugInPath = null)
        {
            logInformation = logInformation ?? ((s1) => { });
            logError = logError ?? ((s1, s2, s3) => { });
            // logExtendedError = logExtendedError ?? ((s1, s2, s3, s4, s5, s6, s7, s8, s9) => { });
            if (!this.initialized)
            {
                lock (this.initializedLock)
                {
                    if (!this.initialized)
                    {
                        logInformation("preprocessing initialize start");
                        this.initialized = true;
                        this.plugInDirectory = plugInPath;

                        // If no plugin path was provided, we use the assembly path.
                        if (String.IsNullOrEmpty(this.plugInDirectory))
                        {
                            var assemblyFilInfo = new FileInfo(Assembly.GetCallingAssembly().FullName);
                            this.plugInDirectory = assemblyFilInfo.DirectoryName;
                        }

                        if (!String.IsNullOrWhiteSpace(this.plugInDirectory))
                        {
                            if (!Directory.Exists(this.plugInDirectory))
                            {
                                logError(new DirectoryNotFoundException(this.plugInDirectory), "Could not find the plugin path {0}".InvariantFormat(this.plugInDirectory), null);
                                return;
                            }

                            // And now use MEF to load all possible plugins.
                            logInformation("preprocessing plugin path: {0}".InvariantFormat(this.plugInDirectory));
                            using (var addInCatalog = new AggregateCatalog())
                            {
                                addInCatalog.Catalogs.Add(new DirectoryCatalog(this.plugInDirectory));

                                using (var addInContainer = new CompositionContainer(addInCatalog))
                                {
                                    //Fill the imports of this object
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
                                        logInformation("preprocessing engine found: {0}".InvariantFormat(registeredPreprocessingEngine.Name));
                                    }
                                }
                            }
                        }
                        logInformation("preprocessing initialize end");
                    }
                }
            }
        }

        /// <summary>
        /// This will call any of the registered preprocessor plugins, that are named in the provided preprocessing config, in the order they are configured.
        /// It will only call the preprocessor if it reports it can handle the filetype. (Using Canprocess).
        /// It will loop through all of them and then return the processed file.
        /// A null value returned by the preprocessors indicates an exceptipon has occurred, and we should break of processing.
        /// The plugins themselves will report the detailed error through the logError and logExtendedError actions.
        /// </summary>
        /// <param name="fileContent">The file content.</param>
        /// <param name="fullFileName">The full filename.</param>
        /// <param name="preprocessConfig">The preprocessing config.</param>
        /// <param name="logInformation">The log information action.</param>
        /// <param name="logError">The log error action.</param>
        /// <param name="logExtendedError">The extended log error action.</param>
        /// <returns>
        /// If no preprocessors are found, the passed in file contents.
        /// Or the result of the pre processors, or null if there was an error while calling the preprocessors.
        /// </returns>
        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Justification = "Simple exception.")]
        public string Process(string fileContent, string fullFileName, PreprocessingConfig preprocessConfig, Action<string> logInformation = null, LogError logError = null, LogExtendedError logExtendedError = null)
        {
            logInformation = logInformation ?? ((s1) => { });
            logError = logError ?? ((s1, s2, s3) => { });
            logExtendedError = logExtendedError ?? ((s1, s2, s3, s4, s5, s6, s7, s8, s9) => { });

            if (!this.initialized)
            {
                throw new Exception("Preprocessing manager has not been Initialized.");
            }

            // Select all the registered preprocessors that are named in the configguration in the order in which they appear in the config.
            logInformation("Registered preprocessors to use: {0}".InvariantFormat(string.Join(";", preprocessConfig.PreprocessingEngines)));
            var preprocessorsToUse = preprocessConfig.PreprocessingEngines.SelectMany(ppe => this.registeredPreprocessingEngines.Where(rppe => rppe.Name.Equals(ppe)));

            // Loop through each available engine that was also configured
            foreach (var preprocessingEngine in preprocessorsToUse)
            {
                // Check if the engine can process the file
                if (preprocessingEngine.CanProcess(fullFileName, preprocessConfig))
                {
                    logInformation("preprocessing with: {0}".InvariantFormat(preprocessingEngine.Name));
                    // Get the new content
                    var newContent = preprocessingEngine.Process(fileContent, fullFileName, preprocessConfig, logInformation, logError, logExtendedError);

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

        #endregion
    }
}