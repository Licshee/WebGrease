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
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using Microsoft.Build.Framework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using WebGrease.Build;

    /// <summary>
    /// This is a test class for AssemblerActivityTest and is intended
    /// to contain all AssemblerActivityTest Unit Tests
    /// </summary>
    [TestClass]
    public class WebGreaseTaskTests
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for the everything build task.</summary>
        [TestMethod]
        public void MsBuildTaskEverythingTest()
        {
            var outputs = ExecuteEverythingBuildTask("test1");
            Assert.AreEqual(1, outputs.Values.Count);
            var output = outputs.Values.FirstOrDefault();
            Assert.IsFalse(string.IsNullOrWhiteSpace(output));
            Assert.IsTrue(output.Contains("\r\n"));
        }

        [TestMethod]
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

        [TestMethod]
        public void MsBuildTaskSassError()
        {
            var errorEventArgs = new List<BuildErrorEventArgs>();
            ExecuteBuildTask("Everything", @"WebGrease.Tests\WebGreaseTask\Test3", errorEventArgs.Add);

            Assert.AreEqual(4, errorEventArgs.Count);
            Assert.AreEqual(2, errorEventArgs[0].LineNumber);
            Assert.AreEqual(2, errorEventArgs[0].EndLineNumber);
            Assert.AreEqual(0, errorEventArgs[0].ColumnNumber);
            Assert.AreEqual(0, errorEventArgs[0].EndColumnNumber);
            Assert.AreEqual("Sass", errorEventArgs[0].Subcategory);
            Assert.IsTrue(errorEventArgs[0].File.EndsWith("errorStylesheet.scss"));
            Assert.IsTrue(errorEventArgs[0].Message.Contains("SASS Syntax error"));
            Assert.IsTrue(errorEventArgs[0].Message.Contains("File to import not found or unreadable: vars.scss"));
        }

        [TestMethod]
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

        private static Dictionary<string, string> ExecuteEverythingBuildTask(string testName, List<BuildErrorEventArgs> errorEventArgs = null)
        {
            errorEventArgs = errorEventArgs ?? new List<BuildErrorEventArgs>();
            var testRootPath = @"WebGrease.Tests\WebGreaseTask\" + testName + @"\";
            ExecuteBuildTask("Everything", testRootPath, errorEventArgs.Add);
            Assert.AreEqual(0, errorEventArgs.Count);

            var log = XDocument.Load(testRootPath + @"Log\css_log.xml");
            var outputs = log.Descendants("Output");
            return outputs.Select(o => (string)o).ToDictionary(o => o, o => File.ReadAllText(testRootPath + o));
        }

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

        private static void LogCustomEvent(CustomBuildEventArgs e)
        {
            Console.WriteLine("Custom :" + e.Message);
        }

        private static void LogWarningEvent(BuildWarningEventArgs e)
        {
            Console.WriteLine("Warning :" + e.Message);
        }

        private static void LogMessageEvent(BuildMessageEventArgs e)
        {
            Console.WriteLine("Message :" + e.Message);
        }

        private static void LogErrorEvent(BuildErrorEventArgs e)
        {
            Console.WriteLine("Error :" + e.Message);
        }
    }
}
