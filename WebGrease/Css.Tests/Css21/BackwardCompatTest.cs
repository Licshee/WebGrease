// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BackwardCompatTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for backward compatibility and is intended
//   to contain all backward compatibility unit tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Css.Tests.Css21
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.Ajax.Utilities.Css;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestSuite;
    using WebGrease.Css;
    using WebGrease.Css.Ast;
    using WebGrease.Css.Extensions;
    using WebGrease.Css.Visitor;

    /// <summary>
    ///   This is a test class for backward compatibility and is intended
    ///   to contain all backward compatibility unit tests
    /// </summary>
    [TestClass]
    public class BackwardCompatTest
    {
        /// <summary>
        /// A test for site parsing
        /// </summary>
        [TestMethod]
        public void MinifyComparisionTest()
        {
            var directoryName = Path.Combine(TestDeploymentPaths.TestDirectory, @"css.tests\css21");
            var directoryInfo = new DirectoryInfo(directoryName);
            foreach (var fileInfo in directoryInfo.EnumerateFiles("*.css", SearchOption.AllDirectories))
            {
                ////
                //// Old Css parser
                ////
                StylesheetNode oldStyleSheetNode;
                var oldCssParser = new Parser();
                using (var streamReader = new StreamReader(fileInfo.FullName))
                {
                    oldStyleSheetNode = oldCssParser.Parse(streamReader);
                }

                var oldMinifiedCss = oldStyleSheetNode.AjaxMinPrint().Replace(" 0%", " 0"); // This is new optimization.

                ////
                //// New Css parser
                ////
                var newStyleSheetNode = CssParser.Parse(new FileInfo(fileInfo.FullName), false);
                newStyleSheetNode = (StyleSheetNode)newStyleSheetNode.Accept(new FloatOptimizationVisitor());
                newStyleSheetNode = (StyleSheetNode)newStyleSheetNode.Accept(new ColorOptimizationVisitor());
                var newMinifiedCss = newStyleSheetNode.MinifyPrint();

                if (string.Compare(oldMinifiedCss, newMinifiedCss) != 0)
                {
                    Trace.WriteLine("Old Css:");
                    Trace.WriteLine(oldMinifiedCss);
                    Trace.WriteLine("New Css:");
                    Trace.WriteLine(newMinifiedCss);

                    throw new Exception("Comparison failed.");
                }
            }
        }
    }
}
