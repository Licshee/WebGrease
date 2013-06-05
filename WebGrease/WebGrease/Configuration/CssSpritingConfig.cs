// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResourceType.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// -----------------------------------------------------------------------

namespace WebGrease.Configuration
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;
    using Extensions;

    /// <summary>
    /// Configuration object for image spriting.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Spriting", Justification="Despite FxCop, this is spelled correctly.")]
    public class CssSpritingConfig : INamedConfig
    {
        /// <summary>
        /// Creates a new instance of the CssSpritingConfig class.
        /// </summary>
        public CssSpritingConfig()
        {
            this.ShouldAutoSprite = true;
            this.ImagePadding = 50;
            this.ShouldAutoVersionBackgroundImages = true;
            this.ImagesToIgnore = new string[0];
            this.DestinationImageFolder = "images";
            this.OutputUnitFactor = 1d;
        }

        /// <summary>
        /// Creates a new instance of the CssSpritingConfig class.
        /// </summary>
        public CssSpritingConfig(XElement element)
            : this()
        {
            Contract.Requires(element != null);
            /* expect this format:
            <Spriting config="Debug">
             <SpriteImages>false</SpriteImages>
             <ImagePadding>75</ImagePadding>
             <ImagesToIgnore>image.gif,image.jpg</ImagesToIgnore>
             <AutoVersionBackgroundImages>false</AutoVersionBackgroundImages>
             <OutputUnit>rem</OutputUnit>
             <OutputUnitFactor>0.1</OutputUnitFactor>
           </Spriting>
            */

            this.Name = (string)element.Attribute("config") ?? string.Empty;

            foreach (var descendant in element.Descendants())
            {
                var name = descendant.Name.ToString();
                var value = descendant.Value;

                switch (name)
                {
                    case "ImagePadding":
                        this.ImagePadding = value.TryParseInt32();
                        break;
                    case "ImagesToIgnore":
                        this.ImagesToIgnore = value.IsNullOrWhitespace() ? new string[0] : value.Split(',').Distinct();
                        break;
                    case "AutoVersionBackgroundImages":
                        this.ShouldAutoVersionBackgroundImages = value.TryParseBool();
                        break;
                    case "SpriteImages":
                        this.ShouldAutoSprite = value.TryParseBool();
                        break;
                    case "OutputUnit":
                        this.OutputUnit = value;
                        break;
                    case "OutputUnitFactor":
                        double outputUnitFactor;
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out outputUnitFactor))
                        {
                            this.OutputUnitFactor = outputUnitFactor;
                        }
                        break;
                    case "IgnoreImagesWithNonDefaultBackgroundSize":
                        this.IgnoreImagesWithNonDefaultBackgroundSize = value.TryParseBool();
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the name of this configuration
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets a value (in pixels) of the space buffer between images.
        /// </summary>
        public int ImagePadding { get; internal set; }

        /// <summary>
        /// Gets the collection of image names to ignore
        /// </summary>
        public IEnumerable<string> ImagesToIgnore { get; internal set; }

        /// <summary>Gets or sets a value indicating whether the set's background images should be autoversioned (renamed).</summary>
        internal bool ShouldAutoVersionBackgroundImages { get; set; }

        /// <summary>Gets or sets a value indicating whether the set's background images should be sprited.</summary>
        internal bool ShouldAutoSprite { get; set; }

        /// <summary>
        /// Gets the destination folder for images.
        /// </summary>
        internal string DestinationImageFolder { get; set; }

        /// <summary>
        /// Gets the output unit (px,rem,em).
        /// </summary>
        internal string OutputUnit { get; set; }

        /// <summary>
        /// Gets the output unit factor (for rem with html font-size:65.2%: 0.1).
        /// </summary>
        internal double OutputUnitFactor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore images that have a background-size property set to non-default ('auto' or 'auto auto').
        /// </summary>
        internal bool IgnoreImagesWithNonDefaultBackgroundSize { get; set; }
    }
}
