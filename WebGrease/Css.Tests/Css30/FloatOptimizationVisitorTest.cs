// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FloatOptimizationVisitorTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for FloatOptimizationVisitorTest and is intended
//   to contain all FloatOptimizationVisitorTest Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Css.Tests.Css30
{
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestSuite;
    using WebGrease.Css;
    using WebGrease.Css.Visitor;

    /// <summary>
    /// This is a test class for FloatOptimizationVisitorTest and is intended
    /// to contain all FloatOptimizationVisitorTest Unit Tests
    /// </summary>
    [TestClass]
    public class FloatOptimizationVisitorTest
    {
        /// <summary>The base directory.</summary>
        private static readonly string BaseDirectory;

        /// <summary>The expect directory.</summary>
        private static readonly string ActualDirectory;

        /// <summary>Initializes static members of the <see cref="FloatOptimizationVisitorTest"/> class.</summary>
        static FloatOptimizationVisitorTest()
        {
            BaseDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"css.tests\css30\floatoptimizationvisitor");
            ActualDirectory = Path.Combine(BaseDirectory, @"actual");
        }

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for float optimization.</summary>
        [TestMethod]
        public void OptimizeFloatTest()
        {
            const string FileName = @"optimizefloat.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName, new List<NodeVisitor> { new FloatOptimizationVisitor() });
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName, new List<NodeVisitor> { new FloatOptimizationVisitor() });
        }
    }
}