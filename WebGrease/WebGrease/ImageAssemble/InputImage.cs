// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InputImage.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Defines Image position where the image will be rendered in vertical sprite (Left / Right).
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.ImageAssemble
{
    using System.Collections.Generic;

    /// <summary>Defines Image position where the image will be rendered in vertical sprite (Left / Right).</summary>
    public enum ImagePosition
    {
        /// <summary>
        /// Left Aligned
        /// </summary>
        Left = 0,

        /// <summary>
        /// Right Aligned
        /// </summary>
        Right,

        /// <summary>
        /// Center Aligned (Horizontally)
        /// </summary>
        Center
    }

    /// <summary>This class defines individual Input Image that needs to be assembled.
    /// <remarks>This class is intended to be used only when invoking IAT from ImageAssembleTask.</remarks>
    /// </summary>
    public class InputImage
    {
        /// <summary>The duplicate image paths.</summary>
        private readonly List<string> duplicateImagePaths = new List<string>();

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the InputImage class. The default image position used is Left.
        /// </summary>
        public InputImage()
        {
            this.Position = ImagePosition.Left;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Image path for this object.
        /// </summary>
        public string AbsoluteImagePath { get; set; }

        /// <summary>Gets or sets the original image path.</summary>
        public string OriginalImagePath { get; set; }

        /// <summary>
        /// Gets or sets Image Position (Left/Right) for this object.
        /// </summary>
        public ImagePosition Position { get; set; }

        /// <summary>Gets DuplicateImagePaths.</summary>
        public IList<string> DuplicateImagePaths
        {
            get
            {
                return this.duplicateImagePaths;
            }
        }

        #endregion
    }
}
