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
    using Activities;
    using Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        public void MainActivityDebugIntegrationTest()
        {
            var testSourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\MainActivityTest");
            var configurationFile = Path.Combine(testSourceDirectory, @"Input\Integration\Debug\Configuration\sample1.webgrease.config");
            var sourceDirectory = Path.Combine(testSourceDirectory, @"Input\Integration\Debug\Content");
            var destinationDirectory = Path.Combine(testSourceDirectory, @"Output\Integration\Debug\sc");
            var logsDirectory = Path.Combine(testSourceDirectory, @"Output\Integration\Debug\logs");

            var webGreaseConfigurationRoot = new WebGreaseConfiguration(configurationFile, "Debug", sourceDirectory, destinationDirectory, logsDirectory);
            var context = new WebGreaseContext(webGreaseConfigurationRoot, null, null, (e, m, f) => Console.WriteLine("File: {0},Message:{1}\r\nException:{2}", f, m, e.InnerException.Message));
            
            var mainActivity = new EverythingActivity(context);
            var success = mainActivity.Execute();

            Assert.IsTrue(success);
            VerifyStatics(destinationDirectory, logsDirectory);
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
            Assert.IsTrue(Directory.Exists(toolsTemp));

            // Verify the Assembled Statics
            var staticAssemblerOutput = Path.Combine(toolsTemp, "StaticAssemblerOutput");
            Assert.IsTrue(Directory.Exists(staticAssemblerOutput));

            // Verify the JS resources expansion
            var jsExpandResourcesOutput = Path.Combine(toolsTemp, "JSLocalizedOutput");
            Assert.IsTrue(Directory.Exists(jsExpandResourcesOutput));

            // Verify the css resources expansion
            var cssExpandResourcesOutput = Path.Combine(toolsTemp, "CssLocalizedOutput");
            Assert.IsTrue(Directory.Exists(cssExpandResourcesOutput));

            // Verify the logs
            var cssLog = Path.Combine(toolsLogs, "css_log.xml");
            Assert.IsTrue(File.Exists(cssLog));
            var jsLog = Path.Combine(toolsLogs, "js_log.xml");
            Assert.IsTrue(File.Exists(Path.Combine(toolsLogs, jsLog)));
            var imagesLog = Path.Combine(toolsLogs, "images_log.xml");
            Assert.IsTrue(File.Exists(Path.Combine(toolsLogs, imagesLog)));
            Assert.IsTrue(Directory.Exists(Path.Combine(toolsLogs, "ToolsTemp", "Resources")));

            //// Verify generated statics
            Assert.IsTrue(Directory.Exists(Path.Combine(sc, "css")));
            Assert.IsTrue(Directory.Exists(Path.Combine(sc, "js")));
            Assert.IsTrue(Directory.Exists(Path.Combine(sc, "i")));
        }
    }
}
