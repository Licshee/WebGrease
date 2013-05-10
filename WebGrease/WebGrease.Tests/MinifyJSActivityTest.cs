// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MinifyJSActivityTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for MinifyJSActivityTest and is intended
//   to contain all MinifyJSActivityTest Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Tests
{
    using System;
    using System.IO;
    using Activities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WebGrease.Tests;

    using WebGrease.Configuration;

    /// <summary>This is a test class for MinifyJSActivityTest and is intended
    /// to contain all MinifyJSActivityTest Unit Tests</summary>
    [TestClass]
    public class MinifyJSActivityTest
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for JS minification.</summary>
        [TestMethod]
        [TestCategory(TestCategories.MinifyJSActivity)]
        [TestCategory(TestCategories.AjaxMin)]
        [TestCategory(TestCategories.JavaScript)]
        public void JSMinificationTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\MinifyJSActivityTest");
            var minifyJSActivity = new MinifyJSActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            minifyJSActivity.SourceFile = Path.Combine(sourceDirectory, @"Input\Case1\test1.js");
            var outputFilePath = Path.Combine(sourceDirectory, @"Output\Case1\test1.js");
            minifyJSActivity.DestinationFile = outputFilePath;
            minifyJSActivity.MinifyArgs = "/global:jQuery";
            minifyJSActivity.ShouldMinify = true;
            minifyJSActivity.Execute();

            // Assertions
            Assert.IsTrue(File.Exists(outputFilePath));
            var text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(text));
            Assert.IsTrue(text == "(function(n){document.write(n)})(jQuery);");
        }

        /// <summary>A test for JS analysis.</summary>
        [TestMethod]
        [TestCategory(TestCategories.MinifyJSActivity)]
        [TestCategory(TestCategories.AjaxMin)]
        [TestCategory(TestCategories.JavaScript)]
        public void JSAnalysisTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\MinifyJSActivityTest");
            var minifyJSActivity = new MinifyJSActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            minifyJSActivity.SourceFile = Path.Combine(sourceDirectory, @"Input\Case2\test1.js");
            minifyJSActivity.DestinationFile = Path.Combine(sourceDirectory, @"Output\Case2\test1.js");
            minifyJSActivity.MinifyArgs = "/global:jQuery";
            minifyJSActivity.AnalyzeArgs = "-analyze -WARN:4";
            minifyJSActivity.ShouldMinify = true;
            minifyJSActivity.ShouldAnalyze = true;

            Exception exception = null;
            try
            {
                minifyJSActivity.Execute();
            }
            catch (WorkflowException workflowException)
            {
                exception = workflowException;
            }

            // Assertions
            Assert.IsNotNull(exception);
        }
    }
}
