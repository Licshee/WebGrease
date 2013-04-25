// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JSLocalizationActivityTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for JSLocalizationActivityTest and is intended
//   to contain all JSLocalizationActivityTest Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Tests
{
    using System.IO;
    using Activities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WebGrease.Configuration;

    /// <summary>
    /// This is a test class for JSLocalizationActivityTest and is intended
    /// to contain all JSLocalizationActivityTest Unit Tests
    /// </summary>
    [TestClass]
    public class JSLocalizationActivityTest
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for JS localization.</summary>
        [TestMethod]
        public void JSLocalizationTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\JSLocalizationActivityTest");
            var jsLocalizationActivity = new JSLocalizationActivity(new WebGreaseContext(new WebGreaseConfiguration())) { DestinationDirectory = Path.Combine(sourceDirectory, "Output"), ResourcesDirectory = Path.Combine(sourceDirectory, @"Input\ToolsLogs\Resources\Locales") };
            var jsLocalizationInput = new JSLocalizationInput { SourceFile = Path.Combine(sourceDirectory, @"input\input1.js"), DestinationFile = "input1" };
            jsLocalizationInput.Locales.Add("en-us");
            jsLocalizationActivity.JsLocalizationInputs.Add(jsLocalizationInput);
            jsLocalizationActivity.Execute();

            // Assertions
            var outputFilePath = Path.Combine(sourceDirectory, @"Output\en-us\input1.js");
            Assert.IsTrue(File.Exists(outputFilePath));
            var text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(text));
            Assert.IsTrue(text.Contains("両極端？山田優vs綾瀬はるかのジャージ対決の意味 男女の理想のプロポーズはどんなセリフか"));
            Assert.IsTrue(text.Contains("1JSValue"));
            Assert.IsTrue(text.Contains("2JSValue"));
        }
    }
}
