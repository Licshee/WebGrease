// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebGreaseConfigurationTests.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The web grease configuration tests.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WebGrease.Tests;
    using WebGrease.Extensions;

    /// <summary>The web grease configuration root test.</summary>
    [TestClass]
    public class WebGreaseConfigurationTests
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        [TestMethod]
        [TestCategory(TestCategories.Configuration)]
        public void ConfigSourceTest()
        {
            var configurationFile = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\WebGreaseConfigurationRootTest\ConfigSource\configsource.webgrease.config");
            var configDirectory = Path.GetDirectoryName(configurationFile);
            var config = new WebGreaseConfiguration(new FileInfo(configurationFile), null, configDirectory, configDirectory, configDirectory);

            // global1.config
            Assert.IsTrue(config.ImageDirectories.All(id => id.EndsWith("global1imagedirectories")));
            Assert.IsTrue(config.ImageExtensions.Contains("global1imageextensions"));

            // global2.config
            Assert.AreEqual(config.TokensDirectory, "global2tokendirectory");

            // global3.config
            Assert.AreEqual(config.OverrideTokensDirectory, "global3overridetokendirectory");

            // css fileset 1 - settings\css1.config
            var cssfileset1 = config.CssFileSets[0];
            Assert.IsNotNull(cssfileset1);
            Assert.AreEqual(2, cssfileset1.Themes.Count());
            Assert.IsTrue(cssfileset1.Themes.Contains("red"));
            Assert.IsTrue(cssfileset1.Themes.Contains("blue"));
            Assert.AreEqual(2, cssfileset1.Locales.Count());
            Assert.IsTrue(cssfileset1.Locales.Contains("en-us"));
            Assert.IsTrue(cssfileset1.Locales.Contains("fr-ca"));
            
            var debugMinification = cssfileset1.Minification.GetNamedConfig("deBUG");
            var defaultMinification = cssfileset1.Minification.GetNamedConfig();
            Assert.AreEqual(debugMinification, defaultMinification);
            Assert.IsNotNull(debugMinification);
            Assert.AreEqual("Debug", debugMinification.Name);
            Assert.AreEqual(false, debugMinification.ShouldValidateLowerCase);

            var releaseMinification = cssfileset1.Minification.GetNamedConfig("release");
            Assert.AreNotEqual(debugMinification, releaseMinification);
            Assert.AreEqual(true, releaseMinification.ShouldValidateLowerCase);

            var defaultSpriting = cssfileset1.ImageSpriting.GetNamedConfig();
            Assert.AreEqual(string.Empty, defaultSpriting.Name);
            Assert.AreEqual(false, defaultSpriting.ShouldAutoSprite);

            var releaseSpriting = cssfileset1.ImageSpriting.GetNamedConfig("Release");
            Assert.AreEqual("Release", releaseSpriting.Name);
            Assert.AreEqual(true, releaseSpriting.ShouldAutoSprite);

            // css fileset 2 - settings\css2.config
            var cssfileset2 = config.CssFileSets.FirstOrDefault(fs => fs.Output.Equals("cssfileset2.css"));
            Assert.IsNotNull(cssfileset2);

            Assert.AreEqual(2, cssfileset2.Themes.Count());
            Assert.IsTrue(cssfileset2.Themes.Contains("themecss2a"));
            Assert.IsTrue(cssfileset2.Themes.Contains("themecss2b"));

            Assert.AreEqual(2, cssfileset2.Locales.Count());
            Assert.IsTrue(cssfileset2.Locales.Contains("localecss2a"));
            Assert.IsTrue(cssfileset2.Locales.Contains("localecss2b"));

            var debugPreprocessing = cssfileset2.Preprocessing.GetNamedConfig("debug");
            Assert.IsNotNull(debugPreprocessing);
            Assert.IsNotNull(debugPreprocessing.Element);
            Assert.AreEqual(debugPreprocessing.Element.Elements("Sass").Attributes("sourceMaps").Select(s => (bool?)s).FirstOrDefault(), true);
            Assert.AreEqual("Debug", debugPreprocessing.Name);
            Assert.AreEqual(1, debugPreprocessing.PreprocessingEngines.Count);
            Assert.AreEqual("sass", debugPreprocessing.PreprocessingEngines[0]);
            Assert.AreEqual(true, debugPreprocessing.Enabled);

            var releasePreprocessing = cssfileset2.Preprocessing.GetNamedConfig("REleasE");
            Assert.IsNotNull(releasePreprocessing);
            Assert.IsNotNull(releasePreprocessing.Element);
            Assert.IsNull(releasePreprocessing.Element.Elements("Sass").Attributes("sourceMaps").Select(s => (bool?)s).FirstOrDefault());
            Assert.AreEqual("Release", releasePreprocessing.Name);
            Assert.AreEqual(1, releasePreprocessing.PreprocessingEngines.Count);
            Assert.AreEqual("sass", releasePreprocessing.PreprocessingEngines[0]);
            Assert.AreEqual(true, releasePreprocessing.Enabled);

            // js fileset 1 - settings\css1.config
            var jsfileset1 = config.JSFileSets.FirstOrDefault(fs => fs.Output.Equals("jsfileset1.js"));
            Assert.IsNotNull(jsfileset1);

            Assert.AreEqual(2, jsfileset1.Locales.Count());
            Assert.IsTrue(jsfileset1.Locales.Contains("jsinclude1"));
            Assert.IsTrue(jsfileset1.Locales.Contains("jsinclude2"));

            var debugJsMinification = jsfileset1.Minification.GetNamedConfig("DEBUG");
            Assert.IsNotNull(debugJsMinification);
            Assert.AreEqual("Debug", debugJsMinification.Name);
            Assert.AreEqual("jQuery;Msn", debugJsMinification.GlobalsToIgnore);

            var releaseJsMinification = jsfileset1.Minification.GetNamedConfig("release");
            Assert.IsNotNull(releaseJsMinification);
            Assert.AreEqual("Release", releaseJsMinification.Name);
            Assert.AreEqual("jQuery2;Msn2", releaseJsMinification.GlobalsToIgnore);
        }

        /// <summary>A test for WebGrease Configuration from an xml file</summary>
        [TestMethod]
        [TestCategory(TestCategories.Configuration)]
        public void WebGreaseConfigurationTest()
        {
            var configurationFile = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\WebGreaseConfigurationRootTest\Input\Debug\Configuration\sample1.webgrease.config");
            var configDirectory = Path.GetDirectoryName(configurationFile);
            var webGreaseConfigurationRoot = new WebGreaseConfiguration(new FileInfo(configurationFile), null, configDirectory, configDirectory, configDirectory);
            Assert.IsNotNull(webGreaseConfigurationRoot);

            var imageDirectories = webGreaseConfigurationRoot.ImageDirectories;
            Assert.IsNotNull(imageDirectories);
            Assert.IsTrue(imageDirectories.Count == 2);
            Assert.IsTrue(imageDirectories[0].EndsWith(@"f1\i"));
            Assert.IsTrue(imageDirectories[1].EndsWith(@"f2\i"));

            var imageExtensions = webGreaseConfigurationRoot.ImageExtensions;
            Assert.IsNotNull(imageExtensions);
            Assert.IsTrue(imageExtensions.Count == 5);
            Assert.IsTrue(imageExtensions[0] == "png");
            Assert.IsTrue(imageExtensions[1] == "jpg");
            var tokensDir = webGreaseConfigurationRoot.TokensDirectory;
            Assert.AreEqual(tokensDir, "App", "TokensDirectory should be 'App' but was '{0}'", tokensDir);
            var tokenOverrideDir = webGreaseConfigurationRoot.OverrideTokensDirectory;
            Assert.AreEqual(tokenOverrideDir, "Site1", "OverrideTokensDirectory should be 'Site1' but was '{0}'", tokenOverrideDir);

            var cssFileSets = webGreaseConfigurationRoot.CssFileSets;
            Assert.IsNotNull(cssFileSets);
            Assert.AreEqual(2, cssFileSets.Count, "should be 2 CssFileSet objects");

            var cssSet1 = cssFileSets[0];
            var minificationConfigs = cssSet1.Minification;
            Assert.IsNotNull(minificationConfigs);
            Assert.IsTrue(minificationConfigs.Count == 2);
            Assert.IsTrue(minificationConfigs.ContainsKey("DeBuG"));
            Assert.IsTrue(minificationConfigs.ContainsKey("RELEASE"));

            var minifyConfig = minificationConfigs["relEASE"];
            Assert.IsNotNull(minifyConfig);
            Assert.IsTrue(minifyConfig.Name == "Release");
            Assert.IsTrue(minifyConfig.ShouldMinify);

            var spriteConfiguration = cssSet1.ImageSpriting["RElease"];
            Assert.IsTrue(spriteConfiguration.ShouldAutoSprite);
            Assert.IsTrue(spriteConfiguration.ShouldAutoVersionBackgroundImages);

            var locales = cssSet1.Locales;
            Assert.IsNotNull(locales);
            Assert.IsTrue(locales.Count == 2);
            Assert.IsTrue(locales[0] == "en-us");
            Assert.IsTrue(locales[1] == "fr-ca");

            var themes = cssSet1.Themes;
            Assert.IsNotNull(themes);
            Assert.IsTrue(themes.Count == 2);
            Assert.IsTrue(themes[0] == "red");
            Assert.IsTrue(themes[1] == "blue");

            var inputs = cssSet1.InputSpecs;
            Assert.IsNotNull(inputs);
            Assert.AreEqual(3, inputs.Count);
            var input = inputs[2];
            Assert.AreEqual("*_mobile.css", input.SearchPattern, "search pattern should be '*_mobile.css'.");
            Assert.AreEqual(SearchOption.TopDirectoryOnly, input.SearchOption, "search option should be 'TopDirectoryOnly'.");
            Assert.IsTrue(input.Path.EndsWith(@"content\css"), "path should be 'content/css'.");

            // now the js portion
            var jsFileSets = webGreaseConfigurationRoot.JSFileSets;
            Assert.IsNotNull(jsFileSets);
            Assert.AreEqual(1, jsFileSets.Count, "should be 1 JsFileSet object");
            var jsSet1 = jsFileSets[0];
            var jsConfigurations = jsSet1.Minification;
            Assert.IsNotNull(jsConfigurations);
            Assert.AreEqual(2, jsConfigurations.Count);
            Assert.IsTrue(jsConfigurations.ContainsKey("DEBUG"));
            Assert.IsTrue(jsConfigurations.ContainsKey("RELEASE"));
            var jsConfig = jsConfigurations["RELEASE"];
            Assert.IsNotNull(jsConfig);
            var jsGlobalsToIgnore = jsConfig.GlobalsToIgnore.Split(';');
            Assert.IsNotNull(jsGlobalsToIgnore);
            Assert.IsTrue(jsGlobalsToIgnore.Length == 2);
            Assert.IsTrue(jsGlobalsToIgnore[0] == "jQuery");
            Assert.IsTrue(jsGlobalsToIgnore[1] == "Msn");
        }

        /// <summary>
        /// A test for setting up WebGrease configuration via CLI parameters
        /// </summary>
        [TestMethod]
        [TestCategory(TestCategories.CommandLine)]
        public void WebGreaseArgumentConfigTest1()
        {
            // setup args
            var args = new[] { "-m", @"-in:C:\temp", @"-out:c:\output.js" };

            WebGreaseConfiguration config;
            var mode = Program.GenerateConfiguration(args, out config);

            Assert.IsTrue(mode == ActivityMode.Minify);
            Assert.IsTrue(config.JSFileSets.Count == 1);
            Assert.IsTrue(config.JSFileSets[0].InputSpecs.Count == 1);
            Assert.IsTrue(config.CssFileSets[0].InputSpecs.Count == 0);
            Assert.AreEqual(@"C:\temp", config.JSFileSets[0].InputSpecs[0].Path);
        }

        /// <summary>
        /// A test for setting up WebGrease configuration via CLI parameters
        /// </summary>
        [TestMethod]
        [TestCategory(TestCategories.CommandLine)]
        public void WebGreaseArgumentConfigTest2()
        {
            // setup args
            var args = new[] { "-b", @"-in:C:\temp", @"-out:c:\output.css", @"-images:c:\images" };

            WebGreaseConfiguration config;

            var mode = Program.GenerateConfiguration(args, out config);

            // only 1 mode at a time is supported on the CLI
            Assert.IsTrue(mode == ActivityMode.Bundle);

            Assert.IsTrue(config.ImageDirectories.Count == 1);
            Assert.AreEqual(@"c:\images", config.ImageDirectories[0]);
            Assert.IsTrue(config.CssFileSets.Count == 1);
            Assert.IsTrue(config.JSFileSets[0].InputSpecs.Count == 0);
            Assert.IsTrue(config.CssFileSets[0].InputSpecs.Count == 1);
            Assert.AreEqual(@"C:\temp", config.CssFileSets[0].InputSpecs[0].Path);
        }

        /// <summary>
        /// A test for setting up WebGrease configuration via CLI parameters
        /// </summary>
        [TestMethod]
        [TestCategory(TestCategories.CommandLine)]
        public void WebGreaseArgumentConfigTest3()
        {
            // setup args
            var args = new[] { "-a", @"-in:C:\temp", @"-out:C:\output", @"-images:C:\images" };

            WebGreaseConfiguration config;
            var mode = Program.GenerateConfiguration(args, out config);

            Assert.IsTrue(mode == ActivityMode.AutoName);

            Assert.IsTrue(config.ImageDirectories.Count == 1);
            Assert.AreEqual(@"C:\images", config.ImageDirectories[0]);
            Assert.IsTrue(config.CssFileSets.Count == 1);
            Assert.IsTrue(config.JSFileSets.Count == 1);
            Assert.AreEqual(@"C:\temp", config.CssFileSets[0].InputSpecs[0].Path);
            Assert.AreEqual(@"C:\temp", config.JSFileSets[0].InputSpecs[0].Path);
            Assert.AreEqual(@"C:\output", config.CssFileSets[0].Output);
            Assert.AreEqual(@"C:\output", config.CssFileSets[0].Output);
        }

        [TestMethod]
        [TestCategory(TestCategories.Configuration)]
        public void WebGreaseConfigurationDefaults()
        {
            // parse the configuration file
            var configurationFile = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\WebGreaseConfigurationRootTest\Input\Debug\Configuration\withDefaults.webgrease.config");
            var configDirectory = Path.GetDirectoryName(configurationFile);
            var webGreaseConfigurationRoot = new WebGreaseConfiguration(new FileInfo(configurationFile), null, configDirectory, configDirectory, configDirectory);
            Assert.IsNotNull(webGreaseConfigurationRoot);

            // there should be two CSS file sets and two JS file sets
            Assert.IsTrue(webGreaseConfigurationRoot.CssFileSets.Count == 2);
            Assert.IsTrue(webGreaseConfigurationRoot.JSFileSets.Count == 2);

            ValidateCssFileSet(webGreaseConfigurationRoot.CssFileSets[0],
                new[] { "en-us", "en-ca", "fr-ca", "en-gb" },
                new[] { "Red", "Orange", "Yellow", "Green", "Blue", "Violet" },
                new CssMinificationConfig { Name = "Retail", ShouldMinify = true, ShouldValidateLowerCase = true, ForbiddenSelectors = new[] { "body" } },
                new CssMinificationConfig { Name = "Debug", ShouldMinify = false, ShouldValidateLowerCase = false, ForbiddenSelectors = new string[] { } });
            ValidateCssFileSet(webGreaseConfigurationRoot.CssFileSets[1],
                new[] { "zh-sg", "zh-tw", "zh-hk" },
                new[] { "Pink", "Green" },
                new CssMinificationConfig { Name = "Retail", ShouldMinify = false, ShouldValidateLowerCase = false, ForbiddenSelectors = new string[] { } },
                null);

            ValidateJsFileSet(webGreaseConfigurationRoot.JSFileSets[0],
                new[] { "en-us", "en-ca", "fr-ca", "en-gb" },
                new JsMinificationConfig { Name = "Retail", ShouldMinify = true, GlobalsToIgnore = "jQuery;$;define", MinificationArugments = "-evals:safe -fnames:lock" },
                new JsMinificationConfig { Name = "Debug", ShouldMinify = false, GlobalsToIgnore = "FooBar", MinificationArugments = string.Empty });
            ValidateJsFileSet(webGreaseConfigurationRoot.JSFileSets[1],
                new[] { "es-es", "es-mx", "es-ar" },
                new JsMinificationConfig { Name = "Retail", ShouldMinify = false, GlobalsToIgnore = string.Empty, MinificationArugments = string.Empty },
                null);
        }

        private static void ValidateCssFileSet(CssFileSet fileSet, string[] locales, string[] themes, CssMinificationConfig retailConfig, CssMinificationConfig debugConfig)
        {
            // if the debug config is null, there should only be the retail config
            Assert.IsTrue(fileSet.Minification.Count == (debugConfig == null ? 1 : 2));
            ValidateListIsSame(fileSet.Locales, locales);
            ValidateListIsSame(fileSet.Themes, themes);

            var retail = fileSet.Minification["Retail"];
            Assert.IsNotNull(retail);
            ValidateCssMinificationConfig(retail, retailConfig);

            CssMinificationConfig debug;
            if (!fileSet.Minification.TryGetValue("Debug", out debug))
            {
                debug = null;
            }

            if (debugConfig != null)
            {
                Assert.IsNotNull(debug);
                ValidateCssMinificationConfig(debug, debugConfig);
            }
            else
            {
                Assert.IsNull(debug);
            }
        }

        private static void ValidateCssMinificationConfig(CssMinificationConfig actual, CssMinificationConfig shouldBe)
        {
            Assert.AreEqual(actual.Name, shouldBe.Name);
            Assert.AreEqual(actual.ShouldMinify, shouldBe.ShouldMinify);
            Assert.AreEqual(actual.ShouldValidateLowerCase, shouldBe.ShouldValidateLowerCase);
            ValidateListIsSame(actual.ForbiddenSelectors, shouldBe.ForbiddenSelectors);
        }

        private static void ValidateJsFileSet(JSFileSet fileSet, string[] locales, JsMinificationConfig retailConfig, JsMinificationConfig debugConfig)
        {
            // if the debug config is null, there should only be the retail config
            Assert.IsTrue(fileSet.Minification.Count == (debugConfig == null ? 1 : 2));
            ValidateListIsSame(fileSet.Locales, locales);

            var retail = fileSet.Minification["Retail"];
            Assert.IsNotNull(retail);

            JsMinificationConfig debug;
            if (!fileSet.Minification.TryGetValue("Debug", out debug))
            {
                debug = null;
            }

            if (debugConfig != null)
            {
                Assert.IsNotNull(debug);
            }
            else
            {
                Assert.IsNull(debug);
            }
        }

        private static void ValidateListIsSame(IEnumerable<string> actual, IEnumerable<string> shouldBe)
        {
            // get enumerators for the two lists and reset them
            var actualEnumerator = actual.GetEnumerator();
            var shouldBeEnumerator = shouldBe.GetEnumerator();
            actualEnumerator.Reset();
            shouldBeEnumerator.Reset();

            for (var ndx = 0; actualEnumerator.MoveNext(); ++ndx)
            {
                // as long as we keep getting another item for the actual, we should also be getting
                // another item for the should-be
                Assert.IsTrue(shouldBeEnumerator.MoveNext());

                // and they should both be equal
                Assert.AreEqual(actualEnumerator.Current, shouldBeEnumerator.Current);
            }

            // we pop out of the list when the actual is done -- the should-be better be done too
            Assert.IsFalse(shouldBeEnumerator.MoveNext());
        }
    }
}
