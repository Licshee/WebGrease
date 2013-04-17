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
    using Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>The web grease configuration root test.</summary>
    [TestClass]
    public class WebGreaseConfigurationTests
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for WebGrease Configuration from an xml file</summary>
        [TestMethod]
        public void WebGreaseConfigurationTest()
        {
            var configurationFile = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\WebGreaseConfigurationRootTest\Input\Debug\Configuration\sample1.webgrease.config");
            var configDirectory = Path.GetDirectoryName(configurationFile);
            var webGreaseConfigurationRoot = new WebGreaseConfiguration(configurationFile, null, configDirectory, configDirectory, configDirectory);
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
        public void WebGreaseArgumentConfigTest2()
        {
            // setup args
            var args = new[] { "-b", @"-in:C:\temp", @"-out:c:\output.css", @"-images:c:\images" };

            WebGreaseConfiguration config;

            string type;
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
        public void WebGreaseArgumentConfigTest3()
        {
            // setup args
            var args = new[] { "-a", @"-in:C:\temp", @"-out:C:\output", @"-images:C:\images" };

            WebGreaseConfiguration config;
            string type;
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
        public void WebGreaseConfigurationDefaults()
        {
            // parse the configuration file
            var configurationFile = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\WebGreaseConfigurationRootTest\Input\Debug\Configuration\withDefaults.webgrease.config");
            var configDirectory = Path.GetDirectoryName(configurationFile);
            var webGreaseConfigurationRoot = new WebGreaseConfiguration(configurationFile, null, configDirectory, configDirectory, configDirectory);
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

        private void ValidateCssFileSet(CssFileSet fileSet, string[] locales, string[] themes, CssMinificationConfig retailConfig, CssMinificationConfig debugConfig)
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

        private void ValidateCssMinificationConfig(CssMinificationConfig actual, CssMinificationConfig shouldBe)
        {
            Assert.AreEqual(actual.Name, shouldBe.Name);
            Assert.AreEqual(actual.ShouldMinify, shouldBe.ShouldMinify);
            Assert.AreEqual(actual.ShouldValidateLowerCase, shouldBe.ShouldValidateLowerCase);
            ValidateListIsSame(actual.ForbiddenSelectors, shouldBe.ForbiddenSelectors);
        }

        private void ValidateJsFileSet(JSFileSet fileSet, string[] locales, JsMinificationConfig retailConfig, JsMinificationConfig debugConfig)
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

        private void ValidateJsMinificationConfig(JsMinificationConfig actual, JsMinificationConfig shouldBe)
        {
            Assert.AreEqual(actual.Name, shouldBe.Name);
            Assert.AreEqual(actual.ShouldMinify, shouldBe.ShouldMinify);
            Assert.AreEqual(actual.MinificationArugments, shouldBe.MinificationArugments);
            Assert.AreEqual(actual.GlobalsToIgnore, shouldBe.GlobalsToIgnore);
        }

        private void ValidateListIsSame(IEnumerable<string> actual, IEnumerable<string> shouldBe)
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
