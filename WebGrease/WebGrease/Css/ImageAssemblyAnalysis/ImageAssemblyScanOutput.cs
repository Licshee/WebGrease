// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImageAssemblyScanOutput.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The image sprite scan output.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.ImageAssemblyAnalysis
{
    using System.Collections.Generic;
    using ImageAssemble;

    /// <summary>The image sprite scan output.</summary>
    public sealed class ImageAssemblyScanOutput
    {
        /// <summary>Initializes a new instance of the <see cref="ImageAssemblyScanOutput"/> class.</summary>
        public ImageAssemblyScanOutput()
        {
            this.ImageReferencesToAssemble = new List<InputImage>();
        }

        /// <summary>Gets or sets ImageAssemblyScanInput.</summary>
        public ImageAssemblyScanInput ImageAssemblyScanInput { get; set; }

        /// <summary>Gets ImageReferencesToAssemble.</summary>
        public IList<InputImage> ImageReferencesToAssemble { get; private set; }
    }
}
