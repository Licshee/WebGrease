﻿namespace ImageAssemble.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WebGrease;
    using WebGrease.Configuration;
    using WebGrease.ImageAssemble;

    /// <summary>
    /// This is a test class for ImageAssembleBaseTest and is intended
    /// to contain all ImageAssembleBaseTest Unit Tests
    /// </summary>
    [TestClass]
    public class ImageAssembleBaseTest
    {

        private const string PositionInSprite = "positioninsprite";

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// A test for Orientation
        /// </summary>
        [TestMethod]
        [TestCategory("ImageAssemble")]
        public void PackingTypeTest()
        {
            ImageAssembleBase target = new NonphotoIndexedAssemble(new WebGreaseContext(new WebGreaseConfiguration()));
            const SpritePackingType expected = SpritePackingType.Vertical;
            SpritePackingType actual = target.PackingType;
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// A test for AssembleFileName
        /// </summary>
        [TestMethod]
        [TestCategory("ImageAssemble")]
        public void AssembleFileNameTest()
        {
            ImageAssembleBase target = new NonphotoIndexedAssemble(new WebGreaseContext(new WebGreaseConfiguration()));
            const string expected = "Combine.png";
            target.AssembleFileName = expected;
            Assert.AreEqual(expected, target.AssembleFileName);
        }


        /// <summary>
        /// A test for PackHorizontal
        /// </summary>
        [TestMethod]
        [TestCategory("ImageAssemble")]
        public void PackAndDrawHorizontalImageTest()
        {
            var target = new NonphotoIndexedAssemble_Accessor(new WebGreaseContext(new WebGreaseConfiguration()));
            var log = new ImageMap_Accessor("ReplaceLog.xml");
            log.AppendPadding("0");
            target.ImageXmlMap = log;
            Bitmap actual = null;
            List<BitmapContainer_Accessor> data = null;
            try
            {
                data = GenerateData(WebGrease.ImageAssemble.ImageType_Accessor.NonphotoIndexed);
                Assert.IsTrue(data.Count > 0);
                actual = target.PackHorizontal(data, true, null);
                int totalWidth = data.Sum(bmp => bmp.Width);
                int maxHeight = data.Max(bmp => bmp.Height);
                Assert.AreEqual(totalWidth, actual.Width);
                Assert.AreEqual(maxHeight, actual.Height);
            }
            finally
            {
                foreach (var entry in data)
                {
                    entry.Bitmap.Dispose();
                }

                if (actual != null)
                {
                    actual.Dispose();
                }
            }
        }

        /// <summary>
        /// A test for PackVertical
        /// </summary>
        [TestMethod]
        [TestCategory("ImageAssemble")]
        public void PackAndDrawVerticalImageTest()
        {
            Bitmap actual = null;
            var log = new ImageMap_Accessor("ReplaceLog.xml");
            var target = new NonphotoIndexedAssemble_Accessor(new WebGreaseContext(new WebGreaseConfiguration())) { AssembleFileName = "combine.png", ImageXmlMap = log };
            List<BitmapContainer_Accessor> data = null;

            try
            {
                data = GenerateData(ImageType_Accessor.NonphotoIndexed);
                Assert.IsTrue(data.Count > 0);
                actual = target.PackVertical(data, true, null);
                var maxWidth = data.Max(bmp => bmp.Width);
                var totalHeight = data.Sum(bmp => bmp.Height);
                Assert.AreEqual(maxWidth, actual.Width);
                Assert.AreEqual(totalHeight, actual.Height);
            }
            finally
            {
                if (data != null)
                {
                    foreach (var entry in data)
                    {
                        entry.Bitmap.Dispose();
                    }
                }

                if (actual != null)
                {
                    actual.Dispose();
                }
            }
        }

        /// <summary>
        /// A test for SaveImage
        /// </summary>
        [TestMethod]
        [STAThread]
        [TestCategory("ImageAssemble")]
        public void OptimizeAndSaveTest_Photo()
        {
            Bitmap originalImage = null;
            try
            {
                var log = new ImageMap_Accessor("ReplaceLog.xml");
                var photoAccessor = new PhotoAssemble_Accessor(new WebGreaseContext(new WebGreaseConfiguration())) { ImageXmlMap = log, AssembleFileName = "Combine.jpg" };
                var jpegData = GenerateData(WebGrease.ImageAssemble.ImageType_Accessor.Photo);
                originalImage = photoAccessor.PackVertical(jpegData, true, null);
                photoAccessor.SaveImage(originalImage);
                Assert.IsTrue(File.Exists(photoAccessor.AssembleFileName));
            }
            finally
            {
                if (originalImage != null)
                {
                    originalImage.Dispose();
                }
            }
        }

        /// <summary>
        /// A test for SaveImage
        /// </summary>
        [TestMethod]
        [STAThread]
        [TestCategory("ImageAssemble")]
        public void OptimizeAndSaveTest_NonphotoNonindexed()
        {
            Bitmap originalImage = null;
            var nonphotoNonindexedAccessor = new NonphotoNonindexedAssemble_Accessor(new WebGreaseContext(new WebGreaseConfiguration()));
            try
            {
                var log = new ImageMap_Accessor("ReplaceLog.xml");
                log.AppendPadding("0");
                nonphotoNonindexedAccessor.ImageXmlMap = log;
                nonphotoNonindexedAccessor.AssembleFileName = "Combine.png";
                var pngData = GenerateData(ImageType_Accessor.NonphotoNonindexed);
                originalImage = nonphotoNonindexedAccessor.PackVertical(pngData, true, null);
                nonphotoNonindexedAccessor.SaveImage(originalImage);
                Assert.IsTrue(File.Exists(nonphotoNonindexedAccessor.AssembleFileName));
            }
            finally
            {
                if (originalImage != null)
                {
                    originalImage.Dispose();
                }
            }
        }

        /// <summary>
        /// A test for SaveImage
        /// </summary>
        [TestMethod]
        [STAThread]
        [TestCategory("ImageAssemble")]
        public void OptimizeAndSaveTest_NonphotoIndexed()
        {
            Bitmap originalImage = null;
            var nonphotoIndexedAccessor = new NonphotoIndexedAssemble_Accessor(new WebGreaseContext(new WebGreaseConfiguration()));
            try
            {
                var log = new ImageMap_Accessor("ReplaceLog.xml");
                log.AppendPadding("0");
                nonphotoIndexedAccessor.ImageXmlMap = log;
                nonphotoIndexedAccessor.AssembleFileName = "Combine.png";
                var gifData = GenerateData(ImageType_Accessor.NonphotoIndexed);
                originalImage = nonphotoIndexedAccessor.PackVertical(gifData, true, null);
                nonphotoIndexedAccessor.SaveImage(originalImage);
                Assert.IsTrue(File.Exists(nonphotoIndexedAccessor.AssembleFileName));
            }
            finally
            {
                if (originalImage != null)
                {
                    originalImage.Dispose();
                }
            }
        }

        /// <summary>
        /// A test for Assemble
        /// </summary>
        [TestMethod]
        [TestCategory("ImageAssemble")]
        public void AssembleTest_NonphotoNonindexed()
        {
            var target = new NonphotoNonindexedAssemble_Accessor(new WebGreaseContext(new WebGreaseConfiguration())) { PackingType = SpritePackingType_Accessor.Horizontal, AssembleFileName = "Combine.png", PaddingBetweenImages = 5 };
            try
            {
                var log = new ImageMap_Accessor("ReplaceLog.xml");
                target.ImageXmlMap = log;
                var inputImages = GenerateData(WebGrease.ImageAssemble.ImageType_Accessor.NonphotoNonindexed);
                target.Assemble(inputImages);
                log.SaveXmlMap();
                Assert.IsTrue(ValidateImageGenerationFromLog("ReplaceLog.xml"));
                ValidateLogFile(inputImages, target.AssembleFileName, target.PackingType);
            }
            finally
            {
                if (File.Exists(target.AssembleFileName))
                {
                    File.Delete(target.AssembleFileName);
                }
            }
        }

        /// <summary>
        /// A test for Assemble
        /// </summary>
        [TestMethod]
        [TestCategory("ImageAssemble")]
        public void AssembleTest_NonphotoIndexed()
        {
            var target = new NonphotoIndexedAssemble_Accessor(new WebGreaseContext(new WebGreaseConfiguration())) { PackingType = SpritePackingType_Accessor.Horizontal, AssembleFileName = "Combine.png", PaddingBetweenImages = 5 };
            try
            {
                var log = new ImageMap_Accessor("ReplaceLog.xml");
                target.ImageXmlMap = log;
                var inputImages = GenerateData(WebGrease.ImageAssemble.ImageType_Accessor.NonphotoIndexed);
                target.Assemble(inputImages);
                log.SaveXmlMap();
                Assert.IsTrue(ValidateImageGenerationFromLog("ReplaceLog.xml"));
                ValidateLogFile(inputImages, target.AssembleFileName, target.PackingType);
            }
            finally
            {
                if (File.Exists(target.AssembleFileName))
                {
                    File.Delete(target.AssembleFileName);
                }
            }
        }

        /// <summary>
        /// A test for Assemble
        /// </summary>
        [TestMethod]
        [TestCategory("ImageAssemble")]
        public void AssembleTest_Photo()
        {
            var target = new PhotoAssemble_Accessor(new WebGreaseContext(new WebGreaseConfiguration())) { PackingType = SpritePackingType_Accessor.Horizontal, AssembleFileName = "Combine.jpg", PaddingBetweenImages = 5 };
            try
            {
                var log = new ImageMap_Accessor("ReplaceLog.xml");
                target.ImageXmlMap = log;
                var inputImages = GenerateData(WebGrease.ImageAssemble.ImageType_Accessor.Photo);
                target.Assemble(inputImages);
                log.SaveXmlMap();
                Assert.IsTrue(ValidateImageGenerationFromLog("ReplaceLog.xml"));
                ValidateLogFile(inputImages, target.AssembleFileName, target.PackingType);
            }
            finally
            {
                if (File.Exists(target.AssembleFileName))
                {
                    File.Delete(target.AssembleFileName);
                }
            }
        }

        #region Private Helper Methods

        private static List<BitmapContainer_Accessor> GenerateData(IEnumerable<ImageType_Accessor> imageTypes)
        {
            var data = new List<BitmapContainer_Accessor>();
            foreach (var images in from imageType in imageTypes let images = Enumerable.Empty<string>() let imagesPath = Path.Combine(Environment.CurrentDirectory, "InputImages", "new", imageType.ToString()) select Directory.GetFiles(imagesPath))
            {
                LoadData(data, images);
            }

            return data;
        }

        private static List<BitmapContainer_Accessor> GenerateData(ImageType_Accessor imageType)
        {
            ImageType_Accessor[] imageTypes = { imageType };
            return GenerateData(imageTypes);
        }

        private static void LoadData(List<BitmapContainer_Accessor> data, IEnumerable<string> files)
        {
            foreach (var path in files)
            {
                var inputImage = new InputImage_Accessor(path);
                var bitmap = (Bitmap)Image.FromFile(path);
                data.Add(new BitmapContainer_Accessor(inputImage) { bitmap = bitmap });
            }
        }

        public static void ValidateLogFile(List<BitmapContainer_Accessor> inputImages, string assembleFileName, SpritePackingType_Accessor packingType)
        {
            Assert.IsTrue(File.Exists("ReplaceLog.xml"));
            var doc = new XmlDocument();
            doc.Load("ReplaceLog.xml");
            Assert.IsNotNull(doc);

            foreach (var entry in inputImages)
            {
                InputImage_Accessor file = entry.InputImage;
                XmlNodeList nodes = doc.SelectNodes("//images/output/input/originalfile[.='" + file.AbsoluteImagePath.ToLowerInvariant() + "']");
                Assert.IsNotNull(nodes);
                Assert.AreEqual(1, nodes.Count, "There should be exactly one row exist for an image file in map Xml log file.");
                XmlNode node = nodes[0];
                Assert.IsNotNull(node);
                XmlNode inputNode = node.ParentNode;
                Assert.IsNotNull(inputNode);
                XmlNode outputNode = inputNode.ParentNode;
                Assert.IsNotNull(outputNode);
                if (outputNode.Attributes != null)
                {
                    string outputFile = outputNode.Attributes["file"].Value;

                    // A blank output file name means the input file was not assembled
                    if (string.IsNullOrEmpty(outputFile))
                    {
                        XmlNode genNode = inputNode.SelectSingleNode("comment");
                        Assert.IsNotNull(genNode);
                        Assert.IsNull(inputNode.SelectSingleNode("width"));
                        Assert.IsNull(inputNode.SelectSingleNode("height"));
                        Assert.IsNull(inputNode.SelectSingleNode("xposition"));
                        Assert.IsNull(inputNode.SelectSingleNode("yposition"));
                    }
                    else
                    {
                        Assert.IsTrue(outputNode.Attributes["file"].Value.Length > 0);
                        using (var bitmap = (Bitmap)Image.FromFile(file.AbsoluteImagePath))
                        {
                            Assert.AreEqual(inputNode.SelectSingleNode("width").InnerText, bitmap.Width.ToString());
                            Assert.AreEqual(inputNode.SelectSingleNode("height").InnerText, bitmap.Height.ToString());
                            Assert.IsTrue(inputNode.SelectSingleNode("xposition").InnerText != string.Empty);
                            Assert.IsTrue(inputNode.SelectSingleNode("yposition").InnerText != string.Empty);

                            if (packingType == SpritePackingType_Accessor.Vertical)
                            {
                                Assert.IsTrue(inputNode.SelectSingleNode(PositionInSprite).InnerText.Equals(file.Position.ToString(), StringComparison.OrdinalIgnoreCase));
                            }
                        }
                    }
                }
            }
        }

        private static bool ValidateImageGenerationFromLog(string logFile)
        {
            if (!File.Exists(logFile))
            {
                return false;
            }
            using (var xmlFile = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // open the image log file and select the file attribute of the output elements
                var xmlLog = XDocument.Load(xmlFile);

                var imagePaths = xmlLog.Root.Elements().Where(e => e.Name == "output").Attributes().Where(a => a.Name == "file").Select(a => a.Value);

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

        #endregion
    }
}
