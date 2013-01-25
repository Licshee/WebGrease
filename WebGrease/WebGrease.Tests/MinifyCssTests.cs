// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MinifyCssTests.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for using MinifyCss and is intended
//   to contain all MinifyCss Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------using System;

namespace WebGrease.Tests
{
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// This is a test class for using MinifyCss and is intended to contain all MinifyCss Unit Tests
    /// </summary>
    [TestClass]
    public class MinifyCssTests
    {
        /// <summary>
        /// Verifies basic tests for good css being minified with available options.
        /// </summary>
        [TestMethod]
        public void MinifiesGoodCssTest()
        {
            var goodCssContent = "p {color:red;}\r\np {margin: 10px}\r\np {width: 10px}\r\nbody {margin: 1em;}";
            var expected = "p{color:red;margin:10px;width:10px}body{margin:1em}";

            var minifier = new CssMinifier();
            var actual = minifier.Minify(goodCssContent);
            Assert.AreEqual<string>(actual, expected);

            // pretty print is the only other option right now; should add indents, new lines etc.
            minifier.ShouldMinify = false;
            expected = "p\r\n{\r\n  color:red;\r\n  margin:10px;\r\n  width:10px\r\n}\r\nbody\r\n{\r\n  margin:1em\r\n}\r\n";
            actual = minifier.Minify(goodCssContent);
            Assert.AreEqual<string>(actual, expected);
        }

        /// <summary>
        /// Tests error reporting for bad (unparseable) css.
        /// </summary>
        [TestMethod]
        public void MinifierErrorsTest()
        {
            var badCss = "p {color:red;}\r\np {margin: 10px}\r\np {width: }\r\nbody margin: 1em;}";
            var minifier = new CssMinifier();
            var output = minifier.Minify(badCss);
            Assert.IsTrue(minifier.Errors.Count > 0);
            Assert.IsTrue(minifier.Errors[0].StartsWith("(3"), "first error should be on line 3");
        }

        /// <summary>
        /// Tests a complex css file and minifies something without blowing up.
        /// </summary>
        [TestMethod]
        public void MinifyComplexCaseTest()
        {
            var minifier = new CssMinifier();
            var filePath = Path.Combine(TestDeploymentPaths.TestDirectory, @"css.tests\css30\sites\vsteam\jquery-combined.css");
            var css = File.ReadAllText(filePath);
            var actual = minifier.Minify(css);
            Assert.IsTrue(actual.Length < css.Length);
        }
    }
}
