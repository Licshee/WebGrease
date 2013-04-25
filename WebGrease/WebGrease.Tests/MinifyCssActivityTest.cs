// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MinifyCssActivityTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for MinifyCssActivityTest and is intended
//   to contain all MinifyCssActivityTest Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Xml.Linq;
    using System.Linq;
    using Activities;
    using Css.ImageAssemblyAnalysis;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WebGrease.Configuration;
    using WebGrease.Css;

    /// <summary>This is a test class for MinifyCssActivityTest and is intended
    /// to contain all MinifyCssActivityTest Unit Tests</summary>
    [TestClass]
    public class MinifyCssActivityTest
    {
        /// <summary>The black listed selectors.</summary>
        private static readonly HashSet<string> BlackListedSelectors = new HashSet<string> { "html>body", "* html", "*:first-child+html p", "head:first-child+body", "head+body", "body>", "*>html", "*html>body" };

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for Css pipeline for property exclusion.</summary>
        [TestMethod]
        public void CssExcludePropertiesTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\MinifyCssActivityTest");
            var minifyCssActivity = new MinifyCssActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            minifyCssActivity.SourceFile = Path.Combine(sourceDirectory, @"Input\Case1\ExcludeByKeys.css");
            minifyCssActivity.DestinationFile = Path.Combine(sourceDirectory, @"Output\Case1\ExcludeByKeys.css");
            minifyCssActivity.ShouldExcludeProperties = true;
            minifyCssActivity.ShouldAssembleBackgroundImages = false;
            minifyCssActivity.Execute();

            // Assertions
            var outputFilePath = minifyCssActivity.DestinationFile;
            Assert.IsTrue(File.Exists(outputFilePath));
            var text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(text));
            Assert.IsTrue(!text.Contains("Exclude"));
        }

        /// <summary>A test for Css pipeline for lower case validation.</summary>
        [TestMethod]
        public void CssLowerCaseValidationTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\MinifyCssActivityTest");
            var minifyCssActivity = new MinifyCssActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            minifyCssActivity.SourceFile = Path.Combine(sourceDirectory, @"Input\Case2\LowerCaseValidation.css");
            minifyCssActivity.DestinationFile = Path.Combine(sourceDirectory, @"Output\Case2\LowerCaseValidation.css");
            minifyCssActivity.ShouldValidateForLowerCase = true;
            minifyCssActivity.ShouldAssembleBackgroundImages = false;
            minifyCssActivity.Execute();

            // Assertions
            var outputFilePath = minifyCssActivity.DestinationFile;
            Assert.IsTrue(File.Exists(outputFilePath));
            var text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(text));
        }

        /// <summary>A test for Css pipeline for hack selectors.</summary>
        [TestMethod]
        public void CssHackSelectorsTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\MinifyCssActivityTest");
            var minifyCssActivity = new MinifyCssActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            minifyCssActivity.SourceFile = Path.Combine(sourceDirectory, @"Input\Case3\HackValidation.css");
            minifyCssActivity.DestinationFile = Path.Combine(sourceDirectory, @"Output\Case3\HackValidation.css");
            minifyCssActivity.ShouldAssembleBackgroundImages = false;
            foreach (var hack in BlackListedSelectors)
            {
                minifyCssActivity.HackSelectors.Add(hack);
            }

            Exception exception = null;
            try
            {
                minifyCssActivity.Execute();
            }
            catch (WorkflowException workflowException)
            {
                exception = workflowException;
            }

            // Assertions
            Assert.IsNotNull(exception);
        }

        /// <summary>A test for Css pipeline for banned selectors.</summary>
        [TestMethod]
        public void CssBannedSelectorsTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\MinifyCssActivityTest");
            var minifyCssActivity = new MinifyCssActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            minifyCssActivity.SourceFile = Path.Combine(sourceDirectory, @"Input\Case4\HackValidation.css");
            minifyCssActivity.DestinationFile = Path.Combine(sourceDirectory, @"Output\Case4\HackValidation.css");
            foreach (var hack in BlackListedSelectors)
            {
                minifyCssActivity.BannedSelectors.Add(hack);
            }

            minifyCssActivity.ShouldAssembleBackgroundImages = false;
            minifyCssActivity.Execute();

            // Assertions
            var outputFilePath = minifyCssActivity.DestinationFile;
            Assert.IsTrue(File.Exists(outputFilePath));
            var text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(string.IsNullOrWhiteSpace(text));
        }

        /// <summary>A test for Css optimization.</summary>
        [TestMethod]
        public void CssOptimizationTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\MinifyCssActivityTest");
            var minifyCssActivity = new MinifyCssActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            minifyCssActivity.SourceFile = Path.Combine(sourceDirectory, @"Input\Case5\OptimizationTest.css");
            minifyCssActivity.DestinationFile = Path.Combine(sourceDirectory, @"Output\Case5\OptimizationTest.css");
            minifyCssActivity.ShouldOptimize = true;
            minifyCssActivity.ShouldAssembleBackgroundImages = false;
            minifyCssActivity.Execute();

            // Assertions
            var outputFilePath = minifyCssActivity.DestinationFile;
            Assert.IsTrue(File.Exists(outputFilePath));
            var text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(text));
            Assert.IsTrue(!text.Contains("#foo"));
        }

        /// <summary>A test for Css sprite.</summary>
        [TestMethod]
        public void CssImageSpriteTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\MinifyCssActivityTest");
            var minifyCssActivity = new MinifyCssActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            minifyCssActivity.SourceFile = Path.Combine(sourceDirectory, @"Input\Case6\SpriteTest.css");
            minifyCssActivity.ImageAssembleScanDestinationFile = Path.Combine(sourceDirectory, @"Output\Case6\SpriteTest_Scan.css");
            minifyCssActivity.ImageAssembleUpdateDestinationFile = Path.Combine(sourceDirectory, @"Output\Case6\SpriteTest_Update.css");
            minifyCssActivity.ImagesOutputDirectory = Path.Combine(sourceDirectory, @"Output\Case6\Images\");
            minifyCssActivity.DestinationFile = Path.Combine(sourceDirectory, @"Output\Case6\SpriteTest.css");
            minifyCssActivity.ShouldAssembleBackgroundImages = true;
            minifyCssActivity.OutputUnit = "rem";
            minifyCssActivity.OutputUnitFactor = 0.1;
            minifyCssActivity.ShouldMinify = true;
            minifyCssActivity.ShouldOptimize = true;
            minifyCssActivity.AdditionalImageAssemblyBuckets.Add(new ImageAssemblyScanInput(".Lazy", new ReadOnlyCollection<string>(new List<string> { @"images/lazy1.jpg", @"images/lazy2.jpg" })));
            minifyCssActivity.Execute();

            // Assertions
            var outputFilePath = minifyCssActivity.DestinationFile;

            // mapping file (so we can look up the target name of the assembled image, as the generated image can be different based on gdi dll versions)
            var mapFilePath = minifyCssActivity.ImageAssembleScanDestinationFile + ".xml";
            var testImage = "media.gif";

            Assert.IsTrue(File.Exists(outputFilePath));
            
            // RTUIT: File generation commented in the minify code, since it does not seem to be used anywhere, save lots of performance
            // outputFilePath = minifyCssActivity.ImageAssembleScanDestinationFile;
            // Assert.IsTrue(File.Exists(outputFilePath));

            Assert.IsTrue(File.Exists(mapFilePath));
            // verify our test file is in the xml file and get the source folder and assembled file name.
            string relativePath;
            using (var fs = File.OpenRead(mapFilePath))
            {
                var mapFile = XDocument.Load(fs);
                var inputElement = mapFile.Root.Descendants()
                    // get at the input elements
                    .Descendants().Where(e => e.Name == "input")
                    // now at the source file name
                    .Descendants().FirstOrDefault(i => i.Name == "originalfile" && i.Value.Contains(testImage));

                // get the output 
                var outputElement = inputElement.Parent.Parent;

                // get the input path from the location of the css file and the output path where the destination file is.
                var inputPath = Path.GetDirectoryName(inputElement.Value).ToLowerInvariant();
                var outputPath = outputElement.Attribute("file").Value.ToLowerInvariant();

                // diff the paths to get the relative path (as found in the final file)
                relativePath = outputPath.MakeRelativeTo(inputPath);
            }
            outputFilePath = minifyCssActivity.ImageAssembleUpdateDestinationFile;
            Assert.IsTrue(File.Exists(outputFilePath));
            var text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(text.Contains("background:0 0 url(" + relativePath + ") no-repeat;"));
        }

        /// <summary>A test for Css sprite.</summary>
        [TestMethod]
        public void CssImageSpriteTest2()
        {
            var sourceDirectory = Path.Combine(
                TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\MinifyCssActivityTest");
            var minifyCssActivity = new MinifyCssActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            minifyCssActivity.SourceFile = Path.Combine(sourceDirectory, @"Input\Case7\SpriteTest.css");
            minifyCssActivity.ImageAssembleScanDestinationFile = Path.Combine(
                sourceDirectory, @"Output\Case7\SpriteTest_Scan.css");
            minifyCssActivity.ImageAssembleUpdateDestinationFile = Path.Combine(
                sourceDirectory, @"Output\Case7\SpriteTest_Update.css");
            minifyCssActivity.ImagesOutputDirectory = Path.Combine(sourceDirectory, @"Output\Case6\Images\");
            minifyCssActivity.DestinationFile = Path.Combine(sourceDirectory, @"Output\Case7\SpriteTest.css");
            minifyCssActivity.ShouldAssembleBackgroundImages = true;
            minifyCssActivity.OutputUnit = "rem";
            minifyCssActivity.OutputUnitFactor = 0.1;
            minifyCssActivity.ShouldMinify = true;
            minifyCssActivity.ShouldOptimize = true;
            minifyCssActivity.Execute();

            // Assertions
            var outputFilePath = minifyCssActivity.DestinationFile;

            // mapping file (so we can look up the target name of the assembled image, as the generated image can be different based on gdi dll versions)
            var mapFilePath = minifyCssActivity.ImageAssembleScanDestinationFile + ".xml";
            var testImage = "media.gif";

            Assert.IsTrue(File.Exists(outputFilePath));
            // RTUIT: File generation commented in the minify code, since it does not seem to be used anywhere, save lots of performance
            // outputFilePath = minifyCssActivity.ImageAssembleScanDestinationFile;
            // Assert.IsTrue(File.Exists(outputFilePath));

            Assert.IsTrue(File.Exists(mapFilePath));
            // verify our test file is in the xml file and get the source folder and assembled file name.
            string relativePath;
            using (var fs = File.OpenRead(mapFilePath))
            {
                var mapFile = XDocument.Load(fs);
                var inputElement = mapFile.Root.Descendants()
                    // get at the input elements
                    .Descendants().Where(e => e.Name == "input")
                    // now at the source file name
                    .Descendants().FirstOrDefault(i => i.Name == "originalfile" && i.Value.Contains(testImage));

                // get the output 
                var outputElement = inputElement.Parent.Parent;

                // get the input path from the location of the css file and the output path where the destination file is.
                var inputPath = Path.GetDirectoryName(inputElement.Value).ToLowerInvariant();
                var outputPath = outputElement.Attribute("file").Value.ToLowerInvariant();

                // diff the paths to get the relative path (as found in the final file)
                relativePath = outputPath.MakeRelativeTo(inputPath);
            }


            // In between result
            outputFilePath = minifyCssActivity.ImageAssembleUpdateDestinationFile;
            Assert.IsTrue(File.Exists(outputFilePath));
            var text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(text.Contains("/*"));
            Assert.IsTrue(text.Contains("*/"));

            // Minified result
            outputFilePath = minifyCssActivity.DestinationFile;
            Assert.IsTrue(File.Exists(outputFilePath));
            text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(!text.Contains("/*"));
            Assert.IsTrue(!text.Contains("*/"));
            Assert.IsTrue(!text.Contains(";;"));
        }
    }
}
