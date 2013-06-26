// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NonphotoNonindexedAssemble.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This class assembles nonphoto, nonindexed images into a single image and saves it
//   in nonindexed format. PNG compression is used since GIF doesn't support nonindexed formats.
//   The PNG optmizer is run after saving.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.ImageAssemble
{
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;

    /// <summary>This class assembles nonphoto, nonindexed images into a single image and saves it
    /// in nonindexed format. PNG compression is used since GIF doesn't support nonindexed formats.
    /// The PNG optmizer is run after saving.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704", Justification = "By Design")]
    internal class NonphotoNonindexedAssemble : ImageAssembleBase
    {
        public NonphotoNonindexedAssemble(IWebGreaseContext context)
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
                return ImageType.NonphotoNonindexed;
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

        /// <summary>Run the optimizer after saving.</summary>
        /// <param name="newImage">Image to be saved</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Invokes LCA approved tool OptiPNG.exe. This is by design.")]
        protected override void SaveImage(Bitmap newImage)
        {
            if (!File.Exists(this.AssembleFileName))
            {
                base.SaveImage(newImage);
                this.OptimizeImage();
            }
        }

        /// <summary>Run the optimizer after passing through the image.</summary>
        /// <param name="image">Bitmap for image to pass through</param>
        /// <param name="inputImage">InputImage for image to pass through</param>
        protected virtual void PassThroughImage(Bitmap image, InputImage inputImage)
        {
            this.OptimizeImage();
        }
    }
}
