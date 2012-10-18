// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CssLocalizationInput.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The Css localization input.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Activities
{
    using System.Collections.Generic;

    /// <summary>The Css localization input.</summary>
    internal sealed class CssLocalizationInput
    {
        /// <summary>Initializes a new instance of the <see cref="CssLocalizationInput"/> class.</summary>
        internal CssLocalizationInput()
        {
            this.Locales = new List<string>();
            this.Themes = new List<string>();
        }

        /// <summary>Gets the Locales.</summary>
        internal IList<string> Locales { get; private set; }

        /// <summary>Gets the Themes.</summary>
        internal IList<string> Themes { get; private set; }

        /// <summary>Gets or sets the source file path.</summary>
        internal string SourceFile { get; set; }

        /// <summary>Gets or sets the destination file path.</summary>
        internal string DestinationFile { get; set; }
    }
}
