// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PhotoAssemble.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This class assembles JPEG images
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.ImageAssemble
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>This class assembles JPEG images</summary>
    internal class NotSupportedAssemble : ImageAssembleBase
    {
        /// <summary>
        /// Gets image type
        /// </summary>
        internal override ImageFormat Format
        {
            get
            {
                return ImageFormat.Bmp;
            }
        }

        /// <summary>
        /// Gets image type
        /// </summary>
        internal override ImageType Type
        {
            get
            {
                return ImageType.NotSupported;
            }
        }

        /// <summary>
        /// Gets default extension for Image type.
        /// </summary>
        internal override string DefaultExtension
        {
            get
            {
                return ".bmp";
            }
        }

        /// <summary>For image types that are not supported, write a separate entry in the image map
        /// since we are not assembling these images.</summary>
        /// <param name="inputImages">The input Images.</param>
        internal override void Assemble(Dictionary<InputImage, Bitmap> inputImages)
        {
            foreach (var entry in inputImages)
            {
                this.ImageXmlMap.AppendToXml(entry.Key.ImagePath, "Not supported");
            }
        }
    }
}
