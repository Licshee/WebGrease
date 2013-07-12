namespace ImageAssemble.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WebGrease;
    using WebGrease.Configuration;
    using WebGrease.ImageAssemble;

    /// <summary>This is a test class for ImageAssembleGeneratorTest and is intended
    /// to contain all ImageAssembleGeneratorTest Unit Tests</summary>
    [TestClass]
    public class ImageAssembleGeneratorTest
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for RegisterImageAssemblers</summary>
        [TestMethod]
        [TestCategory("ImageAssemble")]
        public void RegisterImageAssemblersTest()
        {
            foreach (var entry in ImageAssembleGenerator_Accessor.RegisterAvailableAssemblers(new WebGreaseContext(new WebGreaseConfiguration())))
            {
                switch (entry.DefaultExtension)
                {
                    case ".JPG":
                        Assert.IsTrue(entry.Format == ImageFormat.Jpeg);
                        break;
                    case ".GIF":
                        Assert.IsTrue(entry.Format == ImageFormat.Gif);
                        break;
                    case ".PNG":
                        Assert.IsTrue(entry.Format == ImageFormat.Png);
                        break;
                }
            }
        }

        /// <summary>A test for HasAlpha</summary>
        [TestMethod]
        [TestCategory("ImageAssemble")]
        public void HasAlphaTest()
        {
            Bitmap bitmap = null;
            TestCase[] testCases = 
            { 
                new TestCase("InputImages\\new\\NonphotoNonindexed\\Nonindexed32bppAlphaPNG.png", true), 
                new TestCase("InputImages\\new\\NonphotoNonindexed\\Nonindexed24bppPNG.png", false)
            };
            foreach (var testCase in testCases)
            {
                try
                {
                    bitmap = (Bitmap)Image.FromFile(Path.Combine(Environment.CurrentDirectory, testCase.FileName));
                    Assert.AreEqual(testCase.ExpectedResult, ImageAssembleGenerator_Accessor.HasAlpha(bitmap));
                }
                finally
                {
                    if (bitmap != null)
                    {
                        bitmap.Dispose();
                    }
                }
            }
        }

        /// <summary>A test for IsIndexable</summary>
        [TestMethod]
        [TestCategory("ImageAssemble")]
        public void IsIndexableTest()
        {
            Bitmap bitmap = null;
            TestCase[] testCases = 
            { 
                new TestCase("InputImages\\new\\NonphotoIndexed\\Indexable24bppPNG.png", true), 
                new TestCase("InputImages\\new\\NonphotoIndexed\\Indexable32bppPNG.png", true), 
                new TestCase("InputImages\\new\\NonphotoIndexed\\Indexable32bppPNG256Pixels.png", true), 
                new TestCase("InputImages\\new\\NonphotoIndexed\\Indexed8bppGIF.gif", true), 
                new TestCase("InputImages\\new\\NonphotoIndexed\\Indexed2bppPNG.png", true), 
                new TestCase("InputImages\\new\\NonphotoNonindexed\\Nonindexed32bppAlphaPNG.png", false), 
                new TestCase("InputImages\\new\\NonphotoNonindexed\\Nonindexed24bppPNG.png", false)
            };
            foreach (var testCase in testCases)
            {
                try
                {
                    bitmap = (Bitmap)Image.FromFile(Path.Combine(Environment.CurrentDirectory, testCase.FileName));
                    Assert.AreEqual(testCase.ExpectedResult, ImageAssembleGenerator_Accessor.IsIndexable(bitmap));
                }
                finally
                {
                    if (bitmap != null)
                    {
                        bitmap.Dispose();
                    }
                }
            }
        }

        /// <summary>A test for IsIndexed</summary>
        [TestMethod]
        [TestCategory("ImageAssemble")]
        public void IsIndexedTest()
        {
            Bitmap bitmap = null;
            TestCase[] testCases = 
            { 
                new TestCase("InputImages\\new\\NonphotoIndexed\\Indexed8bppGIF.gif", true), 
                new TestCase("InputImages\\new\\NonphotoIndexed\\Indexable24bppPNG.png", false), 
                new TestCase("InputImages\\new\\NonphotoIndexed\\Indexable32bppPNG.png", false), 
                new TestCase("InputImages\\new\\NonphotoIndexed\\Indexable32bppPNG256Pixels.png", false), 
                new TestCase("InputImages\\new\\NonphotoNonindexed\\Nonindexed32bppAlphaPNG.png", false), 
                new TestCase("InputImages\\new\\NonphotoNonindexed\\Nonindexed24bppPNG.png", false)
            };
            foreach (var testCase in testCases)
            {
                try
                {
                    bitmap = (Bitmap)Image.FromFile(Path.Combine(Environment.CurrentDirectory, testCase.FileName));
                    Assert.AreEqual(testCase.ExpectedResult, ImageAssembleGenerator_Accessor.IsIndexed(bitmap));
                }
                finally
                {
                    if (bitmap != null)
                    {
                        bitmap.Dispose();
                    }
                }
            }
        }

        /// <summary>A test for IsMultiframe</summary>
        [TestMethod]
        [TestCategory("ImageAssemble")]
        public void IsMultiframeTest()
        {
            Bitmap bitmap = null;
            TestCase[] testCases = 
            { 
                new TestCase("InputImages\\new\\NotSupported\\AnimatedGIF.gif", true), 
                new TestCase("InputImages\\new\\NotSupported\\MultiFrameTIF.tif", true), 
                new TestCase("InputImages\\new\\NonphotoIndexed\\Indexed8bppGIF.gif", false), 
                new TestCase("InputImages\\new\\NonphotoIndexed\\Indexed2bppPNG.png", false)
            };
            foreach (var testCase in testCases)
            {
                try
                {
                    bitmap = (Bitmap)Image.FromFile(Path.Combine(Environment.CurrentDirectory, testCase.FileName));
                    Assert.AreEqual(testCase.ExpectedResult, ImageAssembleGenerator_Accessor.IsMultiframe(bitmap));
                }
                finally
                {
                    if (bitmap != null)
                    {
                        bitmap.Dispose();
                    }
                }
            }
        }

        /// <summary>A test for IsPhoto</summary>
        [TestMethod]
        [TestCategory("ImageAssemble")]
        public void IsPhotoTest()
        {
            Bitmap bitmap = null;
            TestCase[] testCases = 
            { 
                new TestCase("InputImages\\new\\Photo\\Photo.jpg", true), 
                new TestCase("InputImages\\new\\Photo\\Grayscale8bppJPG.jpg", true), 
                new TestCase("InputImages\\new\\NonphotoNonindexed\\Nonindexed24bppPNG.png", false), 
                new TestCase("InputImages\\new\\NonphotoIndexed\\Indexed8bppGIF.gif", false)
            };
            foreach (var testCase in testCases)
            {
                try
                {
                    bitmap = (Bitmap)Image.FromFile(Path.Combine(Environment.CurrentDirectory, testCase.FileName));
                    Assert.AreEqual(testCase.ExpectedResult, ImageAssembleGenerator_Accessor.IsPhoto(bitmap));
                }
                finally
                {
                    if (bitmap != null)
                    {
                        bitmap.Dispose();
                    }
                }
            }
        }

        /// <summary>A test for SeparateByImageType</summary>
        [TestMethod]
        [TestCategory("ImageAssemble")]
        public void SeparateByImageTypeTest()
        {
            string[] fileNames =
            {
                "InputImages\\new\\NotSupported\\AnimatedGIF.GIF", 
                "InputImages\\new\\NotSupported\\MultiFrameTIF.TIF", 
                "InputImages\\new\\NotSupported\\MisnamedTextFile.png", 
                "InputImages\\new\\NotSupported\\CorruptPNG.png", 

                "InputImages\\new\\Photo\\Grayscale8bppJPG.jpg", 
                "InputImages\\new\\Photo\\MisnamedJPG", 
                "InputImages\\new\\Photo\\MisnamedJPG.GIF", 
                "InputImages\\new\\Photo\\MisnamedJPG.PNG", 
                "InputImages\\new\\Photo\\Photo.JPG", 

                "InputImages\\new\\NonphotoNonindexed\\Nonindexed24bppBMP.BMP", 
                "InputImages\\new\\NonphotoNonindexed\\Nonindexed32bppAlphaPNG.png", 
                "InputImages\\new\\NonphotoNonindexed\\Nonindexed32bppPNG.png", 

                "InputImages\\new\\NonphotoIndexed\\Indexable24bppPNG.png", 
                "InputImages\\new\\NonphotoIndexed\\Indexable32bppPNG.png", 
                "InputImages\\new\\NonphotoIndexed\\Indexable32bppPNG256Pixels.png", 
                "InputImages\\new\\NonphotoIndexed\\Indexed1bppBMP.bmp", 
                "InputImages\\new\\NonphotoIndexed\\Indexed2bppPNG.PNG", 
                "InputImages\\new\\NonphotoIndexed\\Indexed4bppBMP.bmp", 
                "InputImages\\new\\NonphotoIndexed\\Indexed4bppPNG.PNG", 
                "InputImages\\new\\NonphotoIndexed\\Indexed8bppBMP.bmp", 
                "InputImages\\new\\NonphotoIndexed\\Indexed8bppGIF.GIF", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedBMP.JPG", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedGIF.EXE", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedGIF.JPG", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedGIF.JS", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedGIF.PNG", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedPNG.BMP", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedPNG.GIF", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedPNG.JPG", 
                "InputImages\\new\\NonphotoIndexed\\SingleFrame.tif"
            };
            string[] expectedFileNamesNotSupported =
            {
                "InputImages\\new\\NotSupported\\AnimatedGIF.GIF", 
                "InputImages\\new\\NotSupported\\MultiFrameTIF.TIF", 
                "InputImages\\new\\NotSupported\\MisnamedTextFile.png", 
                "InputImages\\new\\NotSupported\\CorruptPNG.png"
            };
            string[] expectedFileNamesPhoto =
            {
                "InputImages\\new\\Photo\\Grayscale8bppJPG.jpg", 
                "InputImages\\new\\Photo\\MisnamedJPG", 
                "InputImages\\new\\Photo\\MisnamedJPG.GIF", 
                "InputImages\\new\\Photo\\MisnamedJPG.PNG", 
                "InputImages\\new\\Photo\\Photo.JPG"
            };
            string[] expectedFileNamesNonphotoNonindexed =
            {
                "InputImages\\new\\NonphotoNonindexed\\Nonindexed24bppBMP.BMP", 
                "InputImages\\new\\NonphotoNonindexed\\Nonindexed32bppAlphaPNG.png", 
                "InputImages\\new\\NonphotoNonindexed\\Nonindexed32bppPNG.png"
            };
            string[] expectedFileNamesNonphotoIndexed =
            {
                "InputImages\\new\\NonphotoIndexed\\Indexable24bppPNG.png", 
                "InputImages\\new\\NonphotoIndexed\\Indexable32bppPNG.png", 
                "InputImages\\new\\NonphotoIndexed\\Indexable32bppPNG256Pixels.png", 
                "InputImages\\new\\NonphotoIndexed\\Indexed1bppBMP.bmp", 
                "InputImages\\new\\NonphotoIndexed\\Indexed2bppPNG.PNG", 
                "InputImages\\new\\NonphotoIndexed\\Indexed4bppBMP.bmp", 
                "InputImages\\new\\NonphotoIndexed\\Indexed4bppPNG.PNG", 
                "InputImages\\new\\NonphotoIndexed\\Indexed8bppBMP.bmp", 
                "InputImages\\new\\NonphotoIndexed\\Indexed8bppGIF.GIF", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedBMP.JPG", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedGIF.EXE", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedGIF.JPG", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedGIF.JS", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedGIF.PNG", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedPNG.BMP", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedPNG.GIF", 
                "InputImages\\new\\NonphotoIndexed\\MisnamedPNG.JPG", 
                "InputImages\\new\\NonphotoIndexed\\SingleFrame.tif"
            };

            AppendCurrentDirectory(expectedFileNamesNotSupported);
            AppendCurrentDirectory(expectedFileNamesPhoto);
            AppendCurrentDirectory(expectedFileNamesNonphotoNonindexed);
            AppendCurrentDirectory(expectedFileNamesNonphotoIndexed);

            var expectedFileNames = new Dictionary<WebGrease.ImageAssemble.ImageType, string[]>();
            expectedFileNames.Add(WebGrease.ImageAssemble.ImageType.NotSupported, expectedFileNamesNotSupported);
            expectedFileNames.Add(WebGrease.ImageAssemble.ImageType.Photo, expectedFileNamesPhoto);
            expectedFileNames.Add(WebGrease.ImageAssemble.ImageType.NonphotoNonindexed, expectedFileNamesNonphotoNonindexed);
            expectedFileNames.Add(WebGrease.ImageAssemble.ImageType.NonphotoIndexed, expectedFileNamesNonphotoIndexed);

            var inputImagesList = new List<InputImage>();
            foreach (var fileName in fileNames)
            {
                inputImagesList.Add(new InputImage(Path.Combine(Environment.CurrentDirectory, fileName)));
            }

            var separatedLists = ImageAssembleGenerator.SeparateByImageType(inputImagesList.AsReadOnly());
            foreach (ImageType imageType in System.Enum.GetValues(typeof(WebGrease.ImageAssemble.ImageType)))
            {
                var separatedList = separatedLists[imageType];
                CompareLists(separatedList, expectedFileNames[imageType]);
                if (separatedList != null)
                {
                    foreach (var entry in separatedList)
                    {
                        if (entry.Bitmap != null)
                        {
                            entry.Bitmap.Dispose();
                        }
                    }
                }
            }
        }

        /// <summary>A test for AssembleImages</summary>
        [TestMethod]
        [TestCategory("ImageAssemble")]
        public void AssembleImagesTest1()
        {
            try
            {
                var imagePaths = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "InputImages\\new\\NonphotoIndexed")).ToList().AsReadOnly();
                var packingType = SpritePackingType_Accessor.Vertical;
                const string mapFileName = "ReplaceLog.xml";
                var inputImageList = ArgumentParser.ConvertToInputImageList(imagePaths.ToArray());
                ImageAssembleGenerator_Accessor.AssembleImages(inputImageList.AsReadOnly(), SpritePackingType_Accessor.Vertical, string.Empty, mapFileName, false, new WebGreaseContext(new WebGreaseConfiguration()));
                Assert.IsTrue(ValidateImageGenerationFromLog(mapFileName));
                var separatedList = new List<BitmapContainer_Accessor>();
                foreach(var inputImage in inputImageList)
                {
                    Bitmap bitmap = null;
                    try
                    {
                        bitmap = (Bitmap)Image.FromFile(inputImage.AbsoluteImagePath);
                    }
                    catch
                    {
                        bitmap = null;
                    }

                    separatedList.Add(new BitmapContainer_Accessor(inputImage) { Bitmap = bitmap });
                }

                ImageAssembleBaseTest.ValidateLogFile(separatedList, "combine.png", packingType);
            }
            finally
            {
                var filepath = Path.Combine(Environment.CurrentDirectory, "Combine.png");
                var logpath = Path.Combine(Environment.CurrentDirectory, "ReplaceLog.xml");
                File.Delete(filepath);
                File.Delete(logpath);
            }
        }

        /// <summary>The append current directory.</summary>
        /// <param name="filePaths">The file paths.</param>
        private static void AppendCurrentDirectory(string[] filePaths)
        {
            for (var i = 0; i < filePaths.Length; i++)
            {
                filePaths[i] = Path.Combine(Environment.CurrentDirectory, filePaths[i]);
            }
        }

        /// <summary>The compare lists.</summary>
        /// <param name="separatedList">The separated list.</param>
        /// <param name="expectedFileNames">The expected file names.</param>
        private static void CompareLists(List<BitmapContainer> separatedList, string[] expectedFileNames)
        {
            Assert.AreEqual(separatedList.Count, expectedFileNames.Length);

            var expectedFileNameList = expectedFileNames.ToList();
            var fileNameList = separatedList.Select(entry => entry.InputImage.AbsoluteImagePath).ToList();

            fileNameList.Sort();
            expectedFileNameList.Sort();
            for (var i = 0; i < fileNameList.Count; i++)
            {
                Assert.AreEqual(fileNameList[i], expectedFileNameList[i]);
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
    }

    /// <summary>The test case.</summary>
    public class TestCase
    {
        /// <summary>Initializes a new instance of the <see cref="TestCase"/> class.</summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="expectedResult">The expected result.</param>
        public TestCase(string fileName, bool expectedResult)
        {
            this.FileName = fileName;
            this.ExpectedResult = expectedResult;
        }

        /// <summary>
        /// Gets or sets file name for test case
        /// </summary>
        public string FileName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets file name for test case
        /// </summary>
        public bool ExpectedResult
        {
            get;
            set;
        }
    }

}
