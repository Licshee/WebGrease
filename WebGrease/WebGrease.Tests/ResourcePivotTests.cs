namespace Microsoft.WebGrease.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using global::WebGrease;
    using global::WebGrease.Activities;
    using global::WebGrease.Configuration;
    using global::WebGrease.Tests;

    [TestClass]
    public class ResourcePivotTests
    {
        [TestMethod]
        [TestCategory(TestCategories.Configuration)]
        [TestCategory(TestCategories.ResourcePivots)]
        public void ResourcePivotConfigurationTest()
        {
            var configuration = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
                    "<WebGrease><Settings>  <ResourcePivot key=\"locales\" applyMode=\"CssApplyAfterParse\">ja-jp;th-th;zh-sg;generic-generic</ResourcePivot>" +
                    "<ResourcePivot key=\"themes\" applyMode=\"CssApplyAfterParse\">red;blue;orange;green;purple</ResourcePivot>" +
                    "<Dpi>1;1.4;1.5;1.8;2;2.25;2.4</Dpi></Settings></WebGrease>";

            var tmpFile = Path.GetTempFileName();
            File.WriteAllText(tmpFile, configuration);
            var configurationFile = new FileInfo(tmpFile);
            var wgc = new WebGreaseConfiguration(configurationFile, configurationFile.DirectoryName, configurationFile.DirectoryName, configurationFile.DirectoryName, configurationFile.DirectoryName);

            Assert.AreEqual(1, wgc.DefaultDpi.Count);
            Assert.AreEqual(string.Empty, wgc.DefaultDpi.Keys.ElementAt(0));

            var dpis = wgc.DefaultDpi.Values.ElementAt(0);
            Assert.AreEqual(7f, dpis.Count);
            Assert.AreEqual(1.4f, dpis.ElementAtOrDefault(1));
            Assert.AreEqual(2.25f, dpis.ElementAtOrDefault(5));
            Assert.AreEqual(2.4f, dpis.ElementAtOrDefault(6));

            Assert.AreEqual(2, wgc.DefaultJsResourcePivots.Count());
            Assert.AreEqual("locales", wgc.DefaultJsResourcePivots.ElementAtOrDefault(0).Key);
            Assert.AreEqual(4, wgc.DefaultJsResourcePivots["locales"].Keys.Count);
            Assert.AreEqual("themes", wgc.DefaultJsResourcePivots.ElementAtOrDefault(1).Key);
            Assert.AreEqual(5, wgc.DefaultJsResourcePivots["themes"].Keys.Count);
        }

        [TestMethod]
        [TestCategory(TestCategories.Configuration)]
        [TestCategory(TestCategories.ResourcePivots)]
        public void ResourcePivotConfigurationLegacyTest()
        {
            var configuration = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
                    "<WebGrease><Settings>  <Locales>ja-jp;th-th;zh-sg;generic-generic</Locales>" +
                    "<Themes>red;blue;orange;green;purple</Themes>" +
                    "</Settings></WebGrease>";

            var tmpFile = Path.GetTempFileName();
            File.WriteAllText(tmpFile, configuration);
            var configurationFile = new FileInfo(tmpFile);
            var wgc = new WebGreaseConfiguration(configurationFile, configurationFile.DirectoryName, configurationFile.DirectoryName, configurationFile.DirectoryName, configurationFile.DirectoryName);

            Assert.AreEqual(1, wgc.DefaultJsResourcePivots.Count());
            Assert.AreEqual(2, wgc.DefaultCssResourcePivots.Count());
            Assert.AreEqual("locales", wgc.DefaultCssResourcePivots.ElementAtOrDefault(0).Key);
            Assert.AreEqual(4, wgc.DefaultCssResourcePivots["locales"].Keys.Count);

            Assert.AreEqual("themes", wgc.DefaultCssResourcePivots.ElementAtOrDefault(1).Key);
            Assert.AreEqual(5, wgc.DefaultCssResourcePivots["themes"].Keys.Count);
        }

        [TestMethod]
        [TestCategory(TestCategories.ResourcePivots)]
        [TestCategory(TestCategories.EverythingActivity)]
        public void ResourcePivotResourcesTmxSdkTest1()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\ResourcesResolutionActivityTest\ResourcePivotTest1");

            LogExtendedError logExtendedError = (subcategory, code, keyword, file, number, columnNumber, lineNumber, endColumnNumber, message) => Assert.Fail(message);
            var errors = new List<string>();
            Action<string> logErrorMessage = errors.Add;
            LogError logError = (exception, message, name) => errors.Add(message);

            var start = DateTimeOffset.UtcNow;
            var webGreaseConfiguration = new WebGreaseConfiguration(
                new FileInfo(Path.Combine(sourceDirectory, "LandingPage.xml")),
                "Release",
                sourceDirectory,
                Path.Combine(sourceDirectory, "output1"),
                Path.Combine(sourceDirectory, "logs1"),
                Path.Combine(sourceDirectory, "temp1"),
                sourceDirectory);

            var webGreaseContext = new WebGreaseContext(webGreaseConfiguration, null, null, logExtendedError, logErrorMessage, logError);
            new EverythingActivity(webGreaseContext).Execute();
            var time1 = DateTimeOffset.UtcNow - start;
            start = DateTimeOffset.UtcNow;

            var webGreaseConfiguration2 = new WebGreaseConfiguration(
                new FileInfo(Path.Combine(sourceDirectory, "LandingPage2.xml")),
                "Release",
                sourceDirectory,
                Path.Combine(sourceDirectory, "output2"),
                Path.Combine(sourceDirectory, "logs2"),
                Path.Combine(sourceDirectory, "temp2"),
                sourceDirectory);

            var webGreaseContext2 = new WebGreaseContext(webGreaseConfiguration2, null, null, logExtendedError, logErrorMessage, logError);
            new EverythingActivity(webGreaseContext2).Execute();

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    Trace.WriteLine("Error: " + error);
                }

                Assert.Fail("Errors occurred, see test output for details.");
            }

            var time2 = DateTimeOffset.UtcNow - start;

            Trace.WriteLine(string.Format("Parsed: {0}ms, Cloned: {1}ms", time1.TotalMilliseconds, time2.TotalMilliseconds));

            Func<string, string> logFileChange = logFileContent => logFileContent.Replace("/output1/", "/output2/");
            CacheTests.DirectoryMatch(Path.Combine(sourceDirectory, "output1"), Path.Combine(sourceDirectory, "output2"), logFileChange);
            CacheTests.DirectoryMatch(Path.Combine(sourceDirectory, "output2"), Path.Combine(sourceDirectory, "output1"), logFileChange);
        }
    }
}
