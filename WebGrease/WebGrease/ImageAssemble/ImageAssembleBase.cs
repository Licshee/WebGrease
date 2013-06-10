// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImageAssembleBase.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Packing type of Assembled image.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.ImageAssemble
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Common;
    using Css.ImageAssemblyAnalysis;

    /// <summary>Packing type of Assembled image.</summary>
    internal enum SpritePackingType
    {
        /// <summary>
        /// Tiles Images vertically
        /// </summary>
        Vertical = 0,


        /// <summary>
        /// Tiles Images horizontally
        /// </summary>
        Horizontal
    }

    /// <summary>Provides the base implementation for Assembling image,
    /// which can be overridden by child classes.
    /// <remarks>
    /// Uses:
    /// (Octree based quantizer + Dithering) - Ron's code</remarks>
    /// </summary>
    internal abstract class ImageAssembleBase
    {
        private readonly IWebGreaseContext context;

        public ImageAssembleBase(IWebGreaseContext context)
        {
            this.context = context;
        }

        #region Properties

        /// <summary>
        /// Gets Image Format
        /// </summary>
        internal abstract ImageFormat Format
        {
            get;
        }

        /// <summary>
        /// Gets Image Format
        /// </summary>
        internal abstract ImageType Type
        {
            get;
        }

        /// <summary>
        /// Gets default extension for Image type.
        /// Default extension is the first type defined in 
        /// SupportedExtenions property.
        /// </summary>
        internal abstract string DefaultExtension
        {
            get;
        }

        /// <summary>
        /// Gets or sets Assembled file name
        /// </summary>
        internal string AssembleFileName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Assembled Image Packing Type
        /// </summary>
        internal SpritePackingType PackingType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets ImageMap object for Xml Logging
        /// </summary>
        internal ImageMap ImageXmlMap
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Padding value in pixel between images
        /// </summary>
        internal int PaddingBetweenImages
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the command string to execute optimizer Tool exe that
        /// will be used to optimize sprite images. e.g optipng for PNG images
        /// </summary>
        internal string OptimizerToolCommand
        {
            get;
            set;
        }
        #endregion

        #region Public Methods

        /// <summary>Assembles images based on their extension using orientation provided (Auto is default).
        /// Images are combined using Octree based quantizer + dithering algorithm.
        /// Images are also packed in rectangle using Nuclex Rectangle Packer algorithm.</summary>
        /// <param name="inputImages">The input Images.</param>
        internal virtual void Assemble(Dictionary<InputImage, Bitmap> inputImages)
        {
            Bitmap newImage = null;
            try
            {
                // If there is only one image then don't pack images
                if (inputImages.Count > 1)
                {
                    // If Orientation is Auto (which is by default) then get the orientation 
                    // wherein the assembled file size would be minimal.
                    // Else Pack the files according to the value specified.
                    switch (this.PackingType)
                    {
                        case SpritePackingType.Horizontal:
                            newImage = this.PackHorizontal(inputImages, true, null);
                            break;
                        case SpritePackingType.Vertical:
                            newImage = this.PackVertical(inputImages, true, null);
                            break;
                        default:
                            newImage = this.PackVertical(inputImages, true, null);
                            break;
                    }

                    if (newImage != null)
                    {

                        this.SaveImage(newImage);

                        // Add the sprite image size to the imagemap output element.
                        this.ImageXmlMap.UpdateSize(this.AssembleFileName, newImage.Width, newImage.Height);

                        // Hash the saved file using MD5 hashing algorithm
                        this.AssembleFileName = this.HashImage();
                    }
                }
                else if (inputImages.Count == 1)
                {
                    var image = inputImages.Values.First();
                    this.PassThroughImage(image, inputImages.Keys.First());

                    // Add the sprite image size to the imagemap output element.
                    this.ImageXmlMap.UpdateSize(this.AssembleFileName, image.Width, image.Height);

                    // Hash the saved file using MD5 hashing algorithm
                    this.AssembleFileName = this.HashImage();
                }
            }
            catch (OutOfMemoryException ex)
            {
                // If there this type of exception is thrown while Image.FromFile(path)
                // then thrown a custom exception. Ref: http://msdn.microsoft.com/en-us/library/stf701f5.aspx
                var imageException = new ImageAssembleException(ImageAssembleStrings.ImageLoadOutofMemoryExceptionMessage, ex);
                throw imageException;
            }
            catch (Exception)
            {
                // First delete the invalid file, if it exists
                if (File.Exists(this.AssembleFileName))
                {
                     File.Delete(this.AssembleFileName);
                }

                throw;
            }
            finally
            {
                if (newImage != null)
                {
                    newImage.Dispose();
                }
            }
        }

        #endregion

        #region Abstract / Overrideable Methods

        /// <summary>Saves image in a particular format.</summary>
        /// <param name="newImage">Image to be saved</param>
        protected virtual void SaveImage(Bitmap newImage)
        {
            try
            {
                // This try-catch block checks if there is some GDI+ error occured while saving the image. 
                // Incase an exception is thrown, remove the invalid file, if it is generated.
                newImage.Save(this.AssembleFileName, this.Format);
            }
            catch (ExternalException ex)
            {
                // If ExternalException is raised then there is a problem with Image Format or 
                // Image is save to the same file from where it was created. (ref: http://msdn.microsoft.com/en-us/library/9t4syfhh.aspx)
                // This exception is thrown also if access is denied to the path.
                // Handle this condition and throw a custom exception with actual exception as inner exception.
                var imageException = new ImageAssembleException(string.Format(System.Globalization.CultureInfo.CurrentUICulture, ImageAssembleStrings.ImageSaveExternalExceptionMessage, this.AssembleFileName), ex);
                throw imageException;
            }
        }

        /// <summary>Pass through the given the input image as the output image without any image manipulation</summary>
        /// <param name="image">Bitmap for image to pass through</param>
        /// <param name="inputImage">InputImage for image to pass through</param>
        protected virtual void PassThroughImage(Bitmap image, InputImage inputImage)
        {
            this.ImageXmlMap.AppendToXml(inputImage.AbsoluteImagePath, this.AssembleFileName, image.Width, image.Height, 0, 0, "passthrough", true, inputImage.Position);

            if (!File.Exists(this.AssembleFileName))
            {
                File.Copy(inputImage.AbsoluteImagePath, this.AssembleFileName); 
            }
        }

        /// <summary>Runs the optimizer command on the saved image.</summary>
        protected virtual void OptimizeImage()
        {
            // If Optimizer tool command is provided then execute the tool
            if (!string.IsNullOrEmpty(this.OptimizerToolCommand))
            {
                this.OptimizerToolCommand = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + this.OptimizerToolCommand;

                // Execute PNG Optimization tool
                // Find EXE Path and then pass it to ProcessStartInfo
                // Parse arugments and pass it to ProcessStartInfo as well
                // Start Process and wait for it to complete.
                var toolName = ".exe";
                var toolCommand = string.Format(CultureInfo.InvariantCulture, this.OptimizerToolCommand, this.AssembleFileName);
                var pos = toolCommand.IndexOf(toolName, StringComparison.OrdinalIgnoreCase);
                var exePath = toolCommand.Substring(0, pos + toolName.Length + 1);
                Trace.WriteLine("Image Optimization Executable - " + exePath);
                if (!File.Exists(exePath))
                {
                    throw new FileNotFoundException("Could not locate the image optimization executable.", exePath);
                }

                var procStartInfo = new ProcessStartInfo(exePath);
                procStartInfo.CreateNoWindow = true;
                procStartInfo.Arguments = toolCommand.Replace(exePath, string.Empty);
                procStartInfo.UseShellExecute = false;

                // Start the process
                var proc = Process.Start(procStartInfo);

                // Wait for process to complete
                proc.WaitForExit();
            }
        }

        /// <summary>Hashes the Assembled Image using MD5 hash algorithm</summary>
        protected string HashImage()
        {
            var fileInfo = new FileInfo(this.AssembleFileName);

            var newName = this.context.GetFileHash(fileInfo.FullName) + fileInfo.Extension;
        
            var destinationDirectory = fileInfo.DirectoryName;

            // use the first 2 chars for a directory name, the last chars for the file name
            var destinationFilePath = Path.Combine(destinationDirectory, newName.Substring(0, 2));

            // this will be the 2 char subdir, and if it already exists this method does nothing
            Directory.CreateDirectory(destinationFilePath);

            // now get the file 
            destinationFilePath = Path.Combine(destinationFilePath, newName.Remove(0, 2));
            if (!File.Exists(destinationFilePath))
            {
                // Move the file to destination folder
                fileInfo.MoveTo(destinationFilePath);
            }
            else
            {
                // If file already exist then no need to copy again 
                // and remove the original sprite file.
                fileInfo.Delete();
            }
            
            if (!this.ImageXmlMap.UpdateAssembledImageName(this.AssembleFileName, destinationFilePath))
            {
                throw new ImageAssembleException(null, this.AssembleFileName, "Operation failed while replacing assembled image name: '" + this.AssembleFileName + "' with hashed name.");
            }

            return destinationFilePath;
        }

        /// <summary>Packs images in rectangle in Horizontal orientation and makes an entry in Xml Map file.</summary>
        /// <param name="originalBitmaps">Dictionary of image url and its corresponding Bitmap.</param>
        /// <param name="useLogging">Flag to indicate if logging should be done.</param>
        /// <param name="pixelFormat">Nullable PixelFormat.</param>
        /// <returns>Assembled image.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Legacy Code, Fix TODO")]
        protected virtual Bitmap PackHorizontal(Dictionary<InputImage, Bitmap> originalBitmaps, bool useLogging, PixelFormat? pixelFormat)
        {
            var maxHeight = originalBitmaps.Values.Max(c => c.Height);
            var totalWidth = originalBitmaps.Values.Sum(c => c.Width);

            // Add padding
            totalWidth += originalBitmaps.Count * this.PaddingBetweenImages;

            // New bitmap will have width as sum of all widths and heigh as max height
            Bitmap newImage;
            if (pixelFormat != null)
            {
                newImage = new Bitmap(totalWidth, maxHeight, (PixelFormat)pixelFormat);
            }
            else
            {
                newImage = new Bitmap(totalWidth, maxHeight);
            }

            // Sort images descending by Height
            var result = originalBitmaps.OrderByDescending(entry => entry.Value.Height);

            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                var xpoint = 0;
                var addOutputNode = true;

                foreach (var entry in result)
                {
                    var image = entry.Value;
                    graphics.DrawImage(image, new Rectangle(xpoint, 0, image.Width, image.Height));


                    // Log to XmlMap if logging enabled
                    if (useLogging)
                    {
                        this.ImageXmlMap.AppendToXml(entry.Key.AbsoluteImagePath, this.AssembleFileName, image.Width, image.Height, xpoint * -1, 0, null, addOutputNode, entry.Key.Position);
                        addOutputNode = false;
                        foreach (var duplicateImagePath in entry.Key.DuplicateImagePaths)
                        {
                            this.ImageXmlMap.AppendToXml(duplicateImagePath, this.AssembleFileName, image.Width, image.Height, xpoint * -1, 0, "duplicate", addOutputNode, entry.Key.Position);
                        }
                    }

                    xpoint += image.Width + this.PaddingBetweenImages;
                }
            }

            return newImage;
        }

        /// <summary>Packs images in rectangle using the orientation provided and makes an entry in Xml Map file.</summary>
        /// <param name="originalBitmaps">Dictionary of image url and its corresponding Bitmap.</param>
        /// <param name="useLogging">Flag to indicate if logging should be done.</param>
        /// <param name="pixelFormat">Nullable PixelFormat.</param>
        /// <returns>Assembled image.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Legacy Code, Fix TODO")]
        protected virtual Bitmap PackVertical(Dictionary<InputImage, Bitmap> originalBitmaps, bool useLogging, PixelFormat? pixelFormat)
        {
            var maxWidth = originalBitmaps.Values.Max(c => c.Width);
            var totalHeight = originalBitmaps.Values.Sum(c => c.Height);

            // Add padding
            totalHeight += originalBitmaps.Count * this.PaddingBetweenImages;

            // New bitmap will have width as max width and heigh as sum of all heights
            Bitmap spriteImage;
            if (pixelFormat != null)
            {
                spriteImage = new Bitmap(maxWidth, totalHeight, (PixelFormat)pixelFormat);
            }
            else
            {
                spriteImage = new Bitmap(maxWidth, totalHeight);
            }

            // Sort images descending by width
            var result = originalBitmaps.OrderByDescending(entry => entry.Value.Width);

            using (var graphics = Graphics.FromImage(spriteImage))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                var ypoint = 0;
                var addOutputNode = true;

                foreach (var entry in result)
                {
                    // Horizontal position for the image in vertical sprite changes as per market (LTR/RTL) .
                    var xpoint = 0;
                    var currentImage = entry.Value;

                    // When not set explicitly, Image position is Left. This case is possible in two scenarios:
                    // 1. In LTR markets, the image to be sprited is Left aligned in CSS (normal scenario).
                    // 2. In RTL markets, the image to be sprited is Left aligned in CSS.
                    // When any of above occurs, render the image on left side in sprite (horizontal position = 0). 
                    // Image position - Right. This case is possible in two scenarios:
                    // 1. In LTR markets, the image to be sprited is Right aligned in CSS. 
                    // 2. In RTL markets, the image to be sprited is Right aligned in CSS (normal scenario).
                    // When any of above occurs, render the image on right side in sprite (horizontal position = Sprite Image Width - Current Image Width).
                    switch (entry.Key.Position)
                    {
                        case ImagePosition.Right:
                            xpoint = spriteImage.Width - currentImage.Width;
                            break;
                        case ImagePosition.Center:

                            // xpoint will have value using Standard Arithmetic Rounding
                            // i.e. if value is half way between two integers then it will be
                            // used as the next integer value (higher).
                            // e.g 
                            // 3.5 will become 4
                            // 4.5 will become 5
                            xpoint = (spriteImage.Width - currentImage.Width + 1) / 2;
                            break;
                        default:
                            break;
                    }

                    graphics.DrawImage(currentImage, new Rectangle(xpoint, ypoint, currentImage.Width, currentImage.Height));

                    // Log to XmlMap if logging enabled
                    if (useLogging)
                    {
                        this.ImageXmlMap.AppendToXml(entry.Key.AbsoluteImagePath, this.AssembleFileName, currentImage.Width, currentImage.Height, xpoint * -1, ypoint * -1, null, addOutputNode, entry.Key.Position);
                        addOutputNode = false;
                        foreach (var duplicateImagePath in entry.Key.DuplicateImagePaths)
                        {
                            this.ImageXmlMap.AppendToXml(duplicateImagePath, this.AssembleFileName, currentImage.Width, currentImage.Height, xpoint * -1, ypoint * -1, "duplicate", addOutputNode, entry.Key.Position);
                        }
                    }

                    ypoint += currentImage.Height + this.PaddingBetweenImages;
                }
            }

            return spriteImage;
        }

        #endregion
    }
}
