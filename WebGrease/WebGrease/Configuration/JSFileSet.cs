// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JSFileSet.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   A set of js files that are defined together.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Xml.Linq;

    using WebGrease.Extensions;

    /// <summary>
    /// A set of js files that are defined together.
    /// </summary>
    public sealed class JSFileSet : IFileSet
    {
        /// <summary>
        /// This flag is used to determine whether we are using the local file-set
        /// Locales and not the global defaults. If we parse a file-set locale
        /// and this flag is false, we are going to clear anything we picked up from
        /// the global settings, thereby completely replacing the list of locales.
        /// </summary>
        private readonly bool usingFileSetLocales;

        /// <summary>
        /// Initializes a new instance of the <see cref="JSFileSet"/> class.
        /// </summary>
        internal JSFileSet()
        {
            this.Locales = new List<string>();
            this.Minification = new Dictionary<string, JsMinificationConfig>(StringComparer.OrdinalIgnoreCase);
            this.InputSpecs = new List<InputSpec>();
            this.AutoNaming = new Dictionary<string, AutoNameConfig>(StringComparer.OrdinalIgnoreCase);
            this.Bundling = new Dictionary<string, BundlingConfig>(StringComparer.OrdinalIgnoreCase);
            this.Validation = new Dictionary<string, JSValidationConfig>(StringComparer.OrdinalIgnoreCase);
            this.Preprocessing = new Dictionary<string, PreprocessingConfig>(StringComparer.OrdinalIgnoreCase);
            this.LoadedConfigurationFiles = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JSFileSet"/> class.
        /// </summary>
        /// <param name="defaultLocales">The default set of locales.</param>
        /// <param name="defaultMinification">The default set of minification configs.</param>
        /// <param name="defaultPreProcessing">The default pre processing config. </param>
        /// <param name="defaultBundling">The default bundling configuration</param>
        internal JSFileSet(IList<string> defaultLocales, IDictionary<string, JsMinificationConfig> defaultMinification, IDictionary<string, PreprocessingConfig> defaultPreProcessing, IDictionary<string, BundlingConfig> defaultBundling)
            : this()
        {
            // if we were given a default set of locales, add then to the list now
            if (defaultLocales != null && defaultLocales.Count > 0)
            {
                foreach (var locale in defaultLocales)
                {
                    this.Locales.Add(locale.Trim());
                }
            }

            // if we were given a default set of minification configs, copy them now
            if (defaultMinification != null && defaultMinification.Count > 0)
            {
                foreach (var configuration in defaultMinification.Keys)
                {
                    this.Minification[configuration] = defaultMinification[configuration];
                }
            }

            // if we were given a default set of minification configs, copy them now
            if (defaultPreProcessing != null && defaultPreProcessing.Count > 0)
            {
                foreach (var configuration in defaultPreProcessing.Keys)
                {
                    this.Preprocessing[configuration] = defaultPreProcessing[configuration];
                }
            }

            // Set the default bundling
            if (defaultBundling != null && defaultBundling.Count > 0)
            {
                foreach (var configuration in defaultBundling.Keys)
                {
                    this.Bundling[configuration] = defaultBundling[configuration];
                }
            }
        }

        /// <summary>Initializes a new instance of the <see cref="JSFileSet"/> class.</summary>
        /// <param name="jsFileSetElement">config element containing info for a set of js files</param>
        /// <param name="sourceDirectory">The base directory.</param>
        /// <param name="defaultLocales">The default set of locales.</param>
        /// <param name="defaultMinification">The default set of minification configs.</param>
        /// <param name="defaultPreProcessing">The default pre processing config. </param>
        /// <param name="defaultBundling">The default Bundling.</param>
        /// <param name="globalConfig">The global Config.</param>
        /// <param name="configurationFile">The configuration File.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Is not excessive")]
        internal JSFileSet(XElement jsFileSetElement, string sourceDirectory, IList<string> defaultLocales, IDictionary<string, JsMinificationConfig> defaultMinification, IDictionary<string, PreprocessingConfig> defaultPreProcessing, IDictionary<string, BundlingConfig> defaultBundling, GlobalConfig globalConfig, string configurationFile)
            : this(defaultLocales, defaultMinification, defaultPreProcessing, defaultBundling)
        {
            Contract.Requires(jsFileSetElement != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(sourceDirectory));

            var outputAttribute = jsFileSetElement.Attribute("output");
            this.Output = outputAttribute != null ? outputAttribute.Value : string.Empty;
            
            this.GlobalConfig = globalConfig;

            var fileSetElements = jsFileSetElement.Descendants().ToList();
            WebGreaseConfiguration.ForEachConfigSourceElement(
                jsFileSetElement, 
                configurationFile,
                (element, s) =>
                {
                    LoadedConfigurationFiles.Add(s);
                    fileSetElements.AddRange(element.Descendants());
                });

            foreach (var element in fileSetElements)
            {
                var name = element.Name.ToString();
                var value = element.Value;

                switch (name)
                {
                    case "Minification":
                        // generate a configuration and set it on the dictionary. If the name
                        // already exists, this clobbers it.
                        this.Minification.AddNamedConfig(new JsMinificationConfig(element));
                        break;
                    case "Preprocessing":
                        this.Preprocessing.AddNamedConfig(new PreprocessingConfig(element));
                        break;
                    case "Autoname":
                        this.AutoNaming.AddNamedConfig(new AutoNameConfig(element));
                        break;
                    case "Bundling":
                        this.Bundling.AddNamedConfig(new BundlingConfig(element));
                        break;
                    case "Validation":
                        this.Validation.AddNamedConfig(new JSValidationConfig(element));
                        break;
                    case "Locales":
                        if (!this.usingFileSetLocales)
                        {
                            // we haven't found any file-set locales yet, so we
                            // are going to clear the list (if it has anything)
                            // so we clobber the default in effect.
                            this.usingFileSetLocales = true;
                            this.Locales.Clear();
                        }

                        WebGreaseConfiguration.AddSeperatedValues(this.Locales, value);
                        break;

                    case "Inputs":
                        this.InputSpecs.AddInputSpecs(sourceDirectory, element);
                        break;
                }
            }
        }

        /// <summary>Gets the external files.</summary>
        public IList<string> LoadedConfigurationFiles { get; private set; }

        /// <summary>Gets the output specified.</summary>
        public string Output { get; set; }

        /// <summary>Gets the locales.</summary>
        public IList<string> Locales { get; private set; }

        /// <summary>Gets the preprocessing configuration.</summary>
        public IDictionary<string, PreprocessingConfig> Preprocessing { get; private set; }

        /// <summary>
        /// Gets the dictionary of auto naming configs
        /// </summary>
        public IDictionary<string, AutoNameConfig> AutoNaming { get; private set; }

        /// <summary>
        /// Gets the dictionary of auto naming configs
        /// </summary>
        public IDictionary<string, BundlingConfig> Bundling { get; private set; }

        /// <summary>Gets the list of <see cref="InputSpec"/> items specified.</summary>
        public IList<InputSpec> InputSpecs { get; private set; }

        /// <summary>Gets the global config.</summary>
        internal GlobalConfig GlobalConfig { get; private set; }

        /// <summary>
        /// Gets the validation settings
        /// </summary>
        internal IDictionary<string, JSValidationConfig> Validation { get; private set; }

        /// <summary>Gets the dictionary of minification configurations.</summary>
        internal IDictionary<string, JsMinificationConfig> Minification { get; private set; }
    }
}
