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

        /// <summary> A test for ruleset optimization when there is conflict due to the ordering.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void ConflictDueToOrderingNotCollapseTest()
        {
            const string FileName = @"OrderBasedConflicts.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { new OptimizationVisitor() });
        }

        /// <summary> A test for ruleset optimization when there is not conflict due to the ordering.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void ConflictDueToOrderingCollapseTest()
        {
            const string FileName = @"OrderBasedConflictsCollapse.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { new OptimizationVisitor() });
        }

        /// <summary> A test for ruleset merging optimization based on the common declarations accross diffferent selectors.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void MergingRulesetsBasedOnDeclarationsTest()
        {
            const string FileName = @"Merge.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { new OptimizationVisitor() });

            const string FileName2 = @"Merge2.css";
            var styleSheetNode2 = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName2)));
            Assert.IsNotNull(styleSheetNode2);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName2, new List<NodeVisitor> { new OptimizationVisitor() });
            
            const string FileName3 = @"Merge3.css";
            var styleSheetNode3 = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName3)));
            Assert.IsNotNull(styleSheetNode3);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName3, new List<NodeVisitor> { new OptimizationVisitor() });
           
            const string FileName4 = @"Merge4.css";
            var styleSheetNode4 = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName4)));
            Assert.IsNotNull(styleSheetNode4);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName4, new List<NodeVisitor> { new OptimizationVisitor() });
            
            const string FileName5 = @"Merge5.css";
            var styleSheetNode5 = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName5)));
            Assert.IsNotNull(styleSheetNode5);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName5, new List<NodeVisitor> { new OptimizationVisitor() });
            
            const string FileName6 = @"Merge6.css";
            var styleSheetNode6 = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName6)));
            Assert.IsNotNull(styleSheetNode6);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName6, new List<NodeVisitor> { new OptimizationVisitor() });
        }
    }
}