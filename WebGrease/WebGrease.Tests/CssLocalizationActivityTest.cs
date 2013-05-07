// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CssLocalizationActivityTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for CssLocalizationActivityTest and is intended
//   to contain all CssLocalizationActivityTest Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Tests
{
    using System.IO;
    using Activities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WebGrease.Configuration;

    /// <summary>
    /// This is a test class for CssLocalizationActivityTest and is intended
    /// to contain all CssLocalizationActivityTest Unit Tests
    /// </summary>
    [TestClass]
    public class CssLocalizationActivityTest
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for Css localization with no theme.</summary>
        [TestMethod]
        public void CssLocalizationNoThemeTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\CssLocalizationActivityTest");
            var cssLocalizationActivity = new CssLocalizationActivity(new WebGreaseContext(new WebGreaseConfiguration())) { DestinationDirectory = Path.Combine(sourceDirectory, "Output"), LocalesResourcesDirectory = Path.Combine(sourceDirectory, @"Input\ToolsLogs\Resources\Locales"), ThemesResourcesDirectory = Path.Combine(sourceDirectory, @"Input\ToolsLogs\Resources\Locales") };
            var cssLocalizationInput = new CssLocalizationInput { SourceFile = Path.Combine(sourceDirectory, @"input\input1.css"), DestinationFile = "input1" };
            cssLocalizationInput.Locales.Add("en-us");
            cssLocalizationActivity.CssLocalizationInputs.Add(cssLocalizationInput);
            cssLocalizationActivity.Execute();

            // Assertions
            var outputFilePath = Path.Combine(sourceDirectory, @"Output\en-us\generic-generic_input1.css");
            Assert.IsTrue(File.Exists(outputFilePath));
            var text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(text));
            Assert.IsTrue(text.Contains("両極端？山田優vs綾瀬はるかのジャージ対決の意味 男女の理想のプロポーズはどんなセリフか"));
            Assert.IsTrue(text.Contains("1CSSValue"));
            Assert.IsTrue(text.Contains("2CSSValue"));
        }

        /// <summary>A test for Css localization with no locale.</summary>
        [TestMethod]
        public void CssLocalizationNoLocaleTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\CssLocalizationActivityTest");
            var cssLocalizationActivity = new CssLocalizationActivity(new WebGreaseContext(new WebGreaseConfiguration())) { DestinationDirectory = Path.Combine(sourceDirectory, "Output"), LocalesResourcesDirectory = Path.Combine(sourceDirectory, @"Input\ToolsLogs\Resources\Locales"), ThemesResourcesDirectory = Path.Combine(sourceDirectory, @"Input\ToolsLogs\Resources\Locales") };
            var cssLocalizationInput = new CssLocalizationInput { SourceFile = Path.Combine(sourceDirectory, @"input\input1.css"), DestinationFile = "input1" };
            cssLocalizationActivity.CssLocalizationInputs.Add(cssLocalizationInput);
            cssLocalizationActivity.Execute();

            // Assertions
            var outputFilePath = Path.Combine(sourceDirectory, @"Output\generic-generic\generic-generic_input1.css");
            Assert.IsTrue(File.Exists(outputFilePath));
            var text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(text));
        }

        /// <summary>A test for Css localization.</summary>
        [TestMethod]
        public void CssLocalizationTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\CssLocalizationActivityTest");
            var cssLocalizationActivity = new CssLocalizationActivity(new WebGreaseContext(new WebGreaseConfiguration())) { DestinationDirectory = Path.Combine(sourceDirectory, "Output"), LocalesResourcesDirectory = Path.Combine(sourceDirectory, @"Input\ToolsLogs\Resources\Locales"), ThemesResourcesDirectory = Path.Combine(sourceDirectory, @"Input\ToolsLogs\Resources\Locales"), HashedImagesLogFile = Path.Combine(sourceDirectory, @"input\imagesLog.xml") };
            var cssLocalizationInput = new CssLocalizationInput { SourceFile = Path.Combine(sourceDirectory, @"input\input1.css"), DestinationFile = "input1" };
            cssLocalizationInput.Locales.Add("en-us");
            cssLocalizationInput.Locales.Add("fr-ca");
            cssLocalizationInput.Themes.Add("red");
            cssLocalizationInput.Themes.Add("blue");
            cssLocalizationActivity.CssLocalizationInputs.Add(cssLocalizationInput);
            cssLocalizationActivity.Execute();

            // Assertions
            var outputFilePath = Path.Combine(sourceDirectory, @"Output\en-us\red_input1.css");
            Assert.IsTrue(File.Exists(outputFilePath));
            var text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(text));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(text));
            Assert.IsTrue(text.Contains("両極端？山田優vs綾瀬はるかのジャージ対決の意味 男女の理想のプロポーズはどんなセリフか"));
            Assert.IsTrue(text.Contains("1CSSValue"));
            Assert.IsTrue(text.Contains("2CSSValue"));
        }
    }
}
