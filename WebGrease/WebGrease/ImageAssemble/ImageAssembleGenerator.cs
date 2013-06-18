// ----------------------------------------------------------------------------------------------------
// <copyright file="ImageAssembleGenerator.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// <summary>
//   Factory class that invokes appropriate Assemble type to assemble images.
// </summary>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease.ImageAssemble
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Css.ImageAssemblyAnalysis;

    using WebGrease.Css.ImageAssemblyAnalysis.LogModel;

    /// <summary>Factory class that invokes appropriate Assemble type to assemble images.</summary>
    internal static class ImageAssembleGenerator
    {
        /// <summary>Invokes Assemble method of appropriate Image Assembler depending upon
        /// the type of images to be Assembled.</summary>
        /// <param name="inputImages">List of InputImage</param>
        /// <param name="packingType">Image Packing Type(Horizontal/Vertical)</param>
        /// <param name="assembleFileFolder">folder path where the assembled file will be created.</param>
        /// <param name="pngOptimizerToolCommand">PNG Optimizer tool command</param>
        /// <param name="dedup">Remove duplicate images</param>
        /// <param name="context">The webgrease context</param>
        /// <param name="imagePadding">The image padding</param>
        /// <param name="imageAssemblyAnalysisLog">The image Assembly Analysis Log.</param>
        /// <param name="forcedImageType">The forced image type to override detection.</param>
        /// <returns>The <see cref="ImageMap"/>.</returns>
        internal static ImageMap AssembleImages(ReadOnlyCollection<InputImage> inputImages, SpritePackingType packingType, string assembleFileFolder, string pngOptimizerToolCommand, bool dedup, IWebGreaseContext context, int? imagePadding = null, ImageAssemblyAnalysisLog imageAssemblyAnalysisLog = null, ImageType? forcedImageType = null)
        {
            return AssembleImages(inputImages, packingType, assembleFileFolder, null, pngOptimizerToolCommand, dedup, context, imagePadding, imageAssemblyAnalysisLog, forcedImageType);
        }

        /// <summary>Invokes Assemble method of appropriate Image Assembler depending upon
        /// the type of images to be Assembled.</summary>
        /// <param name="inputImages">List of InputImage</param>
        /// <param name="packingType">Image Packing Type(Horizontal/Vertical)</param>
        /// <param name="assembleFileFolder">folder path where the assembled file will be created.</param>
        /// <param name="mapFileName">The map File Name.</param>
        /// <param name="pngOptimizerToolCommand">PNG Optimizer tool command</param>
        /// <param name="dedup">Remove duplicate images</param>
        /// <param name="context">The webgrease context</param>
        /// <param name="imagePadding">The image padding</param>
        /// <param name="imageAssemblyAnalysisLog">The image Assembly Analysis Log.</param>
        /// <param name="forcedImageType">The forced image type to override detection.</param>
        /// <returns>The <see cref="ImageMap"/>.</returns>
        internal static ImageMap AssembleImages(ReadOnlyCollection<InputImage> inputImages, SpritePackingType packingType, string assembleFileFolder, string mapFileName, string pngOptimizerToolCommand, bool dedup, IWebGreaseContext context, int? imagePadding = null, ImageAssemblyAnalysisLog imageAssemblyAnalysisLog = null, ImageType? forcedImageType = null)
        {
            // deduping is optional.  CssPipelineTask already has deduping built into it, so it skips deduping in ImageAssembleTool.
            var inputImagesDeduped = dedup ? DedupImages(inputImages, context) : inputImages;
            var separatedLists = SeparateByImageType(inputImagesDeduped, forcedImageType);

#if DEBUG
            foreach (ImageType imageType in Enum.GetValues(typeof(ImageType)))
            {
                Console.WriteLine();
                Console.WriteLine(Enum.GetName(typeof(ImageType), imageType));
                foreach (var entry in separatedLists[imageType])
                {
                    Console.WriteLine(entry.Key.OriginalImagePath);
                }
            }
#endif

            var padding = imagePadding ?? DefaultPadding;
            var xmlMap = new ImageMap(mapFileName);
            var registeredAssemblers = RegisterAvailableAssemblers(context);
            Dictionary<InputImage, Bitmap> separatedList = null;
            foreach (var registeredAssembler in registeredAssemblers)
            {
                try
                {
                    // Set Image orientation as passed
                    registeredAssembler.PackingType = packingType;

                    // Set Xml Image Xml Map 
                    registeredAssembler.ImageXmlMap = xmlMap;

                    xmlMap.AppendPadding(padding.ToString(CultureInfo.InvariantCulture));
                    registeredAssembler.PaddingBetweenImages = padding;

                    // Set PNG Optimizer tool path for PngAssemble
                    registeredAssembler.OptimizerToolCommand = pngOptimizerToolCommand;

                    // Assemble images of this type
                    separatedList = separatedLists[registeredAssembler.Type];
                    
                    // Set Assembled Image Name
                    registeredAssembler.AssembleFileName = GenerateAssembleFileName(separatedList.Keys, assembleFileFolder) + registeredAssembler.DefaultExtension;

                    registeredAssembler.Assemble(separatedList);
                }
                finally
                {
                    if (separatedList != null)
                    {
                        foreach (var entry in separatedList)
                        {
                            if (entry.Value != null)
                            {
                                if (imageAssemblyAnalysisLog != null)
                                {
                                    imageAssemblyAnalysisLog.UpdateSpritedImage(registeredAssembler.Type, entry.Key.OriginalImagePath, registeredAssembler.AssembleFileName);
                                }

                                context.Cache.CurrentCacheSection.AddSourceDependency(entry.Key.AbsoluteImagePath);
                                entry.Value.Dispose();
                            }
                        }
                    }
                }
            }

            var notSupportedList = separatedLists[ImageType.NotSupported];
            if (notSupportedList != null && notSupportedList.Count > 0)
            {
                var message = new StringBuilder("The following files were not assembled because their formats are not supported:");
#if DEBUG
                foreach (var entry in notSupportedList)
                {
                    message.Append(" " + entry.Key.OriginalImagePath);
                }

                Console.WriteLine(message.ToString());
#endif
                throw new ImageAssembleException(message.ToString());
            }

            return xmlMap;
        }

        /// <summary>Registers available Image Assemblers.</summary>
        /// <param name="context">The global web grease context.</param>
        /// <returns>Dictionary of registered image assembler for file extension</returns>
        private static IEnumerable<ImageAssembleBase> RegisterAvailableAssemblers(IWebGreaseContext context)
        {
            var registeredAssemblers = new List<ImageAssembleBase>();

            var notSupportedAssemble = new NotSupportedAssemble(context);
            registeredAssemblers.Add(notSupportedAssemble);

            var photoAssemble = new PhotoAssemble(context);
            registeredAssemblers.Add(photoAssemble);

            var nonphotoNonindexedAssemble = new NonphotoNonindexedAssemble(context);
            registeredAssemblers.Add(nonphotoNonindexedAssemble);

            var nonphotoIndexedAssemble = new NonphotoIndexedAssemble(context);
            registeredAssemblers.Add(nonphotoIndexedAssemble);

            return registeredAssemblers;
        }

        /// <summary>
        /// Generates a custom file name for the sprite assembly file, based off of the images being used to make the sprite.
        /// </summary>
        /// <param name="inputImages">collection of images to be sprited</param>
        /// <param name="targetFolder">folder path the filename should be in.</param>
        /// <returns>the name of the file, without extention</returns>
        private static string GenerateAssembleFileName(IEnumerable<InputImage> inputImages, string targetFolder)
        {
            var uniqueKey = WebGreaseContext.ComputeContentHash(string.Join("|", inputImages.Select(i => i.AbsoluteImagePath)));

            // the filename is the hash code of the joined file names (to prevent a large sprite from going over the 260 limit of paths).
            return Path.GetFullPath(Path.Combine(targetFolder, uniqueKey));
        }

        /// <summary>Returns whether or not the given bitmap is a photo.</summary>
        /// <remarks>Just uses the huersitic that it is a photo if the source format is JPEG or EXIF.</remarks>
        /// <param name="bitmap">The bitmap to check</param>
        /// <returns>whether or not the given bitmap is a photo.</returns>
        private static bool IsPhoto(Bitmap bitmap)
        {
            return bitmap.RawFormat.Equals(ImageFormat.Jpeg) ||
                   bitmap.RawFormat.Equals(ImageFormat.Exif);
        }

        /// <summary>Returns whether or not the given bitmap is indexed.</summary>
        /// <param name="bitmap">The bitmap to check</param>
        /// <returns>whether or not the given bitmap is indexed.</returns>
        private static bool IsIndexed(Bitmap bitmap)
        {
            return (bitmap.PixelFormat & PixelFormat.Indexed) != 0;
        }

        /// <summary>Returns whether or not the given bitmap PixelFormat has alpha.</summary>
        /// <param name="bitmap">The bitmap to check</param>
        /// <returns>whether or not the given bitmap PixelFormat has alpha.</returns>
        private static bool HasAlpha(Bitmap bitmap)
        {
            return ((bitmap.PixelFormat & PixelFormat.Alpha) != 0) ||
                   ((bitmap.PixelFormat & PixelFormat.PAlpha) != 0);
        }

        /// <summary>Returns whether or not the given bitmap is indexable.
        /// Even if the image is not in indexed format currently, determine if it can be losslessly converted to indexed
        /// format by counting the colors and inspecting the alpha values used in the image.
        /// One palette entry is taken by all pixels with alpha=0 (regardless of RGB values).
        /// One palette entry is tken for each unique RGB value with alpha=255.
        /// If 256 or less palette entries are needed, then the image is indexable.
        /// Though PNG is capable of storing per-palette-entry alpha values, most formats are not able to, so only 
        /// images with fully opaque and fully transparent pixels will be considered indexable.</summary>
        /// <param name="bitmap">The bitmap to check</param>
        /// <returns>whether or not the given bitmap is losslessly indexable.</returns>
        private static bool IsIndexable(Bitmap bitmap)
        {
            int x, y;
            var transparentSeen = false;
            var colorSeen = new BitArray(1 << 24);
            var totalColorsSeen = 0;
            var w = bitmap.Width;
            var h = bitmap.Height;
            int rgb;

            if (!HasAlpha(bitmap))
            {
                if (w * h <= 256)
                {
                    return true;
                }
            }

            for (x = 0; x < w; x++)
            {
                for (y = 0; y < h; y++)
                {
                    var pixelColor = bitmap.GetPixel(x, y);
                    if (pixelColor.A == 0)
                    {
                        if (!transparentSeen)
                        {
                            totalColorsSeen++;
                            transparentSeen = true;
                        }
                    }
                    else if (pixelColor.A == 255)
                    {
                        rgb = (pixelColor.R << 16) + (pixelColor.G << 8) + pixelColor.B;
                        if (!colorSeen[rgb])
                        {
                            totalColorsSeen++;
                            colorSeen[rgb] = true;
                        }
                    }
                    else
                    {
#if DEBUG
                        Console.WriteLine(ResourceStrings.SemiTransparencyFound);
#endif
                        return false;
                    }

                    if (totalColorsSeen > 256)
                    {
#if DEBUG
                        Console.WriteLine(ResourceStrings.MoreThan256Colours);
#endif
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>Returns whether of not the given bitmap is multiframe.</summary>
        /// <param name="bitmap">The bitmap to check</param>
        /// <returns>whether or not the given bitmap is losslessly indexable.</returns>
        private static bool IsMultiframe(Bitmap bitmap)
        {
            var dimension = new FrameDimension(bitmap.FrameDimensionsList[0]);
            return bitmap.GetFrameCount(dimension) > 1;
        }

        /// <summary>Separates the list of input images into 4 lists based on type of image:
        /// (1) Not supported
        /// (2) Photo
        /// (3) Nonphoto, Nonindexed
        /// (4) Nonphoto, Indexed
        /// Returns a Dictionary indexed by image type.  Each entry is a Dictionary of InputImage, Bitmap pairs.
        /// All images that can't be read into a Bitmap and all images that have multiple frames (including animated GIF) go
        /// in the "not supported" list.
        /// All images of type JPEG or EXIF go into the "photo" list.
        /// All other images go into the "nonphoto, nonindexed" or "nonphoto, indexed" lists.
        /// The ones that are indexed or indexable (i.e. can be losslessly converted to indexed format) go into the 
        /// "nonphoto, indexed" list.
        /// The remaining bitmaps go into the "nonphoto, nonindexed" list.</summary>
        /// <param name="inputImages">list of input images to separate</param>
        /// <param name="forcedImageType">The forced image type to override detection.</param>
        /// <returns>separate lists per image type</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Legacy Code")]
        internal static Dictionary<ImageType, Dictionary<InputImage, Bitmap>> SeparateByImageType(ReadOnlyCollection<InputImage> inputImages, ImageType? forcedImageType = null)
        {
            var separatedLists = new Dictionary<ImageType, Dictionary<InputImage, Bitmap>>();
            foreach (ImageType imageType in Enum.GetValues(typeof(ImageType)))
            {
                separatedLists[imageType] = new Dictionary<InputImage, Bitmap>();
            }

            foreach (var inputImage in inputImages)
            {
                Bitmap bitmap = null;
#if DEBUG
                Console.WriteLine(inputImage.OriginalImagePath);
#endif
                try
                {
                    bitmap = (Bitmap)Image.FromFile(inputImage.AbsoluteImagePath);
                }
                catch
                {
                    bitmap = null;
                }

                if (bitmap == null)
                {
                    separatedLists[ImageType.NotSupported].Add(inputImage, bitmap);
                }
                else if (forcedImageType != null)
                {
                    separatedLists[forcedImageType.Value].Add(inputImage, bitmap);
                }
                else if (IsPhoto(bitmap))
                {
                    separatedLists[ImageType.Photo].Add(inputImage, bitmap);
                }
                else
                {
                    if (IsMultiframe(bitmap))
                    {
                        separatedLists[ImageType.NotSupported].Add(inputImage, bitmap);
                    }
                    else if (IsIndexed(bitmap) || IsIndexable(bitmap))
                    {
                        separatedLists[ImageType.NonphotoIndexed].Add(inputImage, bitmap);
                    }
                    else
                    {
                        separatedLists[ImageType.NonphotoNonindexed].Add(inputImage, bitmap);
                    }
                }
            }

            return separatedLists;
        }

        /// <summary>Removes duplicate images</summary>
        /// <param name="inputImages">list of input images to dedup</param>
        /// <param name="context">The webgrease context</param>
        /// <returns>deduped list of images</returns>
        private static ReadOnlyCollection<InputImage> DedupImages(ReadOnlyCollection<InputImage> inputImages, IWebGreaseContext context)
        {
            var inputImagesDeduped = new List<InputImage>();
            var imageHashDictionary = new Dictionary<string, InputImage>();
            foreach (var inputImage in inputImages)
            {
                var imageHashString = context.GetFileHash(inputImage.AbsoluteImagePath) + "." + inputImage.Position;
                if (imageHashDictionary.ContainsKey(imageHashString))
                {
                    var matchingImage = imageHashDictionary[imageHashString];
                    matchingImage.DuplicateImagePaths.Add(inputImage.AbsoluteImagePath);
#if DEBUG
                    Console.WriteLine(ResourceStrings.DuplicateFoundFormat, matchingImage.OriginalImagePath, inputImage.OriginalImagePath, inputImage.Position);
#endif
                }
                else
                {
                    imageHashDictionary.Add(imageHashString, inputImage);
                    inputImagesDeduped.Add(inputImage);
                }
            }

            return inputImagesDeduped.AsReadOnly();
        }

        /// <summary>
        /// Default Padding value
        /// </summary>
        internal const int DefaultPadding = 50;
    }

    /// <summary>Type of image</summary>
    internal enum ImageType
    {
        /// <summary>The not supported.</summary>
        NotSupported,

        /// <summary>The photo.</summary>
        Photo,

        /// <summary>The nonphoto nonindexed.</summary>
        NonphotoNonindexed,

        /// <summary>The nonphoto indexed.</summary>
        NonphotoIndexed,
    }
}
