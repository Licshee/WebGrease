// ---------------------------------------------------------------------
// <copyright file="SelectorValidationOptimizationVisitorTest.cs" company="Microsoft">
//    Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>SelectorValidationOptimizationVisitorTest unit test cases</summary>
// ---------------------------------------------------------------------
namespace Css.Tests.Css30
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WebGrease.Tests;

    using TestSuite;
    using WebGrease;
    using WebGrease.Css;
    using WebGrease.Css.Visitor;

    /// <summary>
    /// Unit tests for <see cref="SelectorValidationOptimizationVisitor"/> class.
    /// </summary>
    [TestClass]
    public class SelectorValidationOptimizationVisitorTest
    {
        /// <summary>The base directory.</summary>
        private static readonly string BaseDirectory;

        /// <summary>
        /// The list of css hacks
        /// </summary>
        private readonly HashSet<string> hacks = new HashSet<string> { "html>body", "* html", "*:first-child+html p", "head:first-child+body", "head+body", "body>", "*>html", "*html>body" };

        /// <summary>Initializes static members of the <see cref="SelectorValidationOptimizationVisitorTest"/> class.</summary>
        static SelectorValidationOptimizationVisitorTest()
        {
            BaseDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"css.tests\css30\selectorvalidationoptimizationvisitor");
        }

        /// <summary>
        /// Unit test for hack exceptions
        /// </summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void HacksExceptions()
        {
            try
            {
                const int ExpectedExceptionCount = 8;
                var exceptionCount = 0;
                var inputDirectory = new DirectoryInfo(Path.Combine(BaseDirectory, "Hacks"));
                foreach (var fileInfo in inputDirectory.GetFiles())
                {
                    this.VisitCssWithHacks(fileInfo.FullName, ref exceptionCount);
                }

                Assert.IsTrue(exceptionCount == ExpectedExceptionCount, "The exception count is not equal to " + ExpectedExceptionCount);
            }
            catch (Exception)
            {
                Assert.Fail("The hacks exception is not caught.");
            }
        }

        /// <summary>
        /// Unit test for removing the list of selectors
        /// </summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void RemoveSelectors()
        {
            this.hacks.Add("foo");
            MinificationVerifier.VerifyMinification(BaseDirectory, "RemoveSelectors.css", new List<NodeVisitor> { new SelectorValidationOptimizationVisitor(this.hacks, false, false) });
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, "RemoveSelectors.css", new List<NodeVisitor> { new SelectorValidationOptimizationVisitor(this.hacks, false, false) });
        }

        /// <summary>
        /// Visits the css with lower case validation visitor
        /// </summary>
        /// <param name="inputFileName">The file name</param>
        /// <param name="count">The exception count</param>
        private void VisitCssWithHacks(string inputFileName, ref int count)
        {
            try
            {
                CssParser.Parse(new FileInfo(inputFileName)).Accept(new SelectorValidationOptimizationVisitor(this.hacks, false, true));
            }
            catch (BuildWorkflowException exception)
            {
                Trace.WriteLine(inputFileName + ":");
                Trace.WriteLine(exception.ToString());
                count++;
            }
        }
    }
}
