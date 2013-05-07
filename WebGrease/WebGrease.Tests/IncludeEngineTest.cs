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

    [TestClass]
    public class IncludeEngineTest
    {
        #region Public Methods and Operators

        [TestMethod]
        public void TestWgInclude1()
        {
            var includeFile = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\IncludeTest\Test1\test1.js");
            var ie = new IncludePreprocessingEngine();
            ie.SetContext(new WebGreaseContext(new WebGreaseConfiguration()));
            var result = ie.Process(File.ReadAllText(includeFile), includeFile, new PreprocessingConfig());

            Assert.IsTrue(result.Contains("included1();"));
            Assert.IsTrue(result.Contains("included2();"));
            Assert.IsTrue(result.Contains("included3();"));
            Assert.IsTrue(result.Contains("included4();"));
            Assert.IsFalse(result.Contains("included5();"));
        }

        #endregion
    }
}