// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImageAssemblyUpdateVisitorTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for ImageAssemblyUpdateVisitorTest and is intended
//   to contain all ImageAssemblyUpdateVisitorTest Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Css.Tests.Css30
{
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WebGrease.Tests;

    using TestSuite;
    using WebGrease.Css;
    using WebGrease.Css.Ast;
    using WebGrease.Css.ImageAssemblyAnalysis;
    using WebGrease.Css.Visitor;

    /// <summary>
    /// This is a test class for ImageAssemblyUpdateVisitorTest and is intended
    /// to contain all ImageAssemblyUpdateVisitorTest Unit Tests
    /// </summary>
    [TestClass]
    public class ImageAssemblyUpdateVisitorTest
    {
        /// <summary>The base directory.</summary>
        private static readonly string BaseDirectory;

        /// <summary>The expect directory.</summary>
        private static readonly string ActualDirectory;

        /// <summary>Initializes static members of the <see cref="ImageAssemblyUpdateVisitorTest"/> class.</summary>
        static ImageAssemblyUpdateVisitorTest()
        {
            BaseDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"css.tests\css30\imageassemblyupdatevisitor");
            ActualDirectory = Path.Combine(BaseDirectory, @"actual");
        }

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for long background selectors which should be sprited.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.ImageAssemblyUpdateVisitor)]
        public void ImageUpdateVisitorLongDeclarationsTest()
        {
            const string FileName = @"imageupdatevisitorlongdeclarations.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));
            
            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);

            var visitor = CreateVisitor(fileInfo.FullName);
            styleSheetNode = styleSheetNode.Accept(visitor) as StyleSheetNode;
            Assert.IsNotNull(styleSheetNode);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
        }

        /// <summary>A test for short background selectors which should be sprited.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void ImageUpdateVisitorShorthandDeclarationsTest()
        {
            const string FileName = @"imageupdatevisitorshorthanddeclarations.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));

            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);

            var visitor = CreateVisitor(fileInfo.FullName);
            styleSheetNode = styleSheetNode.Accept(visitor) as StyleSheetNode;
            Assert.IsNotNull(styleSheetNode);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
        }

        /// <summary>A test for short and first position background selectors which should be sprited.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.ImageAssemblyUpdateVisitor)]
        public void ImageUpdateVisitorShorthandDeclarationsFirstPositionTest()
        {
            const string FileName = @"imageupdatevisitorshorthanddeclarationsfirstposition.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));

            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);

            var visitor = CreateVisitor(fileInfo.FullName);
            styleSheetNode = styleSheetNode.Accept(visitor) as StyleSheetNode;
            Assert.IsNotNull(styleSheetNode);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
        }

        /// <summary>A test for short and second position background selectors which should be sprited.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.ImageAssemblyUpdateVisitor)]
        public void ImageUpdateVisitorShorthandDeclarationsSecondPositionTest()
        {
            const string FileName = @"imageupdatevisitorshorthanddeclarationssecondposition.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));

            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);

            var visitor = CreateVisitor(fileInfo.FullName);
            styleSheetNode = styleSheetNode.Accept(visitor) as StyleSheetNode;
            Assert.IsNotNull(styleSheetNode);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
        }

        /// <summary>A test for using a different unit and scale factor.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.ImageAssemblyUpdateVisitor)]
        public void ImageUpdateVisitorRemTest()
        {
            const string FileName = @"imageupdatevisitorrem.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));

            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);

            // use 2X DPI, REM units, and scale the number by 1/10.
            var visitor = CreateVisitor(fileInfo.FullName, 2d, ImageAssembleConstants.Rem, 0.1);
            styleSheetNode = styleSheetNode.Accept(visitor) as StyleSheetNode;
            Assert.IsNotNull(styleSheetNode);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
        }

        /// <summary>A test for using a different unit and scale factor.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.ImageAssemblyUpdateVisitor)]
        public void ImageUpdateVisitorDpiTest()
        {
            const string FileName = @"imageupdatevisitordpi.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));

            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.Dpi);

            var visitor = CreateVisitor(fileInfo.FullName, styleSheetNode.Dpi.GetValueOrDefault(1d));
            styleSheetNode = styleSheetNode.Accept(visitor) as StyleSheetNode;
            Assert.IsNotNull(styleSheetNode);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
        }

        /// <summary>A test for media background selectors which should be sprited.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.ImageAssemblyUpdateVisitor)]
        public void MediaTest()
        {
            const string FileName = @"media.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));

            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);

            var visitor = CreateVisitor(fileInfo.FullName);
            styleSheetNode = styleSheetNode.Accept(visitor) as StyleSheetNode;
            Assert.IsNotNull(styleSheetNode);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
        }

        /// <summary>A test for page background selectors which should be sprited.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.ImageAssemblyUpdateVisitor)]
        public void PageTest()
        {
            const string FileName = @"page.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));

            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);

            var visitor = CreateVisitor(fileInfo.FullName);
            styleSheetNode = styleSheetNode.Accept(visitor) as StyleSheetNode;
            Assert.IsNotNull(styleSheetNode);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName, new List<NodeVisitor> { visitor });
        }

        /// <summary>Creates the visitor.</summary>
        /// <param name="cssPath">The css path.</param>
        /// <returns>The update visitor.</returns>
        private static ImageAssemblyUpdateVisitor CreateVisitor(string cssPath, double dpi = 1d, string outputUnit = ImageAssembleConstants.Px, double outputUnitFactor = 1d)
        {
            var xmlPath = cssPath + ".xml";
            var xmlPathLazyLoad = cssPath + ".lazyload.xml";
            XDocument.Parse(XDocument.Load(xmlPath).ToString().Replace("[FolderPath]", new FileInfo(xmlPath).DirectoryName)).Save(xmlPath);
            XDocument.Parse(XDocument.Load(xmlPathLazyLoad).ToString().Replace("[FolderPath]", new FileInfo(xmlPathLazyLoad).DirectoryName)).Save(xmlPathLazyLoad);
            return new ImageAssemblyUpdateVisitor(cssPath, new[] { xmlPath, xmlPathLazyLoad }, dpi, outputUnit, outputUnitFactor);
        }
    }
}