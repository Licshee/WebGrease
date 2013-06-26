using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.WebGrease.Tests
{
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using global::WebGrease;
    using global::WebGrease.Configuration;
    using global::WebGrease.Css;
    using global::WebGrease.Css.Ast;
    using global::WebGrease.Css.Extensions;
    using global::WebGrease.Css.Visitor;
    using global::WebGrease.Tests;

    [TestClass]
    public class CloningTests
    {
        [TestMethod]
        [TestCategory(TestCategories.Cloning)]
        public void CloningPerf1()
        {
            var cssFile = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\CloningTests\landingPage.tmx.pc.ms.css");
            var css = File.ReadAllText(cssFile);
            StyleSheetNode stylesheetNode = null;
            StyleSheetNode stylesheetNode2 = null;
            var loopCount = 100;

            var timer = DateTimeOffset.Now;
            var webGreaseContext = new WebGreaseContext(new WebGreaseConfiguration());
            Parallel.For(0, loopCount, i =>
                {
                    var ss = CssParser.Parse(webGreaseContext, css, false);
                    if (loopCount - 1 == i)
                    {
                        stylesheetNode = ss;
                    }
                });

            var timeSpent1 = DateTimeOffset.Now - timer;

            timer = DateTimeOffset.Now;
            Parallel.For(0, loopCount, i =>
                {
                    var ss = stylesheetNode.Accept(new NodeTransformVisitor()) as StyleSheetNode;
                    if (loopCount - 1 == i)
                    {
                        Assert.AreNotEqual(ss, stylesheetNode2);
                        Assert.AreNotEqual(ss, stylesheetNode);
                        stylesheetNode2 = ss;
                    }
                });

            var timeSpent2 = DateTimeOffset.Now - timer;

            Assert.AreEqual(stylesheetNode2.MinifyPrint(), stylesheetNode.MinifyPrint());
            Assert.IsTrue(timeSpent2.TotalMilliseconds < timeSpent1.TotalMilliseconds / 5);
            Trace.WriteLine(string.Format("For {0} Runs, Parsed: {1}ms, Cloned: {2}ms", loopCount, timeSpent1.TotalMilliseconds, timeSpent2.TotalMilliseconds));
        }
    }
}