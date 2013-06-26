// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResourcesResolutionActivityTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for ResourcesResolutionActivityTest and is intended
//   to contain all ResourcesResolutionActivityTest Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Tests
{
    using System.Diagnostics;
    using System.IO;
    using Activities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WebGrease.Tests;

    using WebGrease.Configuration;

    /// <summary>
    /// This is a test class for ResourcesResolutionActivityTest and is intended
    /// to contain all ResourcesResolutionActivityTest Unit Tests.
    /// </summary>
    [TestClass]
    public class ResourcesResolutionActivityTest
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for resources resolution.</summary>
        [TestMethod]
        [TestCategory(TestCategories.ResourcesResolutionActivity)]
        public void ResourceResolutionTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\ResourcesResolutionActivityTest\Input\Content");
            var destinationDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\ResourcesResolutionActivityTest\Output");
            var resourcesResolutionActivity = new ResourcesResolutionActivity(new WebGreaseContext(new WebGreaseConfiguration()))
            {
                SourceDirectory = sourceDirectory,
                ResourceGroupKey = Strings.ThemesResourcePivotKey,
                ApplicationDirectoryName = "App",
                SiteDirectoryName = "Site1",
                DestinationDirectory = Path.Combine(destinationDirectory, @"ToolsLogs\Resources\Themes")
            };

            resourcesResolutionActivity.ResourceKeys.Add("01-black");
            resourcesResolutionActivity.ResourceKeys.Add("02-red");
            resourcesResolutionActivity.Execute();

            Assert.IsTrue(Directory.Exists(resourcesResolutionActivity.DestinationDirectory));
            var themeFile = Path.Combine(resourcesResolutionActivity.DestinationDirectory, "01-black.resx");
            Assert.IsTrue(File.Exists(themeFile));
            var themeResources = ResourcesResolver.ReadResources(themeFile);
            
            Assert.IsTrue(themeResources.ContainsKey("feature_noverride"));
            string value;
            Assert.IsTrue(themeResources.TryGetValue("feature_noverride", out value));
            Assert.IsTrue(value == "feature_noverride");

            Assert.IsTrue(themeResources.ContainsKey("feature_overridebygenericatsite"));
            Assert.IsTrue(themeResources.TryGetValue("feature_overridebygenericatsite", out value));
            Assert.IsTrue(value == "feature_overridebygenericatsite_override");

            Assert.IsTrue(themeResources.ContainsKey("feature_overridebythemeatsite"));
            Assert.IsTrue(themeResources.TryGetValue("feature_overridebythemeatsite", out value));
            Assert.IsTrue(value == "feature_overridebythemeatsite_override");

            Assert.IsTrue(themeResources.ContainsKey("feature_overridebythemeinfeature"));
            Assert.IsTrue(themeResources.TryGetValue("feature_overridebythemeinfeature", out value));
            Assert.IsTrue(value == "feature_overridebythemeinfeature_override");
        }
    }
}
