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
    using System.IO;
    using System.Xml.Linq;

    using Activities;
    using Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WebGrease.Tests;

    using WebGrease.Preprocessing;

    /// <summary>
    /// This is a test class for AssemblerActivityTest and is intended
    /// to contain all AssemblerActivityTest Unit Tests
    /// </summary>
    [TestClass]
    public class AssemblerActivityTest
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for only files in inputs.</summary>
        [TestMethod]
        [TestCategory(TestCategories.AssemblerActivity)]
        [TestCategory(TestCategories.Preprocessing)]
        [TestCategory(TestCategories.Sass)]
        public void WithPreprocessorFiles()
        {
            var preprocessingConfig = new PreprocessingConfig(XElement.Parse("<Preprocessing><Engines>sass</Engines></Preprocessing>"));

            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\AssemblerActivityTest\");
            var assemblerActivity = new AssemblerActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            assemblerActivity.PreprocessingConfig = preprocessingConfig;

            assemblerActivity.Inputs.Add(new InputSpec { Path = Path.Combine(sourceDirectory, @"Input\Case4\Stylesheet1.scss") });
            assemblerActivity.Inputs.Add(new InputSpec { Path = Path.Combine(sourceDirectory, @"Input\Case4\Stylesheet2.css") });
            assemblerActivity.OutputFile = Path.Combine(sourceDirectory, @"Output\Case4\case4.css");
            assemblerActivity.Execute();

            // Assertions
            var outputFilePath = assemblerActivity.OutputFile;
            Assert.IsTrue(File.Exists(outputFilePath));
            var text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(text));
            Assert.IsTrue(text.Contains("Stylesheet1.scss */"));
            Assert.IsTrue(text.Contains("font-size: %MetroSdk.BaseFontSize%;"));
            Assert.IsTrue(text.Contains("@media screen and (min-width: %MetroSdk.Mq.MinWidth%) and (max-width: %MetroSdk.Mq.MaxWidth%) {"));
            Assert.IsTrue(text.Contains("Stylesheet2.css */"));
            Assert.IsTrue(text.Contains(".asome {\r\n    color: blue;\r\n}"));
        }

        /// <summary>A test for only files in inputs.</summary>
        [TestMethod]
        [TestCategory(TestCategories.AssemblerActivity)]
        [TestCategory(TestCategories.JavaScript)]
        public void OnlyFiles()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\AssemblerActivityTest");
            var assemblerActivity = new AssemblerActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            assemblerActivity.Inputs.Add(new InputSpec { Path = Path.Combine(sourceDirectory, @"Input\Case1\Script1.js") });
            assemblerActivity.Inputs.Add(new InputSpec { Path = Path.Combine(sourceDirectory, @"Input\Case1\Script2.js") });
            assemblerActivity.OutputFile = Path.Combine(sourceDirectory, @"Output\Case1\all.js");
            assemblerActivity.Execute();

            // Assertions
            var outputFilePath = assemblerActivity.OutputFile;
            Assert.IsTrue(File.Exists(outputFilePath));
            var text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(text));
            Assert.IsTrue(text.Contains("var name = \"script1.js\";"));
            Assert.IsTrue(text.Contains("var name = \"script2.js\";"));
        }

        /// <summary>A test for only directories in inputs.</summary>
        [TestMethod]
        [TestCategory(TestCategories.AssemblerActivity)]
        [TestCategory(TestCategories.JavaScript)]
        public void OnlyDirectories()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\AssemblerActivityTest");
            var assemblerActivity = new AssemblerActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            assemblerActivity.Inputs.Add(new InputSpec { Path = Path.Combine(sourceDirectory, @"Input\Case2\a") });
            assemblerActivity.Inputs.Add(new InputSpec { Path = Path.Combine(sourceDirectory, @"Input\Case2\b") });
            assemblerActivity.OutputFile = Path.Combine(sourceDirectory, @"Output\Case2\all.js");
            assemblerActivity.Execute();

            // Assertions
            var outputFilePath = assemblerActivity.OutputFile;
            Assert.IsTrue(File.Exists(outputFilePath));
            var text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(text));
            Assert.IsTrue(text.Contains("var name = \"script1.js\";"));
            Assert.IsTrue(text.Contains("var name = \"script2.js\";"));
            Assert.IsTrue(text.Contains("var name = \"script3.js\";"));
        }

        /// <summary>A test for directories with wild cards in inputs.</summary>
        [TestMethod]
        [TestCategory(TestCategories.AssemblerActivity)]
        [TestCategory(TestCategories.JavaScript)]
        public void DirectoriesWithWildCards()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\AssemblerActivityTest");
            var assemblerActivity = new AssemblerActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            assemblerActivity.Inputs.Add(new InputSpec { Path = Path.Combine(sourceDirectory, @"Input\Case3\a"), SearchPattern = "script*.js" });
            assemblerActivity.Inputs.Add(new InputSpec { Path = Path.Combine(sourceDirectory, @"Input\Case3\b"), SearchPattern = "script*.js" });
            assemblerActivity.Inputs.Add(new InputSpec { Path = Path.Combine(sourceDirectory, @"Input\Case3\d"), SearchPattern = "script*.js", SearchOption = SearchOption.TopDirectoryOnly });
            assemblerActivity.OutputFile = Path.Combine(sourceDirectory, @"Output\Case3\all.js");
            assemblerActivity.Execute();

            // Assertions
            var outputFilePath = assemblerActivity.OutputFile;
            Assert.IsTrue(File.Exists(outputFilePath));
            var text = File.ReadAllText(outputFilePath);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(text));
            Assert.IsTrue(text.Contains("var name = \"script1.js\";"));
            Assert.IsTrue(text.Contains("var name = \"script2.js\";"));
            Assert.IsTrue(text.Contains("var name = \"script3.js\";"));
            Assert.IsFalse(text.Contains("var name = \"script4.js\";"));
            Assert.IsFalse(text.Contains("var name = \"script5.js\";"));
            Assert.IsTrue(text.Contains("var name = \"script6.js\";"));
        }
    }
}
