// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TemporaryOverrides.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using WebGrease.Configuration;
    using WebGrease.Extensions;

    /// <summary>The temporary overrides.</summary>
    public class TemporaryOverrides
    {
        /// <summary>The locales.</summary>
        private readonly List<string> locales = new List<string>();

        /// <summary>The themes.</summary>
        private readonly List<string> themes = new List<string>();

        /// <summary>The outputs.</summary>
        private readonly List<string> outputs = new List<string>();

        /// <summary>The output extensions.</summary>
        private readonly List<string> outputExtensions = new List<string>();

        /// <summary>The unique key.</summary>
        private string uniqueKey;

        /// <summary>Gets or sets a value indicating whether skip all.</summary>
        public bool SkipAll { get; set; }

        /// <summary>Gets the unique key.</summary>
        public string UniqueKey
        {
            get
            {
                return this.uniqueKey;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="TemporaryOverrides"/> class.</summary>
        /// <param name="overrideFile">The override file.</param>
        /// <returns>The <see cref="TemporaryOverrides"/>.</returns>
        public static TemporaryOverrides Create(string overrideFile)
        {
            var to = new TemporaryOverrides();
            to.LoadFromFile(overrideFile);
            to.uniqueKey = to.ToJson(true);

            // Only return when there are any values to override
            return to.locales.Any() || to.themes.Any() || to.outputs.Any() || to.SkipAll
                ? to
                : null;
        }

        /// <summary>The should ignore.</summary>
        /// <param name="contentItem">The content item.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public bool ShouldIgnore(ContentItem contentItem)
        {
            return
                contentItem != null
                && contentItem.Pivots != null
                && contentItem.Pivots.Any()
                && contentItem.Pivots.All(cp => this.ShouldIgnoreLocale(cp.Locale) || this.ShouldIgnoreTheme(cp.Theme));
        }

        /// <summary>The should ignore.</summary>
        /// <param name="contentPivot">The content Pivot.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public bool ShouldIgnore(ContentPivot contentPivot)
        {
            return
                contentPivot != null
                && (this.ShouldIgnoreLocale(contentPivot.Locale) || this.ShouldIgnoreTheme(contentPivot.Theme));
        }

        /// <summary>The should ignore.</summary>
        /// <param name="fileSet">The file set.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public bool ShouldIgnore(IFileSet fileSet)
        {
            return fileSet != null
                   && !string.IsNullOrWhiteSpace(fileSet.Output)
                   && (this.ShouldIgnoreOutputs(fileSet) || this.ShouldIgnoreOutputExtensions(fileSet));
        }

        /// <summary>The get items from a string seperated by a semicolon.</summary>
        /// <param name="items">The override locales.</param>
        /// <returns>The items.</returns>
        private static IEnumerable<string> GetItems(string items)
        {
            return
                items == null
                    ? new string[] { }
                    : items.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>Get items from multiple elements where the content values are seperated by a semicolon.</summary>
        /// <param name="elements">The elements.</param>
        /// <param name="elementName">The element name.</param>
        /// <returns>The items.</returns>
        private static IEnumerable<string> GetElementItems(IEnumerable<XElement> elements, string elementName)
        {
            return Enumerable.Where(GetItems(elements.Elements(elementName).Select(e => (string)e).FirstOrDefault()), i => !i.IsNullOrWhitespace())
                             .Select(i => i.Trim());
        }

        /// <summary>The should ignore outputs.</summary>
        /// <param name="fileSet">The file set.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private bool ShouldIgnoreOutputs(IFileSet fileSet)
        {
            return this.outputs.Any()
                   && !this.outputs.Any(
                       output =>
                       fileSet.Output.IndexOf(output, StringComparison.OrdinalIgnoreCase) >= 0
                       && (output.IndexOf(".", StringComparison.OrdinalIgnoreCase) == -1 || fileSet.Output.Count(o => o == '.') > 1));
        }

        /// <summary>The should ignore output extensions.</summary>
        /// <param name="fileSet">The file set.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private bool ShouldIgnoreOutputExtensions(IFileSet fileSet)
        {
            return this.outputExtensions.Any()
                   && !this.outputExtensions.Any(outputExtension => fileSet.Output.EndsWith(outputExtension, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The should ignore theme.</summary>
        /// <param name="themeToIgnore">The theme To Ignore.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private bool ShouldIgnoreTheme(string themeToIgnore)
        {
            return !string.IsNullOrWhiteSpace(themeToIgnore)
                   && this.themes.Any()
                   && !this.themes.Any(theme => themeToIgnore.IndexOf(theme, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>The should ignore locale.</summary>
        /// <param name="localeToIgnore">The locale To Ignore.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private bool ShouldIgnoreLocale(string localeToIgnore)
        {
            return !string.IsNullOrWhiteSpace(localeToIgnore)
                && this.locales.Any()
                && !this.locales.Any(locale => localeToIgnore.IndexOf(locale, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>The load from file.</summary>
        /// <param name="overrideFile">The override file.</param>
        private void LoadFromFile(string overrideFile)
        {
            if (File.Exists(overrideFile))
            {
                var doc = XDocument.Load(overrideFile);
                var overrideElements = doc.Elements("Overrides");
                this.SkipAll = overrideElements.Attributes("SkipAll").Select(a => (bool?)a).FirstOrDefault() == true;
                this.locales.AddRange(GetElementItems(overrideElements, "Locales"));
                this.themes.AddRange(GetElementItems(overrideElements, "Themes"));
                this.outputs.AddRange(GetElementItems(overrideElements, "Outputs"));
                this.outputExtensions.AddRange(GetElementItems(overrideElements, "OutputExtensions"));
            }
        }
    }
}