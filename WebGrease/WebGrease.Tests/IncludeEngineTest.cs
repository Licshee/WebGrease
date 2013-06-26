// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="IncludeEngineTest.cs" company="Microsoft Corporation">
// //   Copyright 2012 Microsoft Corporation, all rights reserved
// // </copyright>
// // --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.WebGrease.Tests
{
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using global::WebGrease;
    using global::WebGrease.Configuration;
    using global::WebGrease.Preprocessing.Include;
    using global::WebGrease.Tests;
    using global::WebGrease.Extensions;

    [TestClass]
    public class IncludeEngineTest
    {
        #region Public Methods and Operators

        [TestMethod]
        [TestCategory(TestCategories.WgInclude)]
        public void TestWgInclude1()
        {
            var includeFile = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\IncludeTest\Test1\test1.js");
            var ie = new IncludePreprocessingEngine();
            var webGreaseContext = new WebGreaseContext(new WebGreaseConfiguration() { DestinationDirectory = TestDeploymentPaths.TestDirectory });
            var result = ie.Process(webGreaseContext, ContentItem.FromFile(includeFile, includeFile.MakeRelativeToDirectory(TestDeploymentPaths.TestDirectory)), new PreprocessingConfig(), false).Content;

            Assert.IsTrue(result.Contains("included1();"));
            Assert.IsTrue(result.Contains("included2();"));
            Assert.IsTrue(result.Contains("included3();"));
            Assert.IsTrue(result.Contains("included4();"));
            Assert.IsFalse(result.Contains("included5();"));
        }

        #endregion
    }
}