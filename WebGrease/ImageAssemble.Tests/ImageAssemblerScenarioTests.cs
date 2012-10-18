namespace ImageAssemble.Tests
{
    #region Using directives

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WebGrease.Css.ImageAssemblyAnalysis;
    using WebGrease.ImageAssemble;

    #endregion

    /// <summary>This suite runs user scenario tests</summary>
    [TestClass]
    public class ImageAssembleTaskScenarioTests
    {
        #region Constants

        /// <summary>The generated images folder.</summary>
        private const string GeneratedImagesFolder = @"GeneratedImages\";

        /// <summary>The input image directory name.</summary>
        private const string InputImageDirectoryName = @"InputImages";

        /// <summary>The corrupted image directory name.</summary>
        private const string CorruptedImageDirectoryName = InputImageDirectoryName + @"\CorruptedFiles";

        /// <summary>The valid gif for image generation.</summary>
        private const string ValidGifForImageGeneration = InputImageDirectoryName + @"\11.gif";

        /// <summary>The invalid gif zero bytes length.</summary>
        private const string InvalidGifZeroBytesLength = CorruptedImageDirectoryName + @"\ZeroByteGif.gif";

        /// <summary>The assembled image directory.</summary>
        internal const string AssembledImageDirectory = "AssembledImages";

        /// <summary>The logfile path name.</summary>
        internal const string LogfilePathName = @".\AssembledImages\Logfile.xml";
        #endregion

        #region Accessors

        /// <summary>Gets or sets TestContext.</summary>
        public TestContext TestContext { get; set; }
        #endregion

        #region Test initialization and cleanup

        /// <summary>Ensure the AssembledImages folder exists</summary>
        [TestInitialize]
        public void MyTestInitialize()
        {
            if (!Directory.Exists(AssembledImageDirectory))
            {
                Directory.CreateDirectory(AssembledImageDirectory);
            }
        }

        /// <summary>Ensure the AssembledImages folder is cleaned up</summary>
        [TestCleanup]
        public void MyTestCleanup()
        {
            if (Directory.Exists(AssembledImageDirectory))
            {
                Directory.Delete(AssembledImageDirectory, true);
            }
        }

        #endregion

        #region Image sprite creation tests

        /// <summary>Generates a sprite of the required type from available images of that type with specified padding</summary>
        /// <param name="imageType">Type of image (Png, Jpg, Gif)</param>
        /// <param name="padding">Padding value between 0 - 1024</param>
        /// <param name="packingType">The packing Type.</param>
        private void GenerateSprite(ImageType imageType, string padding, string packingType)
        {
            // Get some image files
            var filePaths = GetImageFilePaths(imageType);

            // Sprite those images
            ImageAssembleTaskScenarioTests.ImageAssembleActivityTest(filePaths, AssembledImageDirectory, packingType, padding);
        }

        /// <summary>Generates a sprite of the required type from available images of that type with specified padding</summary>
        /// <param name="imageType">Type of image (Png, Jpg, Gif)</param>
        /// <param name="padding">Padding value between 0 - 1024</param>
        private void GenerateSprite(ImageType imageType, string padding)
        {
            // Get some image files
            var filePaths = GetImageFilePaths(imageType);

            // Sprite those images
            ImageAssembleTaskScenarioTests.ImageAssembleActivityTest(filePaths, AssembledImageDirectory, "Vertical", padding);
        }

        /// <summary>Generates a sprite of the required type from available images of that type</summary>
        /// <param name="imageType">Type of image (Png, Jpg, Gif)</param>
        private void GenerateSprite(ImageType imageType)
        {
             this.GenerateSprite(imageType, "0");
        }


        /// <summary>The generate images to sprite.</summary>
        /// <param name="imageCount">The image count.</param>
        /// <returns>The generate images to sprite.</returns>
        private static string GenerateImagesToSprite(int imageCount)
        {
            var imagePaths = new StringBuilder();

            if (Directory.Exists(GeneratedImagesFolder))
            {
                Directory.Delete(GeneratedImagesFolder, true);
            }

            Directory.CreateDirectory(GeneratedImagesFolder);

            var dirInfo = new DirectoryInfo(Environment.CurrentDirectory);

            var imagePath = dirInfo.FullName + @"\" + InputImageDirectoryName + @"\11.gif";

            for (var i = 0; i < imageCount; i++)
            {
                var fileName = string.Format("Copy {0} of {1}", i + 1, Path.GetFileName(ValidGifForImageGeneration));
                File.Copy(imagePath, GeneratedImagesFolder + fileName, true);
                imagePaths.Append(Environment.CurrentDirectory + @"\" + GeneratedImagesFolder + fileName + "|L");
                if (i + 1 < imageCount)
                {
                    imagePaths.Append(";");
                }
            }

            return imagePaths.ToString();
        }

        /// <summary>The verify sprited image dimensions.</summary>
        /// <param name="imagePaths">The image paths.</param>
        /// <param name="packingType">The packing type.</param>
        /// <param name="outputImagePath">The output image path.</param>
        /// <param name="paddingInPixels">The padding in pixels.</param>
        /// <returns>The verify sprited image dimensions.</returns>
        private static bool VerifySpritedImageDimensions(string imagePaths, SpritePackingType packingType, string outputImagePath, int paddingInPixels)
        {
            var rc = false;
            var extent = 0;
            var maxOtherDimension = 0;
            var spriteWidth = 0;
            var spriteHeight = 0;

            using (var spriteBitmap = Image.FromFile(outputImagePath) as Bitmap)
            {
                spriteWidth = spriteBitmap.Width;
                spriteHeight = spriteBitmap.Height;

                var imagePathList = imagePaths.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var imagePath in imagePathList)
                {
                    var pathValue = imagePath.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    var imgPath = pathValue[0];
                    using (var originalBitmap = Image.FromFile(imgPath) as Bitmap)
                    {
                        if (packingType == SpritePackingType.Vertical)
                        {
                            extent += originalBitmap.Height;
                            maxOtherDimension = Math.Max(maxOtherDimension, originalBitmap.Width);
                        }
                        else if (packingType == SpritePackingType.Horizontal)
                        {
                            extent += originalBitmap.Width;
                            maxOtherDimension = Math.Max(maxOtherDimension, originalBitmap.Height);
                        }

                        extent += paddingInPixels;
                    }
                }
            }

            if (packingType == SpritePackingType.Vertical)
            {
                rc = (spriteWidth == maxOtherDimension) && (spriteHeight == extent);
            }
            else if (packingType == SpritePackingType.Horizontal)
            {
                rc = (spriteHeight == maxOtherDimension) && (spriteWidth == extent);
            }

            return rc;
        }

        /// <summary>The verify image output.</summary>
        /// <param name="assembledImagePaths">The assembled image paths.</param>
        /// <returns>The verify image output.</returns>
        private static bool VerifyImageOutput(string assembledImagePaths)
        {
            var logFile = Path.Combine(assembledImagePaths, "logfile.xml");
            if (!File.Exists(logFile))
            {
                return false;
            }
            using (var xmlFile = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // open the image log file and select the file attribute of the output elements
                var xmlLog = XDocument.Load(xmlFile);

                var imagePaths = xmlLog.Root.Elements().Where(e => e.Name == "output").Attributes().Where(a => a.Name == "file" && !string.IsNullOrWhiteSpace(a.Value)).Select(a => a.Value);

                if (!imagePaths.Any())
                {
                    return false;
                }

                //foreach of the image files listed, verify the files exist
                if (imagePaths.Any(image => !File.Exists(image)))
                {
                    return false;
                }
            }
            return true;
        }

        private IEnumerable<string> GetImagesFromLog(string logFile)
        {
            if (!File.Exists(logFile))
            {
                return new string[0];
            }

            using (var xmlFile = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // open the image log file and select the file attribute of the output elements
                var xmlLog = XDocument.Load(xmlFile);

                return xmlLog.Root.Elements().Where(e => e.Name == "output").Attributes().Where(a => a.Name == "file" && !string.IsNullOrWhiteSpace(a.Value)).Select(a => a.Value);
            }
        }

        /// <summary>Retrieves the requested number of images of the requested type</summary>
        /// <param name="imageType"></param>
        /// <param name="maxImages"></param>
        /// <param name="position">The position.</param>
        /// <returns>The get image file paths.</returns>
        private static string GetImageFilePaths(ImageType imageType, uint maxImages, ImagePosition position)
        {
            var currentDir = new DirectoryInfo(Environment.CurrentDirectory);
            var imagesDir = new DirectoryInfo(currentDir.FullName + @"\" + InputImageDirectoryName);

            var files = imagesDir.GetFiles("*." + imageType, SearchOption.TopDirectoryOnly);

            var filePathList = new StringBuilder();

            if (files.Length < maxImages)
            {
                throw new InsufficientFilesOfTypeException(
                    maxImages + " files of type " + imageType + " were requested, but there are only " +
                    files.Length + " files of that type available in directory " + imagesDir.FullName);
            }

            if (0 == maxImages)
            {
                maxImages = (uint)files.Length;
            }

            for (var i = 0; i < maxImages; i++)
            {
                var file = files[i];

                if (file.Extension.ToLower().EndsWith(imageType.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    if (position == ImagePosition.Right)
                    {
                        filePathList.Append(file.FullName + "|R");
                    }
                    else
                    {
                        filePathList.Append(file.FullName + "|L");
                    }

                    if (i + 1 < maxImages)
                    {
                        filePathList.Append(";");
                    }
                }
            }

            return filePathList.ToString();
        }

        /// <summary>The get image file paths.</summary>
        /// <param name="imageType">The image type.</param>
        /// <returns>The get image file paths.</returns>
        private static string GetImageFilePaths(ImageType imageType)
        {
            return GetImageFilePaths(imageType, 0, ImagePosition.Left);
        }

        /// <summary>The valid_ vertical gif test.</summary>
        [TestMethod]
        public void Valid_VerticalGifTest()
        {
            this.GenerateSprite(ImageType.Gif, "0", "Vertical");

            // Verify the output
            Assert.IsTrue(VerifyImageOutput(AssembledImageDirectory ));
        }

        /// <summary>The valid_ vertical jpg test.</summary>
        [TestMethod]
        public void Valid_VerticalJpgTest()
        {
            this.GenerateSprite(ImageType.Jpg, "0", "Vertical");

            // Verify the output
            Assert.IsTrue(VerifyImageOutput(AssembledImageDirectory ));
        }

        /// <summary>The valid_ vertical png test.</summary>
        [TestMethod]
        public void Valid_VerticalPngTest()
        {
            this.GenerateSprite(ImageType.Png, "0", "Vertical");

            // Verify the output
            Assert.IsTrue(VerifyImageOutput(AssembledImageDirectory ));
        }


        /// <summary>The valid_ horizontal gif test.</summary>
        [TestMethod]
        public void Valid_HorizontalGifTest()
        {
            this.GenerateSprite(ImageType.Gif, "0", "Horizontal");

            // Verify the output
            Assert.IsTrue(VerifyImageOutput(AssembledImageDirectory));
        }


        /// <summary>The valid_ horizontal jpg test.</summary>
        [TestMethod]
        public void Valid_HorizontalJpgTest()
        {
            this.GenerateSprite(ImageType.Jpg, "0", "Horizontal");

            // Verify the output
            Assert.IsTrue(VerifyImageOutput(AssembledImageDirectory ));
        }


        /// <summary>The valid_ horizontal png test.</summary>
        [TestMethod]
        public void Valid_HorizontalPngTest()
        {
            this.GenerateSprite(ImageType.Png, "0", "Horizontal");

            // Verify the output
            Assert.IsTrue(VerifyImageOutput(AssembledImageDirectory));
        }


        /// <summary>The valid_ padding 20 pixels test.</summary>
        [TestMethod]
        public void Valid_Padding20PixelsTest()
        {
            var imagePaths = GetImageFilePaths(ImageType.Jpg, 10, ImagePosition.Left);

            ImageAssembleTaskTest(
                null,
                imagePaths,
                AssembledImageDirectory,
                LogfilePathName,
                "Vertical",
                "20");

            var assembledImage = GetImagesFromLog(LogfilePathName).Where(file => file.EndsWith(".jpg")).FirstOrDefault();
            Assert.IsNotNull(assembledImage);
            Assert.IsTrue(VerifySpritedImageDimensions(imagePaths, SpritePackingType.Vertical, assembledImage, 20));
        }



        /// <summary>The valid_ mixed images test.</summary>
        [TestMethod]
        public void Valid_MixedImagesTest()
        {
            // Get some jpg files
            var filePaths = GetImageFilePaths(ImageType.Gif, 3, ImagePosition.Left);
            filePaths += ";" + GetImageFilePaths(ImageType.Png, 3, ImagePosition.Left);
            filePaths += ";" + GetImageFilePaths(ImageType.Jpg, 3, ImagePosition.Left);

            // Sprite those images
            ImageAssembleTaskScenarioTests.ImageAssembleActivityTest(filePaths, AssembledImageDirectory, "Vertical", "0");

            // Verify the output
            Assert.IsTrue(VerifyImageOutput(AssembledImageDirectory));
        }


        /// <summary>Verify that two images can be sprited</summary>
        [TestMethod]
        public void Boundary_MinimumImageCountTest2Jpg()
        {
            // Get two jpegs
            var filePaths = GetImageFilePaths(ImageType.Jpg, 2, ImagePosition.Left);

            // Sprite those images
            ImageAssembleTaskScenarioTests.ImageAssembleActivityTest(filePaths, AssembledImageDirectory, "Vertical", "0");

            // Verify the output
            Assert.IsTrue(VerifyImageOutput(AssembledImageDirectory));
        }

        /// <summary>The boundary_ minimum image count test 1 jpg.</summary>
        [TestMethod]
        public void Boundary_MinimumImageCountTest1Jpg()
        {
            // Get one jpeg
            var filePaths = GetImageFilePaths(ImageType.Jpg, 1, ImagePosition.Left);

            // Attempt to sprite the image (still works with one image)
            ImageAssembleTaskScenarioTests.ImageAssembleActivityTest(filePaths, AssembledImageDirectory, "Vertical", "0");

            // Verify the output
            Assert.IsTrue(VerifyImageOutput(AssembledImageDirectory));
        }

        /// <summary>The boundary_ minimum image count test 1 jpg 1 gif.</summary>
        [TestMethod]
        public void Boundary_MinimumImageCountTest1Jpg1Gif()
        {
            // Get two dissimilar files
            var filePaths = GetImageFilePaths(ImageType.Jpg, 1, ImagePosition.Left);
            filePaths += ";" + GetImageFilePaths(ImageType.Gif, 1, ImagePosition.Left);

            // Sprite those images (should still work)
            ImageAssembleTaskScenarioTests.ImageAssembleActivityTest(filePaths, AssembledImageDirectory, "Vertical", "0");

            // Verify the output
            Assert.IsTrue(VerifyImageOutput(AssembledImageDirectory));
        }

        /// <summary>Generates an arbitrarily high number (currently 700) of files, then 
        /// sprites them</summary>
        [TestMethod]
        public void Boundary_MaximumFileCountTest()
        {
            var imagePaths = GenerateImagesToSprite(700);
            ImageAssembleTaskScenarioTests.ImageAssembleActivityTest(imagePaths, AssembledImageDirectory, "Vertical", "0");
            Assert.IsTrue(VerifyImageOutput(AssembledImageDirectory));
        }

        /// <summary>Verifies that the task fails when a an input file is invalid</summary>
        [TestMethod]
        public void Invalid_CorruptedFileZeroBytesTest()
        {
            try
            {
                var file1Path = ValidGifForImageGeneration;
                var file2Path = InvalidGifZeroBytesLength;

                ImageAssembleActivityTest(
                    file1Path + ";" + file2Path,
                    AssembledImageDirectory,
                    "Vertical",
                    "0");

                // No exception was thrown
                Assert.Fail("The expected ImageAssembleException was not thrown.");
            }
            catch (ImageAssembleException)
            {
                // This should happen
            }
            catch (Exception unexpectedException)
            {
                Assert.Fail("The expected ImageAssembleException was not thrown.  Actual exception: " + unexpectedException);
            }
        }

        /// <summary>The invalid_ duplicate image path test.</summary>
        [TestMethod]
        public void Invalid_DuplicateImagePathTest()
        {
            // Get a file
            var filePaths = GetImageFilePaths(ImageType.Gif, 1, ImagePosition.Left);

            filePaths = filePaths + ";" + filePaths;

            // Sprite those images
            try
            {
                ImageAssembleTaskScenarioTests.ImageAssembleActivityTest(filePaths, AssembledImageDirectory, "Vertical", "0");
            }
            catch (ImageAssembleException)
            {
                // This should happen
            }
            catch (Exception unexpectedException)
            {
                Assert.Fail("The expected ImageAssembleException was not thrown.  Actual exception: " + unexpectedException);
            }
        }

        /// <summary>The invalid_ image not assembled test.</summary>
        [TestMethod]
        public void Invalid_ImageNotAssembledTest()
        {
            // Get some jpg files
            var filePaths = GetImageFilePaths(ImageType.Gif, 3, ImagePosition.Left);
            filePaths += ";" + GetImageFilePaths(ImageType.Png, 3, ImagePosition.Left);
            filePaths += ";" + GetImageFilePaths(ImageType.Jpg, 3, ImagePosition.Left);
            filePaths += ";" + GetImageFilePaths(ImageType.Tiff, 2, ImagePosition.Left);
            filePaths += ";" + Path.Combine(Environment.CurrentDirectory, "InputImages\\new\\NotSupported\\CorruptPNG.png") + "|L";

            // Sprite those images
            try
            {
                ImageAssembleTaskScenarioTests.ImageAssembleActivityTest(filePaths, AssembledImageDirectory, "Vertical", "0");
                Assert.Fail("Exception not generated for invalid file type.");
            }
            catch (ImageAssembleException)
            {
                // Verify the output
                Assert.IsTrue(VerifyImageOutput(AssembledImageDirectory));
            }
        }

        /// <summary>The invalid_8 bit ping test.</summary>
        [TestMethod]
        public void Invalid_8BitPingTest()
        {
            var imagePaths = Environment.CurrentDirectory + @"\" + InputImageDirectoryName + @"\8BitPng.png|L";
            var pngPath = imagePaths.Substring(0, imagePaths.Length - 2);
            imagePaths += ";" + Environment.CurrentDirectory + @"\" + InputImageDirectoryName + @"\24BitPng.png|L";
            imagePaths += ";" + Environment.CurrentDirectory + @"\" + InputImageDirectoryName + @"\32BitPng.png|L";

            ImageAssembleTaskScenarioTests.ImageAssembleActivityTest(imagePaths, AssembledImageDirectory, "Vertical", "0");

            var doc = new XmlDocument();
            var logPath = Environment.CurrentDirectory + @"\" + AssembledImageDirectory + @"\Logfile.xml";
            doc.Load(logPath);
            var inputNodes = doc.DocumentElement.SelectNodes(@"//input/originalfile");

            var found = false;

            foreach (XmlNode inputNode in inputNodes)
            {
                if (pngPath.ToLowerInvariant() == inputNode.InnerText.ToLowerInvariant() &&
                    inputNode.NextSibling.InnerText == "8-bit PNGs are not supported for spriting.")
                {
                    found = true;
                    break;
                }
            }

            Assert.IsFalse(found);
        }


        #endregion

        #region Pixel parity tests - original -> sprited image

        /// <summary>The verify pixel parity.</summary>
        /// <param name="xmlLogFilePath">The xml log file path.</param>
        /// <returns>The verify pixel parity.</returns>
        private bool VerifyPixelParity(string xmlLogFilePath)
        {
            var codecInfoList = ImageCodecInfo.GetImageEncoders();
            var extensionsByGuid = new Dictionary<Guid, string>();

            foreach (var codecInfo in codecInfoList)
            {
                extensionsByGuid.Add(codecInfo.FormatID, codecInfo.FormatDescription);
            }


            var doc = new XmlDocument();
            doc.Load(xmlLogFilePath);

            var outputNodes = doc.DocumentElement.SelectNodes(@"output");

            foreach (XmlNode outputNode in outputNodes)
            {
                // Needed pending fix for bug 
                if (!string.IsNullOrEmpty(outputNode.Attributes["file"].Value))
                {
                    // Get the output file
                    using (var spriteBitmap = Image.FromFile(outputNode.Attributes["file"].Value) as Bitmap)
                    {
                        var inputNodes = outputNode.SelectNodes("input");
                        {
                            foreach (XmlNode inputNode in inputNodes)
                            {
                                using (var sourceBitmap = Image.FromFile(inputNode["originalfile"].InnerText) as Bitmap)
                                {
                                    // Compression type and PixelFormat of input may not match that of output, so not much to verify here
                                    var sourceWidth = int.Parse(inputNode["width"].InnerText);
                                    var sourceHeight = int.Parse(inputNode["height"].InnerText);
                                    var xPosition = int.Parse(inputNode["xposition"].InnerText);
                                    var yPosition = int.Parse(inputNode["yposition"].InnerText) * -1;

                                    for (var y = 0; y < sourceHeight; y++)
                                    {
                                        for (var x = 0; x < sourceWidth; x++)
                                        {
                                            var srcPixel = sourceBitmap.GetPixel(x, y);
                                            var dstPixel = spriteBitmap.GetPixel(xPosition, y + yPosition);
                                            if (srcPixel != dstPixel)
                                            {
                                                // Pixels may not match exactly, so this is difficult to verify
                                                // For JPG there is generational loss.
                                                // For nonphoto indexed images, color quantization may cause some loss.
                                                // For nonphoto nonindexed images, colors should match.
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }


            return true;
        }

        /// <summary>The image pixel parity test gif.</summary>
        [TestMethod]
        public void ImagePixelParityTestGif()
        {
            Assert.IsTrue(this.ImagePixelParityTest(ImageType.Gif));
        }

        /// <summary>The image pixel parity test jpg.</summary>
        [TestMethod]
        public void ImagePixelParityTestJpg()
        {
            Assert.IsTrue(this.ImagePixelParityTest(ImageType.Jpg));
        }

        /// <summary>The image pixel parity test png.</summary>
        [TestMethod]
        public void ImagePixelParityTestPng()
        {
            Assert.IsTrue(this.ImagePixelParityTest(ImageType.Png));
        }

        /// <summary>The image pixel parity test.</summary>
        /// <param name="imageType">The image type.</param>
        /// <returns>The image pixel parity test.</returns>
        private bool ImagePixelParityTest(ImageType imageType)
        {
            var imagePaths = GetImageFilePaths(imageType, 10, ImagePosition.Left);

            ImageAssembleTaskTest(
                null,
                imagePaths,
                AssembledImageDirectory,
                LogfilePathName,
                "Vertical",
                "0");

            return this.VerifyPixelParity(LogfilePathName);
        }

        #endregion

        #region Padding tests

        /// <summary>The padding test.</summary>
        /// <param name="packingType">The packing type.</param>
        /// <param name="paddingValue">The padding value.</param>
        /// <returns>The padding test.</returns>
        private bool PaddingTest(SpritePackingType packingType, int paddingValue)
        {
            var rc = false;

            try
            {
                var imagePaths = GetImageFilePaths(ImageType.Jpg, 10, ImagePosition.Left);
                ImageAssembleTaskTest(
                                    null,
                                    imagePaths,
                                    AssembledImageDirectory,
                                    LogfilePathName,
                                    packingType.ToString(),
                                    paddingValue.ToString());

                if (paddingValue >= 0 && paddingValue <= 1024)
                {
                    rc = VerifyImageOutput(AssembledImageDirectory);
                }
            }
            catch (ImageAssembleException)
            {
                if (paddingValue < 0 || paddingValue > 1024)
                {
                    rc = true;
                }
            }

            return rc;
        }

        /// <summary>The boundary_ padding 0 pixel test.</summary>
        [TestMethod]
        public void Boundary_Padding0PixelTest()
        {
            Assert.IsTrue(this.PaddingTest(SpritePackingType.Vertical, 0));
        }

        /// <summary>The boundary_ padding 1 pixel test.</summary>
        [TestMethod]
        public void Boundary_Padding1PixelTest()
        {
            Assert.IsTrue(this.PaddingTest(SpritePackingType.Vertical, 1));
        }

        /// <summary>The boundary_ padding 99 pixel test.</summary>
        [TestMethod]
        public void Boundary_Padding99PixelTest()
        {
            Assert.IsTrue(this.PaddingTest(SpritePackingType.Vertical, 99));
        }

        /// <summary>The boundary_ padding 100 pixels test.</summary>
        [TestMethod]
        public void Boundary_Padding100PixelsTest()
        {
            Assert.IsTrue(this.PaddingTest(SpritePackingType.Vertical, 1024));
        }

        /// <summary>The invalid_ negative padding value test.</summary>
        [TestMethod]
        public void Invalid_NegativePaddingValueTest()
        {
            Assert.IsTrue(this.PaddingTest(SpritePackingType.Vertical, -1));
        }

        /// <summary>The invalid_ too large padding value test.</summary>
        [TestMethod]
        public void Invalid_TooLargePaddingValueTest()
        {
            Assert.IsTrue(this.PaddingTest(SpritePackingType.Vertical, 101));
        }


        #endregion

        #region Image path tests

        /// <summary>Verifies that the task fails when an empty string is given for image paths</summary>
        [TestMethod]
        public void Invalid_NoImagePathsTest()
        {
            try
            {
                ImageAssembleActivityTest(
                    string.Empty,
                    AssembledImageDirectory,
                    "Vertical",
                    "0");

                // No exception was thrown
                Assert.Fail("The expected ImageAssembleException was not thrown.");
            }
            catch (ImageAssembleException)
            {
                // This should happen
            }
            catch (Exception)
            {
                // This is some other type of exception
                Assert.Fail("The expected ImageAssembleException was not thrown.");
            }
        }

        /// <summary>Verifies that the task fails when a non-existent directory for images is specified</summary>
        [TestMethod]
        public void Invalid_NonExistentImageDirectoryTest()
        {
            try
            {
                ImageAssembleActivityTest(
                    @"c:\somefolder\somesubfolder\images",
                    AssembledImageDirectory,
                    "0");

                // No exception was thrown
                Assert.Fail("The expected ImageAssembleException was not thrown.");
            }
            catch (ImageAssembleException)
            {
                // This should happen
            }
            catch (Exception)
            {
                // This is some other type of exception
                Assert.Fail("The expected ImageAssembleException was not thrown.");
            }
        }

        /// <summary>Verifies that the task fails when a non-existent directory for images is specified</summary>
        [TestMethod]
        public void Invalid_EmptyImageDirectoryTest()
        {
            try
            {
                Directory.CreateDirectory(@"EmptyImageDirectory");

                ImageAssembleActivityTest(
                    @"EmptyImageDirectory",
                    AssembledImageDirectory,
                    "0");

                // No exception was thrown
                Assert.Fail("The expected ImageAssembleException was not thrown.");
            }
            catch (ImageAssembleException)
            {
                // This should happen
            }
            catch (Exception ex)
            {
                // This is some other type of exception
                Assert.Fail("The expected ImageAssembleException was not thrown. Instead the error was :" + ex.Message);
            }
            finally
            {
                Directory.Delete(@"EmptyImageDirectory");
            }
        }

        /// <summary>Verifies that the task fails when a non-existent directory for output is specified</summary>
        [TestMethod]
        public void Invalid_NonExistentOutputDirectoryTest()
        {
            try
            {
                ImageAssembleActivityTest(
                    @"\" + InputImageDirectoryName,
                    @"SomeDirectoryThatDoesNotExist",
                    "0");

                // No exception was thrown
                Assert.Fail("The expected ImageAssembleException was not thrown.");
            }
            catch (ImageAssembleException)
            {
                // This should happen
            }
            catch (Exception unexpectedException)
            {
                Assert.Fail("The expected ImageAssembleException was not thrown.  Actual exception: " + unexpectedException);
            }
        }


        /// <summary>Verifies that the task fails when a read-only directory for output is specified</summary>
        [TestMethod]
        public void Invalid_ReadOnlyOutputDirectoryTest()
        {
            const string ReadOnlyOutputDirectory = @"ReadOnlyOutputDirectory";
            DirectoryInfo dirInfo = null;

            try
            {
                dirInfo = Directory.CreateDirectory(ReadOnlyOutputDirectory);
                dirInfo.Attributes |= FileAttributes.ReadOnly;

                ImageAssembleActivityTest(
                    InputImageDirectoryName,
                    ReadOnlyOutputDirectory,
                    "0");

                // No exception was thrown
                Assert.Fail("The expected ImageAssembleException was not thrown.");
            }
            catch (IOException)
            {
                // This should happen
            }
            catch (Exception unexpectedException)
            {
                Assert.Fail("The expected ImageAssembleException was not thrown.  Actual exception: " + unexpectedException);
            }
            finally
            {
                dirInfo.Attributes = FileAttributes.Normal;
                Directory.Delete(ReadOnlyOutputDirectory);
            }
        }

        #endregion

        #region ImageAssembleTaskTest overloads

        /// <summary>The image assemble task test.</summary>
        /// <param name="inputDirectory">The input directory.</param>
        /// <param name="inputFilePaths">The input file paths.</param>
        /// <param name="outputDirectory">The output directory.</param>
        /// <param name="logfilePath">The logfile path.</param>
        /// <param name="packingScheme">The packing scheme.</param>
        /// <param name="padding">The padding.</param>
        /// <returns>The image assemble task test.</returns>
        private static void ImageAssembleTaskTest(
            string inputDirectory,
            string inputFilePaths,
            string outputDirectory,
            string logfilePath,
            string packingScheme,
            string padding)
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
            Directory.CreateDirectory(outputDirectory);
            var imageAssembleActivity = new ImageAssembler_Accessor
                                            {
                                                InputDirectory = inputDirectory,
                                                InputFilePaths = inputFilePaths,
                                                LogFile = logfilePath,
                                                OutputDirectory = outputDirectory,
                                                PackingScheme = packingScheme,
                                                Padding = padding,
                                                ShouldThrowException = true
                                            };

            imageAssembleActivity.Execute();
        }

        /// <summary>The image assemble activity test.</summary>
        /// <param name="inputDirectory">The input directory.</param>
        /// <returns>The image assemble activity test.</returns>
        private static void ImageAssembleActivityTest(string inputDirectory)
        {
             ImageAssembleTaskScenarioTests.ImageAssembleTaskTest(
                inputDirectory,
                null,
                AssembledImageDirectory,
                LogfilePathName,
                "Vertical",
                "0");
        }

        /// <summary>The image assemble activity test.</summary>
        /// <param name="inputDirectory">The input directory.</param>
        /// <param name="outputDirectory">The output directory.</param>
        /// <returns>The image assemble activity test.</returns>
        private static void ImageAssembleActivityTest(string inputDirectory, string outputDirectory)
        {
             ImageAssembleTaskScenarioTests.ImageAssembleTaskTest(
                inputDirectory,
                null,
                outputDirectory,
                LogfilePathName,
                "Vertical",
                "0");
        }


        /// <summary>The image assemble activity test.</summary>
        /// <param name="inputDirectory">The input directory.</param>
        /// <param name="outputDirectory">The output directory.</param>
        /// <param name="padding">The padding.</param>
        /// <returns>The image assemble activity test.</returns>
        private static void ImageAssembleActivityTest(string inputDirectory, string outputDirectory, string padding)
        {
             ImageAssembleTaskScenarioTests.ImageAssembleTaskTest(
                inputDirectory,
                null,
                outputDirectory,
                LogfilePathName,
                "Vertical",
                padding);
        }


        /// <summary>The image assemble activity test.</summary>
        /// <param name="inputPaths">The input paths.</param>
        /// <param name="outputDirectory">The output directory.</param>
        /// <param name="packingScheme">The packing scheme.</param>
        /// <param name="padding">The padding.</param>
        /// <returns>The image assemble activity test.</returns>
        private static void ImageAssembleActivityTest(string inputPaths, string outputDirectory, string packingScheme, string padding)
        {
             ImageAssembleTaskScenarioTests.ImageAssembleTaskTest(
                null,
                inputPaths,
                outputDirectory,
                LogfilePathName,
                packingScheme,
                padding);
        }

        #endregion
    }

    #region Enumerations

    /// <summary>The image type.</summary>
    public enum ImageType
    {
        /// <summary>The png.</summary>
        Png,

        /// <summary>The gif.</summary>
        Gif,

        /// <summary>The jpg.</summary>
        Jpg,

        /// <summary>The tiff.</summary>
        Tiff,

        /// <summary>The bmp.</summary>
        Bmp,
    }
    #endregion

    #region Custom exceptions

    /// <summary>The insufficient files of type exception.</summary>
    public class InsufficientFilesOfTypeException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="InsufficientFilesOfTypeException"/> class.</summary>
        /// <param name="message">The message.</param>
        public InsufficientFilesOfTypeException(string message) : base(message) { }
    }
    #endregion

}
