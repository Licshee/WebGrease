// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CssFileSet.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   A set of Css files that are defined together.
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
    /// A set of Css files that are defined together.
    /// </summary>
    internal sealed class CssFileSet : IFileSet
    {
        /// <summary>
        /// This flag is used to determine whether we are using the local file-set
        /// Locales and not the global defaults. If we parse a file-set locale
        /// and this flag is false, we are going to clear anything we picked up from
        /// the global settings, thereby completely replacing the default list of locales
        /// with the local set.
        /// </summary>
        private bool usingFileSetLocales;

        /// <summary>
        /// This flag is used to determine whether we are using the local file-set
        /// Themes and not the global defaults. If we parse a file-set theme
        /// and this flag is false, we are going to clear anything we picked up from
        /// the global settings, thereby completely replacing the default list of themes
        /// with the local set.
        /// </summary>
        private bool usingFileSetThemes;

        /// <summary>
        /// Initializes a new instance of the <see cref="CssFileSet"/> class.
        /// </summary>
        internal CssFileSet()
        {
            this.Locales = new List<string>();
            this.Themes = new List<string>();
            this.Minification = new Dictionary<string, CssMinificationConfig>(StringComparer.OrdinalIgnoreCase);
            this.ImageSpriting = new Dictionary<string, CssSpritingConfig>(StringComparer.OrdinalIgnoreCase);
            this.InputSpecs = new List<InputSpec>();
            this.Autonaming = new Dictionary<string, AutoNameConfig>(StringComparer.OrdinalIgnoreCase);
            this.Bundling = new Dictionary<string, BundlingConfig>(StringComparer.OrdinalIgnoreCase);
            this.Preprocessing = new Dictionary<string, PreprocessingConfig>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CssFileSet"/> class.
        /// </summary>
        /// <param name="cssFileSetElement">config element containing info for a set of css files</param>
        /// <param name="sourceDirectory">The base directory.</param>
        /// <param name="defaultLocales">The default set of locales.</param>
        /// <param name="defaultMinification">The default set of minification configs.</param>
        /// <param name="defaultSpriting">The default set of spriting configs. </param>
        /// <param name="defaultThemes">The default set of themes.</param>
        /// <param name="defaultPreprocessing">The default pre processing config.</param>
        /// <param name="configurationFile">The parent configuration file</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Is not excessive")]
        internal CssFileSet(XElement cssFileSetElement, string sourceDirectory, IList<string> defaultLocales, IDictionary<string, CssMinificationConfig> defaultMinification, IDictionary<string, CssSpritingConfig> defaultSpriting, IList<string> defaultThemes, IDictionary<string, PreprocessingConfig> defaultPreprocessing, string configurationFile)
            : this(defaultLocales, defaultMinification, defaultSpriting, defaultThemes, defaultPreprocessing)
        {
            Contract.Requires(cssFileSetElement != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(sourceDirectory));

            var outputAttribute = cssFileSetElement.Attribute("output");
            this.Output = outputAttribute != null ? outputAttribute.Value : string.Empty;

            var fileSetElements = cssFileSetElement.Descendants().ToList();
            WebGreaseConfiguration.ForEachConfigSourceElement(cssFileSetElement, configurationFile, (element, s) => fileSetElements.AddRange(element.Descendants()));

            foreach (var element in fileSetElements)
            {
                var name = element.Name.ToString();
                var value = element.Value;

                switch (name)
                {
                    case "Minification":
                        // create a configuration object from the markup and set it in the dictionary.
                        // if it already exists (a default was there), clobber it with the new one.
                        this.Minification.AddNamedConfig(new CssMinificationConfig(element));
                        break;

                    case "Spriting":
                        this.ImageSpriting.AddNamedConfig(new CssSpritingConfig(element));
                        break;

                    case "Preprocessing":
                        this.Preprocessing.AddNamedConfig(new PreprocessingConfig(element));
                        break;

                    case "Autoname":
                        this.Autonaming.AddNamedConfig(new AutoNameConfig(element));
                        break;

                    case "Bundling":
                        this.Bundling.AddNamedConfig(new BundlingConfig(element));
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

                    case "Themes":
                        if (!this.usingFileSetThemes)
                        {
                            // we haven't found any file-set themes yet, so we
                            // are going to clear the list (if it has anything)
                            // so we clobber the default in effect.
                            this.usingFileSetThemes = true;
                            this.Themes.Clear();
                        }
                        WebGreaseConfiguration.AddSeperatedValues(this.Themes, value);
                        break;

                    case "Inputs":
                        this.InputSpecs.AddInputSpecs(sourceDirectory, element);
                        break;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CssFileSet"/> class.
        /// </summary>
        /// <param name="defaultLocales">The default set of locales.</param>
        /// <param name="defaultMinification">The default set of minification configs.</param>
        /// <param name="defaultSpriting">The default set of spriting configs.</param>
        /// <param name="defaultThemes">The default set of themes.</param>
        /// <param name="defaultPreprocessing">The default pre processing config.</param>
        private CssFileSet(IList<string> defaultLocales, IDictionary<string, CssMinificationConfig> defaultMinification, IDictionary<string, CssSpritingConfig> defaultSpriting, IList<string> defaultThemes, IDictionary<string, PreprocessingConfig> defaultPreprocessing)
            : this()
        {
            // if we were given a default set of locales, add them to the list
            if (defaultLocales != null && defaultLocales.Count > 0)
            {
                foreach (var locale in defaultLocales)
                {
                    this.Locales.Add(locale);
                }
            }

            // if we were given a default set of themes, add them to the list
            if (defaultThemes != null && defaultThemes.Count > 0)
            {
                foreach (var theme in defaultThemes)
                {
                    this.Themes.Add(theme);
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

            // if we were given a default set of spriting configs, copy them now
            if (defaultSpriting != null && defaultSpriting.Count > 0)
            {
                foreach (var configuration in defaultSpriting.Keys)
                {
                    this.ImageSpriting[configuration] = defaultSpriting[configuration];
                }
            }

            // if we were given a default set of minification configs, copy them now
            if (defaultPreprocessing != null && defaultPreprocessing.Count > 0)
            {
                foreach (var configuration in defaultPreprocessing.Keys)
                {
                    this.Preprocessing[configuration] = defaultPreprocessing[configuration];
                }
            }
        }

        /// <summary>Gets the locales.</summary>
        public IList<string> Locales { get; private set; }

        /// <summary>Gets the preprocessing configuration.</summary>
        public IDictionary<string, PreprocessingConfig> Preprocessing { get; private set; }

        /// <summary>
        /// Gets the dictionary of auto naming configs
        /// </summary>
        public IDictionary<string, AutoNameConfig> Autonaming { get; private set; }

        /// <summary>
        /// Gets the dictionary of auto naming configs
        /// </summary>
        public IDictionary<string, BundlingConfig> Bundling { get; private set; }

        /// <summary>Gets the output specified.</summary>
        public string Output { get; set; }

        /// <summary>Gets the list of <see cref="InputSpec"/> items specified.</summary>
        public IList<InputSpec> InputSpecs { get; private set; }

        /// <summary>Gets the themes.</summary>
        internal IList<string> Themes { get; private set; }

        /// <summary>Gets the dictionary of configurations.</summary>
        internal IDictionary<string, CssMinificationConfig> Minification { get; private set; }

        /// <summary>
        /// Gets the dictionary of spriting configurations.
        /// </summary>
        internal IDictionary<string, CssSpritingConfig> ImageSpriting { get; private set; }
    }
}
