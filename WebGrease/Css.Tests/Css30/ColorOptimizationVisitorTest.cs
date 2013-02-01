// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExcludeVisitorTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for ExcludeVisitorTest and is intended
//   to contain all ExcludeVisitorTest Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Css.Tests.Css30
{
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestSuite;
    using WebGrease.Css;
    using WebGrease.Css.Ast;
    using WebGrease.Css.Visitor;

    /// <summary>
    /// Summary description for ColorOptimizationVisitorTest
    /// </summary>
    [TestClass]
    public class ColorOptimizationVisitorTest
    {
        /// <summary>The base directory.</summary>
        private static readonly string BaseDirectory;

        /// <summary>The expect directory.</summary>
        private static readonly string ActualDirectory;

        /// <summary>Initializes static members of the <see cref="ExcludeVisitorTest"/> class.</summary>
        static ColorOptimizationVisitorTest()
        {
            BaseDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"css.tests\css30\ColorOptimizationVisitor");
            ActualDirectory = Path.Combine(BaseDirectory, @"actual");
        }

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void HexCollapse()
        {
            const string FileName = @"HexCollapse.css";
            AstNode styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);

            styleSheetNode = styleSheetNode.Accept(new ColorOptimizationVisitor());
            Assert.IsNotNull(styleSheetNode);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { new ColorOptimizationVisitor() });
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName, new List<NodeVisitor> { new ColorOptimizationVisitor() });
        }

        [TestMethod]
        public void RgbCollapse()
        {
            const string FileName = @"RgbCollapse.css";
            AstNode styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);

            styleSheetNode = styleSheetNode.Accept(new ColorOptimizationVisitor());
            Assert.IsNotNull(styleSheetNode);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { new ColorOptimizationVisitor() });
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName, new List<NodeVisitor> { new ColorOptimizationVisitor() });
        }
    }
}
