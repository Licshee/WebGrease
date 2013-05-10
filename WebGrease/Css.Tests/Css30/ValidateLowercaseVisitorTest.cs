// ---------------------------------------------------------------------
// <copyright file="ValidateLowercaseVisitorTest.cs" company="Microsoft">
//    Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>ValidateLowercaseVisitorTest unit test cases</summary>
// ---------------------------------------------------------------------

namespace Css.Tests.Css30
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WebGrease.Tests;

    using TestSuite;
    using WebGrease;
    using WebGrease.Css;
    using WebGrease.Css.Extensions;
    using WebGrease.Css.Visitor;

    /// <summary>
    /// Unit tests for <see cref="ValidateLowercaseVisitor"/> class.
    /// </summary>
    [TestClass]
    public class ValidateLowercaseVisitorTest
    {
        /// <summary>The base directory.</summary>
        private static readonly string BaseDirectory;

        /// <summary>Initializes static members of the <see cref="ValidateLowercaseVisitorTest"/> class.</summary>
        static ValidateLowercaseVisitorTest()
        {
            BaseDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"css.tests\css30\lowercasevisitorexceptions");
        }

        /// <summary>
        /// Unit test for upper case charset
        /// </summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void LowercaseVisitorExceptions()
        {
            try
            {
                var inputDirectory = new DirectoryInfo(BaseDirectory);
                inputDirectory.GetFiles().ForEach(fileInfo => VisitCssWithLowerCaseValidationVisitor(fileInfo.FullName));
            }
            catch (Exception)
            {
                Assert.Fail("The lower case exception is not caught.");
            }
        }

        /// <summary>
        /// Visits the css with lower case validation visitor
        /// </summary>
        /// <param name="inputFileName">The file name</param>
        private static void VisitCssWithLowerCaseValidationVisitor(string inputFileName)
        {
            try
            {
                CssParser.Parse(new FileInfo(inputFileName)).Accept(new ValidateLowercaseVisitor());
            }
            catch (WorkflowException exception)
            {
                Trace.WriteLine(inputFileName + ":");
                Trace.WriteLine(exception.ToString());
            }
        }
    }
}
