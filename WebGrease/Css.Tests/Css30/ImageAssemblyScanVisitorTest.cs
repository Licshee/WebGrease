// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImageAssemblyScanVisitorTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for ImageAssemblyScanVisitorTest and is intended
//   to contain all ImageAssemblyScanVisitorTest Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Css.Tests.Css30
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestSuite;
    using WebGrease.Css;
    using WebGrease.Css.Ast;
    using WebGrease.Css.ImageAssemblyAnalysis;
    using WebGrease.Css.Visitor;

    /// <summary>This is a test class for ImageAssemblyScanVisitorTest and is intended
    /// to contain all ImageAssemblyScanVisitorTest Unit Tests</summary>
    [TestClass]
    public class ImageAssemblyScanVisitorTest
    {
        /// <summary>The base directory.</summary>
        private static readonly string BaseDirectory;

        /// <summary>The expect directory.</summary>
        private static readonly string ActualDirectory;

        /// <summary>Initializes static members of the <see cref="ImageAssemblyScanVisitorTest"/> class.</summary>
        static ImageAssemblyScanVisitorTest()
        {
            BaseDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"css.tests\css30\imageassemblyscanvisitor");
            ActualDirectory = Path.Combine(BaseDirectory, @"actual");
        }

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for background selectors which should be sprited.</summary>
        [TestMethod]
        public void SpritingCandidatesTest()
        {
            const string FileName = @"spritingcandidates.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));
            
            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);

            var visitor = new ImageAssemblyScanVisitor(fileInfo.FullName, null, null);
            styleSheetNode = styleSheetNode.Accept(visitor) as StyleSheetNode;
            Assert.IsNotNull(styleSheetNode);

            Trace.WriteLine(visitor.ImageAssemblyAnalysisLog.ToString());
            var imageReferencesToAssemble = visitor.DefaultImageAssemblyScanOutput.ImageReferencesToAssemble;
            Assert.IsNotNull(imageReferencesToAssemble);
            Assert.IsTrue(imageReferencesToAssemble.Count == 95);
        }

        /// <summary>A test for background selectors which should be sprited with ignore.</summary>
        [TestMethod]
        public void SpritingCandidatesWithIgnoreTest()
        {
            const string FileName = @"spritingcandidateswithignore.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));

            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);

            var visitor = new ImageAssemblyScanVisitor(fileInfo.FullName, new[] { "/i/1.gif", "/i/2.gif" }, null);
            styleSheetNode = styleSheetNode.Accept(visitor) as StyleSheetNode;
            Assert.IsNotNull(styleSheetNode);

            var imageReferencesToAssemble = visitor.DefaultImageAssemblyScanOutput.ImageReferencesToAssemble;
            Assert.IsNotNull(imageReferencesToAssemble);
            Assert.IsTrue(imageReferencesToAssemble.Count == 3);
            Assert.IsTrue(imageReferencesToAssemble[0].ImagePath.Contains(@"\i\3.gif"));
            Assert.IsTrue(imageReferencesToAssemble[1].ImagePath.Contains(@"\i\4.gif"));
            Assert.IsTrue(imageReferencesToAssemble[2].ImagePath.Contains(@"\i\5.gif"));
        }

        /// <summary>A test for background selectors which should be sprited with buckets.</summary>
        [TestMethod]
        public void SpritingCandidatesWithBucketsTest()
        {
            const string FileName = @"spritingcandidateswithbuckets.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));

            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);

            // Image 1 - Ignore
            // Image 2 - Zero Bucket (Default)
            // Image 3 - First Bucket
            // Image 4, 5 - Second Bucket
            var visitor = new ImageAssemblyScanVisitor(fileInfo.FullName, new[] { "/i/1.gif" }, new[] { new ImageAssemblyScanInput("lazy.xml", new List<string> { "/i/3.gif" }.AsReadOnly()), new ImageAssemblyScanInput("lazy.xml", new List<string> { "/i/4.gif", "/i/5.gif" }.AsReadOnly()) });
            styleSheetNode = styleSheetNode.Accept(visitor) as StyleSheetNode;
            Assert.IsNotNull(styleSheetNode);

            var imageAssemblyScanOutputs = visitor.ImageAssemblyScanOutputs;
            Assert.IsNotNull(imageAssemblyScanOutputs);
            Assert.IsTrue(imageAssemblyScanOutputs.Count == 3);

            // Zero bucket
            var imageAssemblyScanOutput = imageAssemblyScanOutputs[0];
            var imageReferencesToAssemble = imageAssemblyScanOutput.ImageReferencesToAssemble;
            Assert.IsTrue(imageReferencesToAssemble.Count == 1);
            var imageReferenceToAssemble = imageReferencesToAssemble[0];
            Assert.IsNotNull(imageReferenceToAssemble);
            Assert.IsTrue(imageReferenceToAssemble.ImagePath.Contains(@"\i\2.gif"));

            // First bucket
            imageAssemblyScanOutput = imageAssemblyScanOutputs[1];
            imageReferencesToAssemble = imageAssemblyScanOutput.ImageReferencesToAssemble;
            Assert.IsTrue(imageReferencesToAssemble.Count == 1);
            imageReferenceToAssemble = imageReferencesToAssemble[0];
            Assert.IsNotNull(imageReferenceToAssemble);
            Assert.IsTrue(imageReferenceToAssemble.ImagePath.Contains(@"\i\3.gif"));

            // Second bucket
            imageAssemblyScanOutput = imageAssemblyScanOutputs[2];
            imageReferencesToAssemble = imageAssemblyScanOutput.ImageReferencesToAssemble;
            Assert.IsTrue(imageReferencesToAssemble.Count == 2);
            imageReferenceToAssemble = imageReferencesToAssemble[0];
            Assert.IsNotNull(imageReferenceToAssemble);
            Assert.IsTrue(imageReferenceToAssemble.ImagePath.Contains(@"\i\4.gif"));
            imageReferenceToAssemble = imageReferencesToAssemble[1];
            Assert.IsNotNull(imageReferenceToAssemble);
            Assert.IsTrue(imageReferenceToAssemble.ImagePath.Contains(@"\i\5.gif"));
        }

        /// <summary>A test for background selectors with duplicate declaration.</summary>
        [TestMethod]
        public void RepeatedPropertyNameExceptionTest()
        {
            const string FileName = @"repeatedpropertynameexception.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));

            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);

            try
            {
                styleSheetNode.Accept(new ImageAssemblyScanVisitor(fileInfo.FullName, null, null));
            }
            catch (ImageAssembleException imageAssembleException)
            {
                Assert.IsTrue(imageAssembleException.ToString().Contains(string.Format(CultureInfo.InvariantCulture, CssStrings.RepeatedPropertyNameError, "background-image")));
            }
        }

        /// <summary>A test for duplicate background format exception.</summary>
        [TestMethod]
        public void DuplicateBackgroundFormatExceptionTest()
        {
            const string FileName = @"duplicatebackgroundformatexception.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));

            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);
            
            try
            {
                styleSheetNode.Accept(new ImageAssemblyScanVisitor(fileInfo.FullName, null, null));
            }
            catch (ImageAssembleException imageAssembleException)
            {
                Assert.IsTrue(imageAssembleException.ToString().Contains(CssStrings.DuplicateBackgroundFormatError));
            }
        }

        /// <summary>A test for duplicate image references with different rules.</summary>
        [TestMethod]
        public void DuplicateImageReferenceWithDifferentRulesExceptionTest()
        {
            const string FileName = @"duplicateimagereferencewithdifferentrulesexception.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));

            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);

            try
            {
                styleSheetNode.Accept(new ImageAssemblyScanVisitor(fileInfo.FullName, null, null));
            }
            catch (ImageAssembleException imageAssembleException)
            {
                Assert.IsTrue(imageAssembleException.ToString().Contains(string.Format(CultureInfo.InvariantCulture, CssStrings.DuplicateImageReferenceWithDifferentRulesError, Path.Combine(ActualDirectory, "foo.gif").ToLowerInvariant())));
            }
        }

        /// <summary>A test for too many lengths on background node.</summary>
        [TestMethod]
        public void TooManyLengthsExceptionTest()
        {
            const string FileName = @"toomanylengthsexception.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));

            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);

            try
            {
                styleSheetNode.Accept(new ImageAssemblyScanVisitor(fileInfo.FullName, null, null));
            }
            catch (ImageAssembleException imageAssembleException)
            {
                Assert.IsTrue(imageAssembleException.ToString().Contains(string.Format(CultureInfo.InvariantCulture, CssStrings.TooManyLengthsError, string.Empty).TrimEnd(new[] { '.', '\'' })));
            }
        }
    }
}