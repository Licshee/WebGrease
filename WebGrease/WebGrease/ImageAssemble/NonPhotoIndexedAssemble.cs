﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NonphotoIndexedAssemble.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This class assembles nonphoto, indexed images into a single image and saves it
//   in indexed format. Color quanization is done to ensure it can be saved in indexed
//   format.  PNG compression is used since it almost always yields smaller files than GIF
//   and it is very broadly supported.  The PNG optmizer is run after saving.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.ImageAssemble
{
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>This class assembles nonphoto, indexed images into a single image and saves it
    /// in indexed format. Color quanization is done to ensure it can be saved in indexed
    /// format.  PNG compression is used since it almost always yields smaller files than GIF
    /// and it is very broadly supported.  The PNG optmizer is run after saving.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704", Justification = "By Design")]
    internal class NonphotoIndexedAssemble : ImageAssembleBase
    {
        public NonphotoIndexedAssemble(IWebGreaseContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Gets image type
        /// </summary>
        internal override ImageFormat Format
        {
            get
            {
                return ImageFormat.Png;
            }
        }

        /// <summary>
        /// Gets image type
        /// </summary>
        internal override ImageType Type
        {
            get
            {
                return ImageType.NonphotoIndexed;
            }
        }

        /// <summary>
        /// Gets default extension for Image type.
        /// </summary>
        internal override string DefaultExtension
        {
            get
            {
                return ".png";
            }
        }

        /// <summary>Quantize colors before saving so that we can use an indexed format.
        /// Run the optimizer after saving.</summary>
        /// <param name="newImage">Image to be saved</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Invokes LCA approved tool OptiPNG.exe. This is by design.")]
        protected override void SaveImage(Bitmap newImage)
        {

            Bitmap packedBitmap = null;
            try
            {
                packedBitmap = ColorQuantizer.Quantize(newImage, PixelFormat.Format8bppIndexed);
                base.SaveImage(packedBitmap);
                this.OptimizeImage();
            }
            finally
            {
                if (packedBitmap != null)
                {
                    packedBitmap.Dispose();
                }
            }
        }

        /// <summary>Run the optimizer after passing through the image.</summary>
        /// <param name="image">Bitmap for image to pass through</param>
        /// <param name="inputImage">InputImage for image to pass through</param>
        protected override void PassThroughImage(Bitmap image, InputImage inputImage)
        {
            base.PassThroughImage(image, inputImage);
            this.OptimizeImage();
        }
    }
}
