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
    using System.Xml.Linq;
    using WebGrease;

    /// <summary>
    /// A set of js files that are defined together.
    /// </summary>
    internal sealed class JSFileSet
    {
        /// <summary>
        /// This flag is used to determine whether we are using the local file-set
        /// Locales and not the global defaults. If we parse a file-set locale
        /// and this flag is false, we are going to clear anything we picked up from
        /// the global settings, thereby completely replacing the list of locales.
        /// </summary>
        private bool usingFileSetLocales;

        /// <summary>
        /// Initializes a new instance of the <see cref="JSFileSet"/> class.
        /// </summary>
        internal JSFileSet()
        {
            this.Locales = new List<string>();
            this.Minification = new Dictionary<string, JsMinificationConfig>(StringComparer.OrdinalIgnoreCase);
            this.InputSpecs = new List<InputSpec>();
            this.Autonaming = new Dictionary<string, AutoNameConfig>(StringComparer.OrdinalIgnoreCase);
            this.Bundling = new Dictionary<string, BundlingConfig>(StringComparer.OrdinalIgnoreCase);
            this.Validation = new Dictionary<string, JSValidationConfig>(StringComparer.OrdinalIgnoreCase);
            this.Preprocessing = new PreprocessingConfig();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JSFileSet"/> class.
        /// </summary>
        /// <param name="defaultLocales">The default set of locales.</param>
        /// <param name="defaultMinification">The default set of minification configs.</param>
        /// <param name="defaultPreProcessing">The default pre processing config. </param>
        internal JSFileSet(IList<string> defaultLocales, IDictionary<string, JsMinificationConfig> defaultMinification, PreprocessingConfig defaultPreProcessing)
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

            this.Preprocessing = defaultPreProcessing;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JSFileSet"/> class.
        /// </summary>
        /// <param name="jsFileSetElement">config element containing info for a set of js files</param>
        /// <param name="sourceDirectory">The base directory.</param>
        /// <param name="defaultLocales">The default set of locales.</param>
        /// <param name="defaultMinification">The default set of minification configs.</param>
        /// <param name="defaultPreProcessing">The default pre processing config. </param>
        internal JSFileSet(XElement jsFileSetElement, string sourceDirectory, IList<string> defaultLocales, IDictionary<string, JsMinificationConfig> defaultMinification, PreprocessingConfig defaultPreProcessing)
            : this(defaultLocales, defaultMinification, defaultPreProcessing)
        {
            Contract.Requires(jsFileSetElement != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(sourceDirectory));

            var nameAttribute = jsFileSetElement.Attribute("name");
            this.Name = nameAttribute != null ? nameAttribute.Value : string.Empty;

            var outputAttribute = jsFileSetElement.Attribute("output");
            this.Output = outputAttribute != null ? outputAttribute.Value : string.Empty;

            foreach (var element in jsFileSetElement.Descendants())
            {
                var name = element.Name.ToString();
                var value = element.Value;

                switch (name)
                {
                    case "Minification":
                        // generate a configuration and set it on the dictionary. If the name
                        // already exists, this clobbers it.
                        var configuration = new JsMinificationConfig(element);
                        this.Minification[configuration.Name] = configuration;
                        break;
                    case "Preprocessing":
                        this.Preprocessing = new PreprocessingConfig(element);
                        break;
                    case "Autoname":
                        var autoNameConfig = new AutoNameConfig(element);
                        this.Autonaming[autoNameConfig.Name] = autoNameConfig;
                        break;
                    case "Bundling":
                        var bundlingConfig = new BundlingConfig(element);
                        this.Bundling[bundlingConfig.Name] = bundlingConfig;
                        break;
                    case "Validation":
                        var validateConfig = new JSValidationConfig(element);
                        this.Validation[validateConfig.Name] = validateConfig;
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

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            foreach (var locale in value.Split(Strings.SemicolonSeparator, StringSplitOptions.RemoveEmptyEntries))
                            {
                                this.Locales.Add(locale);
                            }
                        }
                        break;
                    case "Inputs":
                        foreach (var inputElement in element.Descendants())
                        {
                            var input = new InputSpec(inputElement, sourceDirectory);
                            if (!string.IsNullOrWhiteSpace(input.Path))
                            {
                                this.InputSpecs.Add(input);
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>Gets the name of the configuration.</summary>
        internal string Name { get; private set; }

        /// <summary>Gets the output specified.</summary>
        internal string Output { get; set; }

        /// <summary>Gets the locales.</summary>
        internal IList<string> Locales { get; private set; }

        /// <summary>Gets the preprocessing configuration.</summary>
        internal PreprocessingConfig Preprocessing { get; private set; }

        /// <summary>
        /// Gets the validation settings
        /// </summary>
        internal IDictionary<string, JSValidationConfig> Validation { get; private set; }

        /// <summary>Gets the dictionary of minification configurations.</summary>
        internal IDictionary<string, JsMinificationConfig> Minification { get; private set; }

        /// <summary>
        /// Gets the dictionary of auto naming configs
        /// </summary>
        internal IDictionary<string, AutoNameConfig> Autonaming { get; private set; }

        /// <summary>
        /// Gets the dictionary of auto naming configs
        /// </summary>
        internal IDictionary<string, BundlingConfig> Bundling { get; private set; }

        /// <summary>Gets the list of <see cref="InputSpec"/> items specified.</summary>
        internal List<InputSpec> InputSpecs { get; set; }
    }
}
