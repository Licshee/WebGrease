// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CssLocalizationActivity.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Css Localization ActivityMode class
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using Common;

    /// <summary>Css Localization ActivityMode class</summary>
    internal sealed class CssLocalizationActivity
    {
        /// <summary>The context.</summary>
        private readonly IWebGreaseContext context;

        /// <summary>Initializes a new instance of the <see cref="CssLocalizationActivity"/> class.</summary>
        /// <param name="context">The context.</param>
        public CssLocalizationActivity(IWebGreaseContext context)
        {
            this.context = context;
            this.CssLocalizationInputs = new List<CssLocalizationInput>();
        }

        /// <summary>
        /// The destination directory.
        /// </summary>
        internal string DestinationDirectory { private get; set; }

        /// <summary>
        /// Gets or sets the  Semicolon separated folder paths to the theme resources.
        /// The resource files will be searched in the folder paths.
        /// </summary>
        internal string ThemesResourcesDirectory { private get; set; }

        /// <summary>
        /// Gets or sets the Semicolon separated paths to the locale resources.
        /// The resource files will be searched in the folder paths.
        /// </summary>
        internal string LocalesResourcesDirectory { private get; set; }

        /// <summary>
        /// Gets the Css Localization Inputs (Locales, Themes etc.)
        /// </summary>
        internal IList<CssLocalizationInput> CssLocalizationInputs { get; private set; }

        /// <summary>Localize and theme the input file.</summary>
        /// <param name="context">The context.</param>
        /// <param name="inputItem">The input file.</param>
        /// <param name="locales">The locales.</param>
        /// <param name="localeResources">The locale resources.</param>
        /// <param name="themes">The themes.</param>
        /// <param name="themeResources">The theme resources.</param>
        /// <param name="localizedAndThemedItemAction">The localized Item Action.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Resource keys should be lowercase")]
        internal static bool LocalizeAndTheme(IWebGreaseContext context, ContentItem inputItem, IEnumerable<string> locales, IDictionary<string, IDictionary<string, string>> localeResources, IEnumerable<string> themes, IDictionary<string, IDictionary<string, string>> themeResources, Func<ContentItem, bool> localizedAndThemedItemAction)
        {
            var successfull = true;
            return context.SectionedAction(SectionIdParts.CssLocalizationActivity).Execute(() =>
            {
                try
                {
                    var css = inputItem.Content;
                    foreach (var locale in locales.Select(t => t.ToLowerInvariant()))
                    {
                        var localizedCss = ResourcesResolver.ExpandResourceKeys(css, localeResources[locale]);

                        if (!themes.Any())
                        {
                            var contentItem = ContentItem.FromContent(localizedCss, inputItem, locale);
                            localizedAndThemedItemAction(contentItem);
                        }
                        else
                        {
                            foreach (var theme in themes.Select(t => t.ToLowerInvariant()))
                            {
                                var localizedAndthemedCss = ResourcesResolver.ExpandResourceKeys(localizedCss, themeResources[theme]);

                                // Compute the output file name
                                var contentItem = ContentItem.FromContent(localizedAndthemedCss, inputItem, locale, theme);
                                successfull = localizedAndThemedItemAction(contentItem) && successfull;
                            }
                        }
                    }
                }
                catch (ResourceOverrideException resourceOverrideException)
                {
                    // There was a resource override in folder path that does not
                    // allow resource overriding. For this case, we need to
                    // show a build error.
                    var errorMessage = string.Format(CultureInfo.CurrentUICulture, "CssLocalizationActivity - {0} has more than one value assigned. Only one value per key name is allowed in libraries and features. Resource key overrides are allowed at the product level only.", resourceOverrideException.TokenKey);
                    throw new WorkflowException(errorMessage, resourceOverrideException);
                }
                catch (Exception exception)
                {
                    throw new WorkflowException("CssLocalizationActivity - Error happened while executing the expand css resources activity", exception);
                }

                return successfull;
            });
        }

        /// <summary>When overridden in a derived class, executes the task.</summary>
        internal void Execute()
        {
            if (string.IsNullOrWhiteSpace(this.DestinationDirectory))
            {
                throw new ArgumentException("CssLocalizationActivity - The destination directory cannot be null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(this.ThemesResourcesDirectory))
            {
                throw new ArgumentException("CssLocalizationActivity - The css themes directory cannot be null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(this.LocalesResourcesDirectory))
            {
                throw new ArgumentException("CssLocalizationActivity - The css locales directory cannot be null or whitespace.");
            }

            this.context.SectionedAction(SectionIdParts.CssLocalizationActivity).Execute(() =>
            {
                try
                {
                    // Create the destination directory if does not exist.
                    Directory.CreateDirectory(this.DestinationDirectory);

                    foreach (var cssLocalizationInput in this.CssLocalizationInputs.Where(_ => (_ != null && !string.IsNullOrWhiteSpace(_.DestinationFile))))
                    {
                        var locales = cssLocalizationInput.Locales.Count == 0 ? new List<string> { Strings.DefaultLocale } : cssLocalizationInput.Locales;
                        foreach (var localeName in locales.Where(_ => !string.IsNullOrWhiteSpace(_)))
                        {
                            // Process the css for locale folders
                            this.ExpandLocaleAndThemeResources(cssLocalizationInput, localeName);
                        }
                    }
                }
                catch (ResourceOverrideException resourceOverrideException)
                {
                    // There was a resource override in folder path that does not
                    // allow resource overriding. For this case, we need to
                    // show a build error.
                    var errorMessage = string.Format(CultureInfo.CurrentUICulture, "CssLocalizationActivity - {0} has more than one value assigned. Only one value per key name is allowed in libraries and features. Resource key overrides are allowed at the product level only.", resourceOverrideException.TokenKey);
                    throw new WorkflowException(errorMessage, resourceOverrideException);
                }
                catch (Exception exception)
                {
                    throw new WorkflowException("CssLocalizationActivity - Error happened while executing the expand css resources activity", exception);
                }
            });
        }

        /// <summary>Process the files for themes defined at site definition layer.</summary>
        /// <param name="cssLocalizationInput">The css localization input.</param>
        /// <param name="localeName">Name of the locale.</param>
        private void ExpandLocaleAndThemeResources(CssLocalizationInput cssLocalizationInput, string localeName)
        {
            // Css locale resources
            Dictionary<string, string> cssLocaleResources;
            ResourcesManager.TryGetResources(this.LocalesResourcesDirectory, localeName, out cssLocaleResources);

            // Now perform a look up for themes for current site
            var themes = cssLocalizationInput.Themes.Count == 0 ? new List<string> { Strings.DefaultLocale } : cssLocalizationInput.Themes;

            // Apply the theme and locale resources
            foreach (var themeName in themes.Where(_ => !string.IsNullOrWhiteSpace(_)))
            {
                // Css theme resources (By design - Not caching here to keep the low footprint)
                Dictionary<string, string> cssThemeResources;
                ResourcesManager.TryGetResources(this.ThemesResourcesDirectory, themeName, out cssThemeResources);

                // Read the file from hard drive
                var cssContent = File.ReadAllText(cssLocalizationInput.SourceFile);

                // Apply the theme resources
                cssContent = ResourcesResolver.ExpandResourceKeys(cssContent, cssThemeResources);

                // Apply the locale resources
                cssContent = ResourcesResolver.ExpandResourceKeys(cssContent, cssLocaleResources);

                // Compute the output file name
                var destinationFile = cssLocalizationInput.DestinationFile.EndsWith(Strings.Css, StringComparison.OrdinalIgnoreCase) ? cssLocalizationInput.DestinationFile : Path.Combine(this.DestinationDirectory, localeName, string.Format(CultureInfo.InvariantCulture, "{0}_{1}.{2}", themeName, cssLocalizationInput.DestinationFile, Strings.Css));

                // Write the expanded file to disk
                FileHelper.WriteFile(destinationFile, cssContent);
            }
        }
    }
}
