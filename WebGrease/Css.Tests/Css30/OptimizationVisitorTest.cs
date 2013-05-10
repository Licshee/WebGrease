// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OptimizationVisitorTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for OptimizationVisitorTest and is intended
//   to contain all OptimizationVisitorTest Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Css.Tests.Css30
{
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WebGrease.Tests;

    using TestSuite;
    using WebGrease.Css;
    using WebGrease.Css.Visitor;

    /// <summary>
    /// This is a test class for OptimizationVisitorTest and is intended
    /// to contain all OptimizationVisitorTest Unit Tests
    /// </summary>
    [TestClass]
    public class OptimizationVisitorTest
    {
        /// <summary>The base directory.</summary>
        private static readonly string BaseDirectory;

        /// <summary>The expect directory.</summary>
        private static readonly string ActualDirectory;

        /// <summary>Initializes static members of the <see cref="OptimizationVisitorTest"/> class.</summary>
        static OptimizationVisitorTest()
        {
            BaseDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"css.tests\css30\optimizationvisitor");
            ActualDirectory = Path.Combine(BaseDirectory, @"actual");
        }

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for ruleset optimization.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void OptimizeRulesetTest()
        {
            const string FileName = @"ruleset.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { new OptimizationVisitor() });
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName, new List<NodeVisitor> { new OptimizationVisitor() });
        }

        /// <summary>A test for border optimization.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void OptimizeBorder()
        {
            const string FileName = @"border.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { new OptimizationVisitor() });
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName, new List<NodeVisitor> { new OptimizationVisitor() });
        }
    }
}