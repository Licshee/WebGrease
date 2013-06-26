// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AssemblerActivityTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for AssemblerActivityTest and is intended
//   to contain all AssemblerActivityTest Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using Microsoft.Build.Framework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WebGrease.Tests;

    using Moq;

    using WebGrease.Build;
    using WebGrease.Configuration;
    using WebGrease.Extensions;
    using WebGrease.Css;
    using WebGrease.Css.Ast;
    using WebGrease.Css.Ast.MediaQuery;
    using WebGrease.Css.Extensions;

    /// <summary>
    /// This is a test class for AssemblerActivityTest and is intended
    /// to contain all AssemblerActivityTest Unit Tests
    /// </summary>
    [TestClass]
    public class WebGreaseTaskTests
    {
        /// <summary>A test for the everything build task.</summary>
        [TestMethod]
        [TestCategory(TestCategories.WebGreaseTask)]
        [TestCategory(TestCategories.EverythingActivity)]
        [TestCategory(TestCategories.MinifyCssActivity)]
        [TestCategory(TestCategories.MediaQueryMerge)]
        public void MsBuildTaskMergeMediaQueriesTest1()
        {
            var outputs = ExecuteEverythingBuildTask("MediaQueryMergeTest1");
            Assert.AreEqual(2, outputs.Values.Count);

            var negativeOutput = GetOutput(outputs, "mediaquerymergetest1negative.css");
            Assert.AreEqual(3, CountOccurences(negativeOutput, "@media screen{"));

            var output = GetOutput(outputs, "mediaquerymergetest1.css");
            Assert.IsFalse(string.IsNullOrWhiteSpace(output));

            Assert.AreEqual(1, CountOccurences(output, "@media screen{"));
            Assert.AreEqual(1, CountOccurences(output, "@media screen and (max-width:500px){"));
            Assert.AreEqual(1, CountOccurences(output, ".someclass{"));
            Assert.AreEqual(1, CountOccurences(output, ".someclass{color:blue}"));
        }

        private static string GetOutput(IDictionary<string, string> outputs, string outputName)
        {
            return outputs.Where(o => o.Key.EndsWith(outputName, StringComparison.OrdinalIgnoreCase)).Select(k => k.Value).FirstOrDefault();
        }

        [TestMethod]
        [TestCategory(TestCategories.WebGreaseTask)]
        [TestCategory(TestCategories.EverythingActivity)]
        [TestCategory(TestCategories.MinifyCssActivity)]
        [TestCategory(TestCategories.MediaQueryMerge)]
        public void MsBuildTaskMergeMediaQueriesTest2()
        {
            var outputs = ExecuteEverythingBuildTask("MediaQueryMergeTest2");
            Assert.AreEqual(1, outputs.Values.Count);
            var output = outputs.Values.FirstOrDefault();
            Assert.IsFalse(string.IsNullOrWhiteSpace(output));

            Assert.AreEqual(1, CountOccurences(output, "@media screen{"));
            Assert.AreEqual(1, CountOccurences(output, "@media screen and (min-width:896px) and (max-width:1791px){"));
            Assert.AreEqual(1, CountOccurences(output, "@media screen and (min-width:1792px){"));
            Assert.AreEqual(1, CountOccurences(output, "@media screen and (min-width:896px){"));
            Assert.AreEqual(1, CountOccurences(output, "@media screen and (max-width:895px){"));
        }

        /// <summary>A test for the everything build task.</summary>
        [TestMethod]
        [TestCategory(TestCategories.WebGreaseTask)]
        [TestCategory(TestCategories.EverythingActivity)]
        public void MsBuildTaskEverythingTest()
        {
            var outputs = ExecuteEverythingBuildTask("test1");
            Assert.AreEqual(1, outputs.Values.Count);
            var output = outputs.Values.FirstOrDefault();
            Assert.IsFalse(string.IsNullOrWhiteSpace(output));
            Assert.IsTrue(output.Contains("\r\n"));
        }

        /// <summary>The ms build task remove selectors.</summary>
        [TestMethod]
        [TestCategory(TestCategories.WebGreaseTask)]
        [TestCategory(TestCategories.EverythingActivity)]
        public void MsBuildTaskRemoveSelectors()
        {
            var errorEventArgs = new List<BuildErrorEventArgs>();
            var outputs = ExecuteEverythingBuildTask("test4", errorEventArgs);
            Assert.AreEqual(1, outputs.Values.Count);

            // Expected output: .purple{color:purple}.red{color:red}#toc .heading.slate,#toc .heading.slate a{font-size:10px}.bg.red,#toc .heading.slate,#toc .heading.slate a{font-size:12px}
            var output = outputs.Values.FirstOrDefault();

            Assert.IsFalse(string.IsNullOrWhiteSpace(output));

            Assert.IsFalse(output.Contains(".pink"));
            Assert.IsFalse(output.Contains(".green"));
            Assert.IsFalse(output.Contains(".blue"));

            Assert.IsTrue(output.Contains(".purple{"));
            Assert.IsTrue(output.Contains("color:purple"));
            Assert.IsTrue(output.Contains(".red{"));
            Assert.IsTrue(output.Contains("color:red"));
            Assert.IsTrue(output.Contains("#toc .heading.slate,#toc .heading.slate a{font-size:10px"));
            Assert.IsTrue(output.Contains("#toc .heading.slate,#toc .heading.slate a{font-size:12px"));
        }

        /// <summary>The ms build task sass error.</summary>
        [TestMethod]
        [TestCategory(TestCategories.WebGreaseTask)]
        [TestCategory(TestCategories.EverythingActivity)]
        [TestCategory(TestCategories.Preprocessing)]
        [TestCategory(TestCategories.Sass)]
        public void MsBuildTaskSassError()
        {
            var errorEventArgs = new List<BuildErrorEventArgs>();
            ExecuteBuildTask("Everything", @"WebGrease.Tests\WebGreaseTask\Test3", errorEventArgs.Add);

            var sassError = errorEventArgs.FirstOrDefault(eea => eea.Subcategory.Equals("Sass"));
            Assert.IsNotNull(sassError);

            Assert.AreEqual(2, sassError.LineNumber);
            Assert.AreEqual(2, sassError.EndLineNumber);
            Assert.AreEqual(0, sassError.ColumnNumber);
            Assert.AreEqual(0, sassError.EndColumnNumber);
            Assert.IsTrue(sassError.File.EndsWith("errorStylesheet.imports.scss", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(sassError.Message.Contains("SASS Syntax error"));
            Assert.IsTrue(sassError.Message.Contains("File to import not found or unreadable: vars.scss"));
        }

        /// <summary>The ms build task assembletest.</summary>
        [TestMethod]
        [TestCategory(TestCategories.WebGreaseTask)]
        [TestCategory(TestCategories.EverythingActivity)]
        public void MsBuildTaskAssembletest()
        {
            var outputFolder = ExecuteBuildTask("Bundle", @"WebGrease.Tests\WebGreaseTask\Test2");
            var outputFile1 = Path.Combine(outputFolder, "test2.css");

            Assert.IsTrue(File.Exists(outputFile1));

            var commentReplace = new Regex("/\\*.*?\\*/", RegexOptions.Singleline | RegexOptions.Compiled);
            var output1 = commentReplace.Replace(File.ReadAllText(outputFile1), string.Empty);

            Assert.IsTrue(output1.Contains("@media screen {"));
            Assert.IsTrue(output1.Contains(".someClass {"));
            Assert.IsTrue(output1.Contains("color: red"));
            Assert.IsTrue(output1.Contains(".asome {"));
        }

        /// <summary>The execute everything build task.</summary>
        /// <param name="testName">The test name.</param>
        /// <param name="errorEventArgs">The error event args.</param>
        /// <returns>The resulting css files.</returns>
        private static IDictionary<string, string> ExecuteEverythingBuildTask(string testName, List<BuildErrorEventArgs> errorEventArgs = null, string addedConfigName = null)
        {
            errorEventArgs = errorEventArgs ?? new List<BuildErrorEventArgs>();
            var testRootPath = @"WebGrease.Tests\WebGreaseTask\" + testName + @"\";
            ExecuteBuildTask("Everything", testRootPath, errorEventArgs.Add);


            Assert.AreEqual(0, errorEventArgs.Count);

            var log = XDocument.Load(testRootPath + @"Log\css_log.xml");
            var results = log.Descendants("File")
               .SelectMany(f => f.Elements("Input").ToDictionary(i => (string)i, i => File.ReadAllText(testRootPath + (string)f.Element("Output"))));

            var expectedOutputFile = testRootPath + "expectedOutput\\" + testName + ".css";
            if (File.Exists(expectedOutputFile))
            {
                var expectedOutput = File.ReadAllText(expectedOutputFile);
                var actualOutput = results.Where(r => r.Key.EndsWith(testName + ".css", StringComparison.OrdinalIgnoreCase)).Select(r => r.Value).FirstOrDefault();
                Assert.IsNotNull(actualOutput);

                var actualCss = CssParser.Parse(new WebGreaseContext(new WebGreaseConfiguration()), actualOutput);
                var expectedCss = CssParser.Parse(new WebGreaseContext(new WebGreaseConfiguration()), expectedOutput);

                EnsureExpectedCssResult(expectedCss.StyleSheetRules, actualCss);
            }

            return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private static void EnsureExpectedCssResult(IEnumerable<StyleSheetRuleNode> styleSheetRuleNodes, StyleSheetNode actualCss, MediaNode mediaQueryNode = null)
        {
            foreach (var styleSheetRuleNode in styleSheetRuleNodes)
            {
                if (styleSheetRuleNode is MediaNode)
                {
                    var mediaNode = styleSheetRuleNode as MediaNode;
                    EnsureExpectedCssResult(mediaNode.Rulesets, actualCss, mediaNode);
                }
                else if (styleSheetRuleNode is RulesetNode)
                {
                    var ruleSetNode = styleSheetRuleNode as RulesetNode;
                    foreach (var declarationNode in ruleSetNode.Declarations)
                    {
                        EnsureDeclarationNode(declarationNode, ruleSetNode, mediaQueryNode, actualCss);
                    }
                }
            }
        }

        private static void EnsureDeclarationNode(DeclarationNode declarationNode, RulesetNode ruleSetNode, MediaNode mediaQueryNode, StyleSheetNode actualCss)
        {
            var rules = actualCss.StyleSheetRules.ToArray();
            var expectedMediaQuery = string.Empty;
            if (mediaQueryNode != null)
            {
                expectedMediaQuery = mediaQueryNode.PrintSelector();
                rules = rules.OfType<MediaNode>().Where(r => r.PrintSelector().Equals(expectedMediaQuery)).SelectMany(mq => mq.Rulesets).ToArray();
            }

            var expectedRule = ruleSetNode.PrintSelector();
            var declarations = rules
                .OfType<RulesetNode>()
                .Where(rsn => rsn.PrintSelector().Equals(expectedRule))
                .SelectMany(r => r.Declarations).ToArray();

            var expectedproperty = declarationNode.Property;
            var declarationValues = declarations.Where(d => d.Property.Equals(expectedproperty)).ToArray();

            var expectedValue = declarationNode.ExprNode.TermNode.MinifyPrint();
            if (!declarationValues.Any(d => d.ExprNode.TermNode.MinifyPrint().Equals(expectedValue)))
            {
                Assert.Fail("Could not find [{0}] --> [{1}] --> {2}: {3}; ".InvariantFormat(expectedMediaQuery, expectedRule, expectedproperty, expectedValue));
            }
        }

        /// <summary>The execute build task.</summary>
        /// <param name="activity">The activity.</param>
        /// <param name="rootFolderForTest">The root folder for test.</param>
        /// <param name="errorAction">The error action.</param>
        /// <param name="configType">The config type.</param>
        /// <returns>The <see cref="string"/>.</returns>
        private static string ExecuteBuildTask(string activity, string rootFolderForTest, Action<BuildErrorEventArgs> errorAction = null, string configType = null)
        {
            var buildEngineMock = new Mock<IBuildEngine>();

            buildEngineMock
                .Setup(bem => bem.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback((BuildErrorEventArgs e) =>
                {
                    if (errorAction == null)
                    {
                        LogErrorEvent(e);
                    }
                    else
                    {
                        errorAction(e);
                    }
                });

            buildEngineMock
                .Setup(bem => bem.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()))
                .Callback((BuildMessageEventArgs e) => LogMessageEvent(e));

            buildEngineMock
                .Setup(bem => bem.LogWarningEvent(It.IsAny<BuildWarningEventArgs>()))
                .Callback((BuildWarningEventArgs e) => LogWarningEvent(e));

            buildEngineMock
                .Setup(bem => bem.LogCustomEvent(It.IsAny<CustomBuildEventArgs>()))
                .Callback((CustomBuildEventArgs e) => LogCustomEvent(e));

            var buildTask = new WebGreaseTask();
            buildTask.BuildEngine = buildEngineMock.Object;

            buildTask.Activity = activity;
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, rootFolderForTest);

            buildTask.ConfigurationPath = sourceDirectory;
            if (configType != null)
            {
                buildTask.ConfigType = configType;
            }

            buildTask.ApplicationRootPath = sourceDirectory;
            buildTask.RootInputPath = Path.Combine(sourceDirectory, "input");
            buildTask.RootOutputPath = Path.Combine(sourceDirectory, "output");
            buildTask.LogFolderPath = Path.Combine(sourceDirectory, "log");
            if (!Directory.Exists(buildTask.LogFolderPath))
            {
                Directory.CreateDirectory(buildTask.LogFolderPath);
            }
            if (!Directory.Exists(buildTask.RootOutputPath))
            {
                Directory.CreateDirectory(buildTask.RootOutputPath);
            }

            var result = buildTask.Execute();
            if (!result)
            {
                return null;
            }

            return buildTask.RootOutputPath;
        }

        /// <summary>The log custom event.</summary>
        /// <param name="e">The e.</param>
        private static void LogCustomEvent(CustomBuildEventArgs e)
        {
            Console.WriteLine("Custom :" + e.Message);
        }

        /// <summary>The log warning event.</summary>
        /// <param name="e">The e.</param>
        private static void LogWarningEvent(BuildWarningEventArgs e)
        {
            Console.WriteLine("Warning :" + e.Message);
        }

        /// <summary>The log message event.</summary>
        /// <param name="e">The e.</param>
        private static void LogMessageEvent(BuildMessageEventArgs e)
        {
            Console.WriteLine("Message :" + e.Message);
        }

        /// <summary>The log error event.</summary>
        /// <param name="e">The e.</param>
        private static void LogErrorEvent(BuildErrorEventArgs e)
        {
            Console.WriteLine("Error :" + e.Message);
        }

        private static int CountOccurences(string haystack, string needle)
        {
            if (haystack == null)
            {
                throw new ArgumentNullException("haystack");
            }
            if (needle == null)
            {
                throw new ArgumentNullException("needle");
            }
            return (haystack.Length - haystack.Replace(needle, "").Length) / needle.Length;
        }
    }
}
