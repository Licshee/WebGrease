// -----------------------------------------------------------------------
// <copyright file="CssMinificationConfig.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// -----------------------------------------------------------------------

namespace WebGrease.Configuration
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Xml.Linq;
    using Extensions;

    /// <summary>
    /// Configuration for CSS Minification settings.
    /// </summary>
    internal class CssMinificationConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CssMinificationConfig"/> class.
        /// </summary>
        public CssMinificationConfig()
        {
            this.ShouldMinify = true;
            this.ForbiddenSelectors = new string[0];
            this.RemoveSelectors = new string[0];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CssMinificationConfig"/> class.
        /// </summary>
        /// <param name="element">element containing css config info</param>
        public CssMinificationConfig(XElement element)
            : this()
        {
            Contract.Requires(element != null);
            /* expect this format:
            <Minification config="Debug">
             <Minify>false</Minify>
           </Minification>
            */

            var nameAttribute = element.Attribute("config");
            this.Name = nameAttribute != null ? nameAttribute.Value : string.Empty;

            foreach (var descendant in element.Descendants())
            {
                var name = descendant.Name.ToString();
                var value = descendant.Value;

                switch (name)
                {
                    case "Minify":
                        this.ShouldMinify = value.TryParseBool();
                        break;
                    case "ValidateLowerCase":
                        this.ShouldValidateLowerCase = value.TryParseBool();
                        break;
                    case "ExcludeProperties":
                        this.ShouldExcludeProperties = value.TryParseBool();
                        break;
                    case "ProhibitedSelectors":
                        this.ForbiddenSelectors = value.IsNullOrWhitespace() ? new string[0] : value.Split(';');
                        break;
                    case "RemoveSelectors":
                        this.RemoveSelectors = value.IsNullOrWhitespace() ? new string[0] : value.Split(';');
                        break;

                }
            }
        }

        /// <summary>
        /// Gets or sets the name for this config instance.
        /// </summary>
        internal string Name { get; set; }

        /// <summary>Gets or sets a value indicating whether the set should be minified.</summary>
        internal bool ShouldMinify { get; set; }

        /// <summary>
        /// Gets or sets a value indicating wheter to validate selectores and properties to be all lower case.
        /// </summary>
        internal bool ShouldValidateLowerCase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether css should be validated for lower case.
        /// </summary>
        internal bool ShouldExcludeProperties { get; set; }

        /// <summary>
        /// Gets or sets a collection of selectors which are forbidden to be in the file.
        /// </summary>
        internal IEnumerable<string> ForbiddenSelectors { get; set; }

        /// <summary>
        /// Gets or sets a collection of selectors that will be ignored and not outputted.
        /// </summary>
        internal IEnumerable<string> RemoveSelectors { get; set; }
    }
}
