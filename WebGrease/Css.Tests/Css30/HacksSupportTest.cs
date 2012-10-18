// -----------------------------------------------------------------------
// <copyright file="TroubleshootingTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Css.Tests.Css30
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestSuite;
    using WebGrease.Css;
    using WebGrease.Css.Extensions;

    /// <summary>
    /// Test for trouble shooting
    /// </summary>
    [TestClass]
    public class HacksSupportTest
    {
        /// <summary>The base directory.</summary>
        private static readonly string BaseDirectory;

        /// <summary>The expect directory.</summary>
        private static readonly string ActualDirectory;

        /// <summary>Initializes static members of the <see cref="ParserTest"/> class.</summary>
        static HacksSupportTest()
        {
            BaseDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"css.tests\css30\Sites");
            ActualDirectory = Path.Combine(BaseDirectory, @"VSTeam");
        }

        /// <summary>A test for the basic collection of hacks</summary>
        [TestMethod]
        public void HacksFileTest()
        {
            const string FileName = @"hacks.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);

            var styleSheetRules = styleSheetNode.StyleSheetRules;
            Assert.IsNotNull(styleSheetRules);

            var minifiedCss = styleSheetNode.MinifyPrint();
            var prettyCss = styleSheetNode.PrettyPrint();
            Assert.IsFalse(string.IsNullOrWhiteSpace(minifiedCss));
            Assert.IsFalse(string.IsNullOrWhiteSpace(prettyCss));

            Assert.IsTrue(minifiedCss.Length < prettyCss.Length, "hacks were not minified");
        }
    }
}
