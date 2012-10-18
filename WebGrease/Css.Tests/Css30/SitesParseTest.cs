// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SitesParseTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for tests for various sites css
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Css.Tests.Css30
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestSuite;
    using WebGrease.Css;

    /// <summary>This is a test class for tests for various sites css</summary>
    [TestClass]
    public class SitesParseTest
    {
        /// <summary>A test for site parsing various site css.</summary>
        [TestMethod]
        public void ParseTest()
        {
            var directoryName = Path.Combine(TestDeploymentPaths.TestDirectory, @"css.tests\css21\sites");
            var directoryInfo = new DirectoryInfo(directoryName);
            TryParseCssFiles(directoryInfo);

            directoryName = Path.Combine(TestDeploymentPaths.TestDirectory, @"css.tests\css30\sites");
            directoryInfo = new DirectoryInfo(directoryName);
            TryParseCssFiles(directoryInfo);
        }

        /// <summary>The try parse css files.</summary>
        /// <param name="directoryInfo">The directory info.</param>
        private static void TryParseCssFiles(DirectoryInfo directoryInfo)
        {
            foreach (var cssFile in directoryInfo.EnumerateFiles("*.css", SearchOption.AllDirectories))
            {
                try
                {
                    var styleSheetNode = CssParser.Parse(cssFile, false);
                    Assert.IsNotNull(styleSheetNode);
                }
                catch (Exception)
                {
                    // Parse again with Trace ON
                    CssParser.Parse(cssFile);
                    throw;
                }
            }
        }
    }
}
