using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Ajax.Utilities;

namespace WebGrease.Tests
{
    /// <summary>
    /// Encapsulates tests for validating that AjaxMin's direct features are available correctly.
    /// </summary>
    [TestClass]
    public class AjaxMinHooks
    {
        /// <summary>
        /// Tests that AjaxMin's css minification can be called from code directly.
        /// </summary>
        /// <remarks>This is not meant to test AjaxMin's minification itself, much, just that integration is available from WebGrease</remarks>
        [TestMethod]
        public void MinifyCssWithAjaxMinTest()
        {
            var basicCssFilePath = Path.Combine(TestDeploymentPaths.TestDirectory, @"AjaxMin\Input\BasicTest.css");
            var cssContent = File.ReadAllText(basicCssFilePath);
            var minifier = new Minifier();
            string actual = minifier.MinifyStyleSheet(cssContent);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(actual));
            Assert.IsTrue(actual.Contains("important"), "AjaxMin css minifier by default should not remove important comments.");
            Assert.IsFalse(actual.Contains("unimportant"), "AjaxMin css minifier by default should remove normal comments.");
            Assert.IsTrue(actual.Contains(@"body{color:#f0f}a:active"), "AjaxMin css minifier by default should minify color names and whitespace.");
            Assert.AreEqual(0, minifier.ErrorList.Count);
        }

        /// <summary>
        /// Tests that AjaxMin's css minification will go through usual error reporting paths. This includes loading resource strings.
        /// </summary>
        /// <remarks>This is not meant to test AjaxMin's minification itself, much, just that integration is available from WebGrease</remarks>
        [TestMethod]
        public void MinifyCssWithAjaxMinErrorTest()
        {
            var errorCssFilePath = Path.Combine(TestDeploymentPaths.TestDirectory, @"AjaxMin\Input\ErrorReachedTest.css");
            var cssContent = File.ReadAllText(errorCssFilePath);
            var minifier = new Minifier();
            string actual = minifier.MinifyStyleSheet(cssContent);
            Assert.IsTrue(minifier.ErrorList.Count > 0);
            Assert.IsTrue(minifier.ErrorList.First().ToString().Contains("Expected selector"));
        }

        /// <summary>
        /// Tests that AjaMin's js minification can be called from code directly.
        /// </summary>
        /// <remarks>This is not meant to test AjaxMin's minification itself, much, just that integration is available from WebGrease</remarks>
        [TestMethod]
        public void MinifyJSWithAjaxMinTest()
        {
            var jsFilePath = Path.Combine(TestDeploymentPaths.TestDirectory, @"AjaxMin\Input\BasicTest.js");
            var fileContent = File.ReadAllText(jsFilePath);
            var minifier = new Minifier();
            string actual = minifier.MinifyJavaScript(fileContent);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(actual));
            Assert.IsTrue(actual.Contains("important"), "AjaxMin js minifier by default should not remove important comments.");
            Assert.IsFalse(actual.Contains("unimportant"), "AjaxMin js minifier by default should remove normal comments.");
            Assert.IsTrue(actual.Contains("foobar"), "AjaxMin js minifier should concatenate string literals by default");
            Assert.IsTrue(actual.Contains("b=3"), "AjaxMin js minifier should add literal number expressions by default");
        }

        /// <summary>
        /// Tests that AjaMin's js minification will go through usual error reporting paths. This includes loading resource strings.
        /// </summary>
        /// <remarks>This is not meant to test AjaxMin's minification itself, much, just that integration is available from WebGrease</remarks>
        [TestMethod]
        public void MinifyJSWithAjaxMinErrorTest()
        {
            var errorFilePath = Path.Combine(TestDeploymentPaths.TestDirectory, @"AjaxMin\Input\ErrorReachedTest.js");
            var fileContent = File.ReadAllText(errorFilePath);
            var minifier = new Minifier();
            string actual = minifier.MinifyJavaScript(fileContent);
            Assert.IsTrue(minifier.ErrorList.Count > 0, "Minifier should have hit errors");
            Assert.IsTrue(minifier.ErrorList.First().ToString().Contains("Expected identifier"));
        }
    }
}
