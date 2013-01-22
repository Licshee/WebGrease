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
    using System.Xml.Linq;

    /// <summary>
    /// A set of Css files that are defined together.
    /// </summary>
    internal sealed class CssFileSet
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
            this.Preprocessing = new PreprocessingConfig();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CssFileSet"/> class.
        /// </summary>
        /// <param name="defaultLocales">The default set of locales.</param>
        /// <param name="defaultMinification">The default set of minification configs.</param>
        /// <param name="defaultSpriting">The default set of spriting configs.</param>
        /// <param name="defaultThemes">The default set of themes.</param>
        /// <param name="defaultPreprocessing">The default pre processing config.</param>
        internal CssFileSet(IList<string> defaultLocales, IDictionary<string, CssMinificationConfig> defaultMinification, IDictionary<string, CssSpritingConfig> defaultSpriting, IList<string> defaultThemes, PreprocessingConfig defaultPreprocessing)
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

            if (defaultPreprocessing != null)
            {
                this.Preprocessing = defaultPreprocessing;
            }
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
        internal CssFileSet(XElement cssFileSetElement, string sourceDirectory, IList<string> defaultLocales, IDictionary<string, CssMinificationConfig> defaultMinification, IDictionary<string, CssSpritingConfig> defaultSpriting, IList<string> defaultThemes, PreprocessingConfig defaultPreprocessing)
            : this(defaultLocales, defaultMinification, defaultSpriting, defaultThemes, defaultPreprocessing)
        {
            Contract.Requires(cssFileSetElement != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(sourceDirectory));

            var nameAttribute = cssFileSetElement.Attribute("name");
            this.Name = nameAttribute != null ? nameAttribute.Value : string.Empty;

            var outputAttribute = cssFileSetElement.Attribute("output");
            this.Output = outputAttribute != null ? outputAttribute.Value : string.Empty;

            foreach (var element in cssFileSetElement.Descendants())
            {
                var name = element.Name.ToString();
                var value = element.Value;

                switch (name)
                {
                    case "Minification":
                        // create a configuration object from the markup and set it in the dictionary.
                        // if it already exists (a default was there), clobber it with the new one.
                        var miniConfig = new CssMinificationConfig(element);
                        this.Minification[miniConfig.Name] = miniConfig;
                        break;
                    case "Spriting":
                        var spriteConfig = new CssSpritingConfig(element);
                        this.ImageSpriting[spriteConfig.Name] = spriteConfig;
                        break;
                    case "Autoname":
                        var autoNameConfig = new AutoNameConfig(element);
                        this.Autonaming[autoNameConfig.Name] = autoNameConfig;
                        break;
                    case "Bundling":
                        var bundlingConfig = new BundlingConfig(element);
                        this.Bundling[bundlingConfig.Name] = bundlingConfig;
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
                                this.Locales.Add(locale.Trim());
                            }
                        }
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

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            foreach (var theme in value.Split(Strings.SemicolonSeparator, StringSplitOptions.RemoveEmptyEntries))
                            {
                                this.Themes.Add(theme.Trim());
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

        /// <summary>Gets the themes.</summary>
        internal IList<string> Themes { get; private set; }

        /// <summary>Gets the locales.</summary>
        internal IList<string> Locales { get; private set; }

        /// <summary>Gets the dictionary of configurations.</summary>
        internal IDictionary<string, CssMinificationConfig> Minification { get; private set; }

        /// <summary>Gets the preprocessing configuration.</summary>
        internal PreprocessingConfig Preprocessing { get; private set; }

        /// <summary>
        /// Gets the dictionary of spriting configurations.
        /// </summary>
        internal IDictionary<string, CssSpritingConfig> ImageSpriting { get; private set; }

        /// <summary>
        /// Gets the dictionary of auto naming configs
        /// </summary>
        internal IDictionary<string, AutoNameConfig> Autonaming { get; private set; }

        /// <summary>
        /// Gets the dictionary of auto naming configs
        /// </summary>
        internal IDictionary<string, BundlingConfig> Bundling { get; private set; }

        /// <summary>Gets the name of the set.</summary>
        internal string Name { get; set; }

        /// <summary>Gets the output specified.</summary>
        internal string Output { get; set; }

        /// <summary>Gets the list of <see cref="InputSpec"/> items specified.</summary>
        internal IList<InputSpec> InputSpecs { get; private set; }
    }
}
