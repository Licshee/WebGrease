// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JSLocalizationInput.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The js localization input.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Activities
{
    using System.Collections.Generic;

    /// <summary>The js localization input.</summary>
    internal sealed class JSLocalizationInput
    {
        /// <summary>Initializes a new instance of the <see cref="JSLocalizationInput"/> class.</summary>
        internal JSLocalizationInput()
        {
            this.Locales = new List<string>();
        }

        /// <summary>Gets the Locales.</summary>
        internal IList<string> Locales { get; private set; }

        /// <summary>Gets or sets the source file path.</summary>
        internal string SourceFile { get; set; }

        /// <summary>Gets or sets the destination file path.</summary>
        internal string DestinationFile { get; set; }
    }
}
