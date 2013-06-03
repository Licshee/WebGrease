// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainActivityTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The web grease configuration root test.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Activities;
    using Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WebGrease.Tests;
    using WebGrease.Extensions;

    /// <summary>The web grease configuration root test.</summary>
    [TestClass]
    public class MainActivityTest
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for WebGrease Debug Integration</summary>
        [TestMethod]
        [TestCategory(TestCategories.EverythingActivity)]
        [TestCategory(TestCategories.WebGreaseTask)]
        public void MainActivityDebugIntegrationTest()
        {
            var testSourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\MainActivityTest");
            var configurationFile = Path.Combine(testSourceDirectory, @"Input\Integration\Debug\Configuration\sample1.webgrease.config");
            var sourceDirectory = Path.Combine(testSourceDirectory, @"Input\Integration\Debug\Content");
            var destinationDirectory = Path.Combine(testSourceDirectory, @"Output\Integration\Debug\sc");
            var logsDirectory = Path.Combine(testSourceDirectory, @"Output\Integration\Debug\logs");

            var webGreaseConfigurationRoot = new WebGreaseConfiguration(new FileInfo(configurationFile), "Debug", sourceDirectory, destinationDirectory, logsDirectory);
            var context = new WebGreaseContext(webGreaseConfigurationRoot, logInformation: null, logExtendedWarning: null, logError: LogError, logExtendedError: LogExtendedError);

            var mainActivity = new EverythingActivity(context);
            var success = mainActivity.Execute();

            Assert.IsTrue(success);
            VerifyStatics(destinationDirectory, logsDirectory);
        }

        private void LogExtendedError(string subcategory, string errorcode, string helpkeyword, string file, int? linenumber, int? columnnumber, int? endlinenumber, int? endcolumnnumber, string message)
        {
            Console.WriteLine("Error:" + new { subcategory, errorcode, helpkeyword, file, linenumber, columnnumber, endlinenumber, endcolumnnumber, message }.ToJson());
        }

        private static void LogError(Exception e, string m, string f)
        {
            Console.WriteLine(
                "File: {0},Message:{1}\r\nException:{2}\r\nInnerException:{3}",
                f,
                m,
                e != null
                    ? e.ToString()
                    : string.Empty,
                e != null && e.InnerException != null
                    ? e.InnerException.ToString()
                    : string.Empty
                );
        }

        /// <summary>The verify statics.</summary>
        private static void VerifyStatics(string destinationDirectory, string logsDirectory)
        {
            // Verify "sc" directory
            var sc = destinationDirectory;
            Assert.IsTrue(Directory.Exists(sc));

            // Verify tools logs
            var toolsLogs = logsDirectory;
            Assert.IsTrue(Directory.Exists(toolsLogs));

            // Verify tools temp
            var toolsTemp = Path.Combine(toolsLogs, "ToolsTemp");
            Assert.IsFalse(Directory.Exists(toolsTemp));

            // Verify the Assembled Statics happened in memory
            var staticAssemblerOutput = Path.Combine(toolsTemp, "StaticAssemblerOutput");
            Assert.IsFalse(Directory.Exists(staticAssemblerOutput));

            // Verify the JS resources expansion happened in memory
            var jsExpandResourcesOutput = Path.Combine(toolsTemp, "JSLocalizedOutput");
            Assert.IsFalse(Directory.Exists(jsExpandResourcesOutput));

            // Verify the css resources expansion happened in memory, should contain no css files
            var cssExpandResourcesOutput = Path.Combine(toolsTemp, "CssLocalizedOutput");
            Assert.IsFalse(Directory.Exists(cssExpandResourcesOutput));

            // Verify the logs
            Assert.IsTrue(File.Exists(Path.Combine(toolsLogs, "css_log.xml")));
            Assert.IsTrue(File.Exists(Path.Combine(toolsLogs, "js_log.xml")));
            Assert.IsTrue(File.Exists(Path.Combine(toolsLogs, "images_log.xml")));

            // Happens in memory
            Assert.IsFalse(Directory.Exists(Path.Combine(toolsLogs, "ToolsTemp", "Resources")));

            //// Verify generated statics
            Assert.IsTrue(Directory.Exists(Path.Combine(sc, "css")));
            Assert.IsTrue(Directory.Exists(Path.Combine(sc, "js")));
            Assert.IsTrue(Directory.Exists(Path.Combine(sc, "i")));
        }
    }
}
