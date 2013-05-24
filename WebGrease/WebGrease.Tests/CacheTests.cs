using System;

namespace Microsoft.WebGrease.Tests
{
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using Microsoft.Build.Framework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using global::WebGrease;
    using global::WebGrease.Activities;
    using global::WebGrease.Build;
    using global::WebGrease.Extensions;
    using global::WebGrease.Tests;

    [TestClass]
    public class CacheTests
    {
        [TestMethod]
        [TestCategory(TestCategories.Caching)]
        [TestCategory(TestCategories.WebGreaseTask)]
        [TestCategory(TestCategories.EverythingActivity)]
        public void CacheCleanUpTest()
        {
            // TODO: Cache clean up tests.
        }

        [TestMethod]
        [TestCategory(TestCategories.Caching)]
        [TestCategory(TestCategories.WebGreaseTask)]
        [TestCategory(TestCategories.EverythingActivity)]
        public void CacheUpdateTest()
        {
            var testRoot = GetTestRoot(@"WebGrease.Tests\PerformanceTests\Test1");
            const string ConfigType = "Release";
            const string TaskName = "EVERYTHING";

            var inputRoot = Path.Combine(testRoot, "input");
            var inputJsRoot = Path.Combine(inputRoot, "js");
            var inputCssRoot = Path.Combine(inputRoot, "css");
            var inputImagesRoot = Path.Combine(inputRoot, "images");
            var inputResxRoot = Path.Combine(inputRoot, "resources");

            var outputscss1 = string.Empty;
            var outputjs1 = string.Empty;

            var allPreExecute = new Action<WebGreaseTask>(wgt =>
                {
                    wgt.Measure = true;
                    wgt.RootOutputPath = Path.Combine(wgt.ApplicationRootPath, "output");
                    wgt.CacheRootPath = Path.Combine(wgt.ApplicationRootPath, "cache");
                    wgt.CacheEnabled = true;
                });

            var allPreExecute2 = new Action<WebGreaseTask>(wgt =>
                {
                    wgt.Measure = true;
                    wgt.RootOutputPath = Path.Combine(wgt.ApplicationRootPath, "output2");
                    wgt.CacheRootPath = Path.Combine(wgt.ApplicationRootPath, "cache");
                    wgt.CacheEnabled = true;
                });

            // Ensure logo is there, initially using the msn blue logo.
            var msnLogoMobile = Path.Combine(inputImagesRoot, "MSN_Logo_Mobile.png");
            var msnLogoBlueMobile = Path.Combine(inputImagesRoot, "MSN_Logo_Blue_Mobile.png");
            File.Copy(msnLogoBlueMobile, msnLogoMobile);

            // ----------------------------------------------------------------------------------------
            // First clean run, everything should be hit and everything should output.
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute, buildTask =>
                    {
                        var dgmlFiles = Directory.GetFiles(buildTask.RootOutputPath, "*.dgml");
                        foreach (string dgmlFile in dgmlFiles)
                        {
                            var fi = new FileInfo(dgmlFile);
                            File.Copy(dgmlFile, new DirectoryInfo(buildTask.RootOutputPath + "..\\..\\..\\..\\..\\..\\..\\").FullName + fi.Name, true);
                        }

                        Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.Preprocessing, SectionIdParts.Process, "Sass"), "Sass should be run");
                        Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting), "Pre-cache run should be spriting");
                        Assert.IsTrue(HasExecuted(buildTask, 1, SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting, SectionIdParts.Assembly), "Pre-cache run should only be assembling once, since the image are the same for all themes/locales.");
                        Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Optimize), "Pre-cache run should be optimizing");
                        Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.CssFileSet), "Pre-cache  run should have css filesets");
                        Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Pre-cache  run should have js filesets");
                        Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting, SectionIdParts.Assembly), "Should be doing sprite assembling");
                        
                        Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyJsActivity));
                        Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Parse));

                        // Css
                        var outputscss = outputscss1 = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                        Assert.IsTrue(outputscss.Contains(".imports1scss"));
                        Assert.IsTrue(outputscss.Contains(".test1importsscss"));
                        Assert.IsTrue(outputscss.Contains(".test1include"));
                        Assert.IsTrue(outputscss.Contains(".test1scss"));
                        Assert.IsTrue(outputscss.Contains(".include1"));
                        Assert.IsTrue(outputscss.Contains(".tokentest{background-image:url('locale:generic-generic'),url('theme:theme1')"));
                        Assert.IsTrue(outputscss.Contains(".sprite{background:transparent url(../../i/8e/6ab5cb2d7c3ae73a540ecb5b6b0231.png)"));
                        Assert.IsTrue(outputscss.Contains(".nosprite2{background:transparent url('/images/nosprite2.png')"));
                        Assert.IsTrue(outputscss.Contains(".nosprite4{background:transparent url(/images/nosprite4.png)"));

                        var hashedcssfile = GetOutputFile(buildTask, "css", "test1", "generic-generic", "Theme2");
                        var outputscsstheme2 = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme2");
                        Assert.IsTrue(outputscsstheme2.Contains(".tokentest{background-image:url('locale:generic-generic'),url('theme:theme2')"));
                        var outputscsstheme2Enca = GetOutputContent(buildTask, "css", "test1", "en-ca", "Theme2");
                        Assert.IsTrue(outputscsstheme2Enca.Contains(".tokentest{background-image:url('locale:en-ca'),url('theme:theme2')"));

                        var hashedcssfilePath = new FileInfo(hashedcssfile).DirectoryName;

                        // Img
                        var hashedImageData1 = GetHashedFileContent(buildTask, "images", "/images/nosprite1.png", hashedcssfilePath);
                        Assert.IsNotNull(hashedImageData1);

                        var hashedImageData3 = GetHashedFileContent(buildTask, "images", "/images/nosprite3.png", hashedcssfilePath);
                        Assert.IsNotNull(hashedImageData3);

                        outputjs1 = GetOutputContent(buildTask, "js", "test1", "generic-generic");
                        Assert.IsTrue(outputjs1.Contains("function test1a()"));
                        Assert.IsTrue(outputjs1.Contains("function test1b()"));
                        Assert.IsTrue(outputjs1.Contains("function test1ainclude()"));
                        Assert.IsTrue(outputjs1.Contains("function include1()"));
                    });

            // ----------------------------------------------------------------------------------------
            // Second fully cached run, nothing should happen
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute, buildTask =>
            {
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.Preprocessing, SectionIdParts.Process, "Sass"), "Sass should not be run");
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.CssFileSet), "Second run should do nothing at all, but has processed a css file set.");
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Second run should do nothing at all, but has processed a js file set.");
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyJsActivity, SectionIdParts.Process));
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Parse));
                var outputscss2 = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                var outputjs2 = GetOutputContent(buildTask, "js", "test1", "generic-generic");

                // Verify outputs are the same after doing an incremental build.
                Assert.AreEqual(outputscss1, outputscss2);
                Assert.AreEqual(outputjs1, outputjs2);
            });

            // ----------------------------------------------------------------------------------------
            // Second fully cached run, should be getting from cache
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute2, buildTask =>
            {
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.Preprocessing, SectionIdParts.Process, "Sass"));
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyJsActivity, SectionIdParts.Process));
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Parse));

                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.CssFileSet));
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.JsFileSet));
                var outputscss2 = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                var outputjs2 = GetOutputContent(buildTask, "js", "test1", "generic-generic");

                // Verify outputs are the same after doing an incremental build.
                Assert.AreEqual(outputscss1, outputscss2);
                Assert.AreEqual(outputjs1, outputjs2);
            });

            Func<string, string> logFileChange = logFileContent => logFileContent.Replace("/output2/", "/output/");
            DirectoryMatch(Path.Combine(testRoot, "output"), Path.Combine(testRoot, "output2"), logFileChange);
            DirectoryMatch(Path.Combine(testRoot, "output2"), Path.Combine(testRoot, "output"), logFileChange);

            // ----------------------------------------------------------------------------------------
            // Nothing should happen again
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute2, buildTask =>
            {
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.Preprocessing, SectionIdParts.Process, "Sass"));
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.CssFileSet));
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.JsFileSet));
                var outputscss2 = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                var outputjs2 = GetOutputContent(buildTask, "js", "test1", "generic-generic");

                // Verify outputs are the same after doing an incremental build.
                Assert.AreEqual(outputscss1, outputscss2);
                Assert.AreEqual(outputjs1, outputjs2);
            });

            DirectoryMatch(Path.Combine(testRoot, "output"), Path.Combine(testRoot, "output2"), logFileChange);
            DirectoryMatch(Path.Combine(testRoot, "output2"), Path.Combine(testRoot, "output"), logFileChange);

            // ----------------------------------------------------------------------------------------
            // Update one of the css source files and make sure it regenerates.
            var test1Cssfile = Path.Combine(inputCssRoot, "test1.css");
            File.WriteAllText(test1Cssfile, File.ReadAllText(test1Cssfile) + "\r\n.addedcss1{ color: blue; }");
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute, buildTask =>
            {
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.Preprocessing, SectionIdParts.Process, "Sass"), "Sass should not be run");
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting, SectionIdParts.Assembly), "Should sprite analyzing");
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting), "Should sprite analyzing");
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.CssFileSet), "Should do css since we have updated a source file");
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Should do nothing at all for js.");
                var outputscss = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                Assert.IsTrue(outputscss.Contains(".addedcss1"));
            });


            // ----------------------------------------------------------------------------------------
            // Change one of the sprited images and see if it runs again
            var msnLogoGreenMobile = Path.Combine(inputImagesRoot, "MSN_Logo_green_Mobile.png");
            File.Copy(msnLogoGreenMobile, msnLogoMobile, true);
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute, buildTask =>
            {
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.Preprocessing, SectionIdParts.Process, "Sass"), "Sass should not be run");
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting, SectionIdParts.Assembly), "Should be spriting again for the new image.");
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Should do nothing at all for js.");
                var outputscss = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                Assert.IsFalse(outputscss.Contains(".sprite{background:transparent url(../../i/8e/6ab5cb2d7c3ae73a540ecb5b6b0231.png)"));
                Assert.IsTrue(outputscss.Contains(".sprite{background:transparent url(../../i/90/24488e832e97d18f5b9a6e432bf389.png)"));
                Assert.IsTrue(outputscss.Contains(".nosprite1{background:transparent url(../../i/b6/dd31595c78921c95d1f9838b3c8d6d.png)"));
            });

            // ----------------------------------------------------------------------------------------
            // Change one of the hashed images and see if it runs again
            var img1 = Path.Combine(inputImagesRoot, "nosprite1.png");
            var img2 = Path.Combine(inputImagesRoot, "nosprite2.png");

            File.Copy(img2, img1, true);
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute, buildTask =>
            {
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.Preprocessing, SectionIdParts.Process, "Sass"), "Sass should not be run");
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting, SectionIdParts.Assembly), "Should not be spriting again for the new image.");
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.ImageHash), "Should be hashing again for the new image.");
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Should do nothing at all for js.");
                var outputscss = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                Assert.IsTrue(outputscss.Contains(".nosprite1{background:transparent url(../../i/0d/1d3d5aa1d7fb8b2d7bc23e9655b886.png)"));
            });

            // ----------------------------------------------------------------------------------------
            // Change one of the theme resx files and make sure it reruns css and check if it has picked up the value
            var csstheme1Resxfile = Path.Combine(inputResxRoot, "css", "themes", "theme1.resx");
            File.WriteAllText(csstheme1Resxfile, File.ReadAllText(csstheme1Resxfile).Replace("<value>theme1</value>", "<value>theme1.updated</value>"));
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute, buildTask =>
            {
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity), "Should be spriting again for the new image.");
                var outputscss = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                Assert.IsTrue(outputscss.Contains(".tokentest{background-image:url('locale:generic-generic'),url('theme:theme1.updated')"));
            });

            // ----------------------------------------------------------------------------------------
            // Change one of the locale resx files and make sure it reruns css
            var cssgenericgenericresxfile = Path.Combine(inputResxRoot, "css", "locales", "generic-generic.resx");
            File.WriteAllText(cssgenericgenericresxfile, File.ReadAllText(cssgenericgenericresxfile).Replace("<value>generic-generic</value>", "<value>generic-generic.updated</value>"));
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute, buildTask =>
            {
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity), "Should be css minifying again for the changed css.");
                var outputscss = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                Assert.IsTrue(outputscss.Contains(".tokentest{background-image:url('locale:generic-generic.updated'),url('theme:theme1.updated')"));
            });

            // SASS
            // ----------------------------------------------------------------------------------------
            // Add something to a indirectly included file using sass import and verify it updates
            var test1Scssfile = Path.Combine(inputCssRoot, "test1.import.scss");
            File.WriteAllText(test1Scssfile, File.ReadAllText(test1Scssfile) + "\r\n.addedscss1{ color: green; }");
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute, buildTask =>
            {
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.Preprocessing, SectionIdParts.Process, "Sass"), "Sass should run");
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity), "Should be running minifycss again for changed sass.");
                var outputscss = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                Assert.IsTrue(outputscss.Contains(".addedscss1"));
            });

            // ----------------------------------------------------------------------------------------
            // Remove a indirectly included file using sass import and verify it updates
            /* Currently unsupported scenario, re-add when we enable multiple "library" paths for sass.
            File.Delete(test1Scssfile);
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute, buildTask =>
            {
                Assert.IsTrue(HasExecuted(buildTask, TimeMeasureNames.MinifyCssActivity), "Should be running minifycss again for changed sass.");
                var outputscss = GetOutput(buildTask, "css", "test1", "generic-generic", "Theme1");
                Assert.IsFalse(outputscss.Contains(".addedscss1"));
                Assert.IsFalse(outputscss.Contains(".test1importsscss"));
            });
             * */

            // ----------------------------------------------------------------------------------------
            // Add a file to a indirectly included folder using sass imports and verify it updates
            var test1ImportsScssfile = Path.Combine(inputCssRoot, "imports", "imports2.scss");
            File.WriteAllText(test1ImportsScssfile, ".imports2scss{color:purple}");
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute, buildTask =>
            {
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.Preprocessing, SectionIdParts.Process, "Sass"), "Sass should run");
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity), "Should be running minifycss again for changed sass.");
                var outputscss = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                Assert.IsTrue(outputscss.Contains(".imports2scss"));
            });

            // ----------------------------------------------------------------------------------------
            // Remove a file from an indirectly included file using sass import and verify it updates
            File.Delete(test1ImportsScssfile);
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute, buildTask =>
            {
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.Preprocessing, SectionIdParts.Process, "Sass"), "Sass should run");
                Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity), "Should not be running minifycss again since it is the same as it was before we added it (2 actions above).");
                var outputscss = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                Assert.IsFalse(outputscss.Contains(".imports2scss"));
            });

            // WGINCLUDE
            // ----------------------------------------------------------------------------------------
            // Add something to a indirectly included file using wginclude to a direct file and verify it updates
            var test1IncludeCssfile = Path.Combine(inputCssRoot, "test1.include.css");
            File.WriteAllText(test1IncludeCssfile, ".test1includecss2a{color:yellow}");
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute, buildTask =>
            {
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.Preprocessing, SectionIdParts.Process, "WgInclude"), "Sass should run");
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity), "Should be running minifycss again for changed sass.");
                var outputscss = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                Assert.IsTrue(outputscss.Contains(".test1includecss2a"));
            });

            // Add something to a indirectly included file using wginclude with a folder mask and verify it updates
            var test1IncludeCssfile2 = Path.Combine(inputCssRoot, "include", "include1.css");
            File.WriteAllText(test1IncludeCssfile2, ".test1includecssfile2{color:yellow}");
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute, buildTask =>
            {
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.Preprocessing, SectionIdParts.Process, "WgInclude"), "Sass should run");
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity), "Should be running minifycss again for changed sass.");
                var outputscss = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                Assert.IsTrue(outputscss.Contains(".test1includecssfile2"));
            });

            // ----------------------------------------------------------------------------------------
            // Add something to a indirectly included folder using wginclude and verify it updates
            var test1IncludeCssfile3 = Path.Combine(inputCssRoot, "include", "include2.css");
            File.WriteAllText(test1IncludeCssfile3, ".test1includecssfile3{color:yellow}");
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute, buildTask =>
            {
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.Preprocessing, SectionIdParts.Process, "WgInclude"), "Sass should run");
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity), "Should be running minifycss again for changed sass.");
                var outputscss = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                Assert.IsTrue(outputscss.Contains(".test1includecssfile3"));
            });

            // ----------------------------------------------------------------------------------------
            // Delete it from the inincluded folder using wginclude and verify it updates
            File.Delete(test1IncludeCssfile3);
            File.Delete(test1IncludeCssfile2);
            ExecuteBuildTask(TaskName, testRoot, ConfigType, allPreExecute, buildTask =>
            {
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.Preprocessing, SectionIdParts.Process, "WgInclude"), "Sass should run");
                Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity), "Should be running minifycss again for changed sass.");
                var outputscss = GetOutputContent(buildTask, "css", "test1", "generic-generic", "Theme1");
                Assert.IsFalse(outputscss.Contains(".test1includecssfile3"));
                Assert.IsFalse(outputscss.Contains(".test1includecssfile2"));
                Assert.IsFalse(outputscss.Contains(".include1"));
            });
        }

        private static string GetOutputContent(WebGreaseTask postExecuteBuildTask, string type, string name, string locale, string theme = null)
        {
            return File.ReadAllText(GetOutputFile(postExecuteBuildTask, type, name, locale, theme));
        }

        private static string GetOutputFile(WebGreaseTask postExecuteBuildTask, string type, string name, string locale, string theme)
        {
            var file = "/" + locale + "/" + type + "/";
            if (theme != null)
            {
                file += theme + "_";
            }

            file += name + "." + type;

            var hashedFile = GetHashedFile(postExecuteBuildTask, type, file);
            return hashedFile;
        }

        private static string GetHashedFileContent(WebGreaseTask postExecuteBuildTask, string type, string file, string relativePath = null)
        {
            return File.ReadAllText(GetHashedFile(postExecuteBuildTask, type, file, relativePath));
        }

        private static string GetHashedFile(WebGreaseTask postExecuteBuildTask, string type, string file, string relativePath = null)
        {
            var doc = XDocument.Load(Path.Combine(postExecuteBuildTask.RootOutputPath, "statics", type + "_log.xml"));
            var fileElement = doc.Root.Elements("File").FirstOrDefault(e => e.Elements("Input").Any(i => ((string)i).Equals(file, StringComparison.OrdinalIgnoreCase)));

            var normalizeUrl = ((string)fileElement.Element("Output")).Trim('/').NormalizeUrl();

            var hashedFile = Path.Combine(relativePath ?? postExecuteBuildTask.ApplicationRootPath, normalizeUrl);
            return hashedFile;
        }

        private static bool HasExecuted(WebGreaseTask postExecuteBuildTask, params string[] idParts)
        {
            return postExecuteBuildTask.MeasureResults.Any(tm => tm.Name.Equals(string.Join(".", idParts)));
        }

        private static bool HasExecuted(WebGreaseTask postExecuteBuildTask, int exactCount, params string[] idParts)
        {
            return postExecuteBuildTask.MeasureResults.Count(tm => tm.Name.Equals(string.Join(".", idParts))) == exactCount;
        }

        [TestMethod]
        [TestCategory(TestCategories.Caching)]
        [TestCategory(TestCategories.WebGreaseTask)]
        [TestCategory(TestCategories.EverythingActivity)]
        public void CacheCleanCacheAndDestinationTest()
        {
            var testRoot = GetTestRoot(@"WebGrease.Tests\PerformanceTests\TmxSdk");
            Action<WebGreaseTask> preExecute = buildTask =>
                {
                    buildTask.RootOutputPath = Path.Combine(buildTask.ApplicationRootPath, "output.clean.cache");
                    buildTask.ToolsTempPath = Path.Combine(buildTask.ApplicationRootPath, "temp1.clean.cache");
                    buildTask.CacheRootPath = Path.Combine(buildTask.ApplicationRootPath, "cache.clean.cache");
                    buildTask.CacheEnabled = true;
                    buildTask.FileType = FileTypes.JS;
                };

            ExecuteBuildTask("EVERYTHING", testRoot, "Release", preExecute,
                buildTask =>
                    {
                        Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyJsActivity), "Should be minifying js");
                        Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Should have a jsfileset");
                    });

            ExecuteBuildTask("EVERYTHING", testRoot, "Release", preExecute,
                buildTask =>
                    {
                        Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyJsActivity), "Should not be minifying js");
                        Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Should not have a jsfileset");
                    });

            ExecuteBuildTask("CLEANDESTINATION", testRoot, "Release", preExecute, task => { });
            ExecuteBuildTask("CLEANCACHE", testRoot, "Release", preExecute, task => { });

            ExecuteBuildTask("EVERYTHING", testRoot, "Release", preExecute,
                buildTask =>
                    {
                        Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyJsActivity), "Should be minifying js");
                        Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Should have a jsfileset");
                    });
        }

        [TestMethod]
        [TestCategory(TestCategories.Caching)]
        [TestCategory(TestCategories.WebGreaseTask)]
        [TestCategory(TestCategories.EverythingActivity)]
        public void CacheCleanDestinationTest()
        {
            var testRoot = GetTestRoot(@"WebGrease.Tests\PerformanceTests\TmxSdk");
            Action<WebGreaseTask> preExecute = buildTask =>
                {
                    buildTask.RootOutputPath = Path.Combine(buildTask.ApplicationRootPath, "output.clean.destination");
                    buildTask.ToolsTempPath = Path.Combine(buildTask.ApplicationRootPath, "temp1.clean.destination");
                    buildTask.CacheRootPath = Path.Combine(buildTask.ApplicationRootPath, "cache.clean.destination");
                    buildTask.CacheEnabled = true;
                    buildTask.FileType = FileTypes.JS;
                };

            ExecuteBuildTask("EVERYTHING", testRoot, "Release", preExecute,
                buildTask =>
                    {
                        Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyJsActivity), "Should be minifying js");
                        Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Should have a jsfileset");
                    });

            ExecuteBuildTask("EVERYTHING", testRoot, "Release", preExecute,
                buildTask =>
                    {
                        Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyJsActivity), "Should not be minifying js");
                        Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Should not have a jsfileset");
                    });

            ExecuteBuildTask("CLEANDESTINATION", testRoot, "Release", preExecute, task => { });
            ExecuteBuildTask("EVERYTHING", testRoot, "Release", preExecute,
                buildTask =>
                    {
                        Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyJsActivity), "Should be minifying js");
                        Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Should have a jsfileset");
                    });
        }

        [TestMethod]
        [TestCategory(TestCategories.Caching)]
        [TestCategory(TestCategories.WebGreaseTask)]
        [TestCategory(TestCategories.EverythingActivity)]
        public void CachePerformanceTest()
        {
            var testRoot = GetTestRoot(@"WebGrease.Tests\PerformanceTests\TmxSdk");
            var perfRoot = GetTestRoot(@"..\..\Performance");
            if (!Directory.Exists(perfRoot))
            {
                Directory.CreateDirectory(perfRoot);
            }

            double measure1 = 0;
            double measure2 = int.MaxValue / 2;
            double measure3 = int.MaxValue;

            var commonPreBuildTask = new Action<WebGreaseTask>((buildTask) =>
                {
                    buildTask.OverrideLocales = "";
                    buildTask.OverrideThemes = "";
                });

            Action<WebGreaseTask> preExecute1 = buildTask =>
                {
                    commonPreBuildTask(buildTask);
                    buildTask.RootOutputPath = Path.Combine(buildTask.ApplicationRootPath, "output1");
                    buildTask.ToolsTempPath = Path.Combine(buildTask.ApplicationRootPath, "temp1");
                    buildTask.CacheRootPath = Path.Combine(buildTask.ApplicationRootPath, "cache");
                    buildTask.CacheEnabled = true;
                };

            Execute(testRoot, perfRoot, "precache", "Release",
                preExecute1,
                buildTask =>
                {
                    Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting), "Pre-cache run should be spriting");
                    Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Optimize), "Pre-cache run should be optimizing");
                    Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.CssFileSet), "Pre-cache  run should have any css filesets");
                    Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Pre-cache  run should have any js filesets");
                    measure1 = buildTask.MeasureResults.Sum(m => m.Duration);
                });

            Execute(testRoot, perfRoot, "incremental", "Release",
                preExecute1,
                buildTask =>
                {
                    Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting), "Incremental.1 run should not be spriting");
                    Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Optimize), "Incremental.1 run should not be optimizing");
                    Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.CssFileSet), "Incremental.1 run should not have any css filesets");
                    Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Incremental.1 run should not have any js filesets");
                });

            Execute(testRoot, perfRoot, null, "Release", preExecute1, null, "CLEANDESTINATION");
            Execute(testRoot, perfRoot, "postcache", "Release",
                preExecute1,
                buildTask =>
                {
                    Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting), "Post-cache run should not be spriting");
                    Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Optimize), "Post-cache run should not be optimizing");
                    Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.CssFileSet), "Post-cache  run should have any css filesets");
                    Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Post-cache  run should have any js filesets");
                    measure2 = buildTask.MeasureResults.Sum(m => m.Duration);
                });

            Action<WebGreaseTask> preExecute2 = buildTask =>
                {
                    commonPreBuildTask(buildTask);
                    buildTask.RootOutputPath = Path.Combine(buildTask.ApplicationRootPath, "output2");
                    buildTask.ToolsTempPath = Path.Combine(buildTask.ApplicationRootPath, "temp2");
                    buildTask.CacheRootPath = Path.Combine(buildTask.ApplicationRootPath, "cache");
                    buildTask.CacheEnabled = true;
                };
            Execute(testRoot, perfRoot, "postcache2", "Release",
                preExecute2,
                buildTask =>
                {
                    Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting), "Post-cache run should not be spriting");
                    Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Optimize), "Post-cache run should not be optimizing");
                    Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.CssFileSet), "Post-cache  run should have any css filesets");
                    Assert.IsTrue(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Post-cache  run should have any js filesets");
                    measure2 = buildTask.MeasureResults.Sum(m => m.Duration);
                });

            Execute(testRoot, perfRoot, "incremental2", "Release",
                preExecute2,
                buildTask =>
                {
                    Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Spriting), "Incremental.2 run should not be spriting");
                    Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.MinifyCssActivity, SectionIdParts.Optimize), "Incremental.2 run should not be optimizing");
                    Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.CssFileSet), "Incremental.2 run should not have any css filesets");
                    Assert.IsFalse(HasExecuted(buildTask, SectionIdParts.JsFileSet), "Incremental.2 run should not have any js filesets");
                    measure3 = buildTask.MeasureResults.Sum(m => m.Duration);
                });

            Func<string, string> logFileChange = logFileContent => logFileContent.Replace("/output1/", "/output2/");
            DirectoryMatch(Path.Combine(testRoot, "output2"), Path.Combine(testRoot, "output1"), logFileChange);
            DirectoryMatch(Path.Combine(testRoot, "output1"), Path.Combine(testRoot, "output2"), logFileChange);

            // TODO: Assert folder output1 == output2
        }

        private static void DirectoryMatch(string path1, string path2, Func<string, string> logFileChange)
        {
            foreach (var file1 in Directory.GetFiles(path1))
            {
                // Ignore debug/report files
                if (file1.EndsWith(".measure.txt") || file1.EndsWith(".measure.csv") || file1.EndsWith(".dgml") || file1.EndsWith(".scan.xml"))
                {
                    continue;
                }

                var relativeFile = file1.MakeRelativeTo(path1.EnsureEndSeparator());
                var file2 = Path.Combine(path2, relativeFile);

                Assert.IsTrue(File.Exists(file2), "File does not exist: {0}".InvariantFormat(file2));

                // For log files we do somethinf special to check if they match.
                if (file1.EndsWith("_log.xml"))
                {
                    var logFile1 = logFileChange(File.ReadAllText(file1));
                    var logFile2 = logFileChange(File.ReadAllText(file2));
                    Assert.AreEqual(
                        WebGreaseContext.ComputeContentHash(logFile1), 
                        WebGreaseContext.ComputeContentHash(logFile2),
                        "Log files do not match: {0} and {1}".InvariantFormat(file1, file2));
                }
                else
                {
                    Assert.AreEqual(
                        WebGreaseContext.ComputeFileHash(file1),
                        WebGreaseContext.ComputeFileHash(file2),
                        "Files do not match: {0} and {1}".InvariantFormat(file1, file2));
                }
            }

            foreach (var directory1 in Directory.GetDirectories(path1))
            {
                var relative = directory1.MakeRelativeTo(path1.EnsureEndSeparator());
                DirectoryMatch(directory1, Path.Combine(path2, relative), logFileChange);
            }
        }

        private static void Execute(string testRoot, string perfRoot, string measureName, string configType, Action<WebGreaseTask> preExecute, Action<WebGreaseTask> postExecute, string action = "EVERYTHING")
        {
            ExecuteBuildTask(action, testRoot, configType, preExecute, pb =>
            {
                if (measureName != null)
                {
                    var time = DateTime.Now.ToString("yyMMdd_HHmmss");

                    File.Copy(Path.Combine(pb.MeasureReportPath, "TmxSdk.Everything.measure.txt"), Path.Combine(perfRoot, "TmxSdk.Everything.measure." + time + "." + measureName + ".txt"));
                    if (postExecute != null)
                    {
                        postExecute(pb);
                    }
                }
            });
        }

        private static string GetTestRoot(string path)
        {
            return Path.Combine(TestDeploymentPaths.TestDirectory, path);
        }

        private static void ExecuteBuildTask(string activity, string rootFolderForTest, string configType, Action<WebGreaseTask> preExecute, Action<WebGreaseTask> postExecute)
        {
            var buildEngineMock = new Mock<IBuildEngine>();
            buildEngineMock
                .Setup(bem => bem.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback((BuildErrorEventArgs e) =>
                    {
                        throw new Exception("!!! [Error]:" + e.Message + " at line [" + e.LineNumber + "] of file [" + e.File + "]");
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
            var rootPath = Path.Combine(TestDeploymentPaths.TestDirectory, rootFolderForTest);

            buildTask.ConfigurationPath = rootPath;
            if (configType != null)
            {
                buildTask.ConfigType = configType;
            }

            buildTask.ApplicationRootPath = rootPath;
            buildTask.RootInputPath = Path.Combine(rootPath, "input");
            buildTask.CacheRootPath = Path.Combine(rootPath, "cache");
            buildTask.Measure = true;

            if (preExecute != null)
            {
                preExecute(buildTask);
            }

            buildTask.LogFolderPath = Path.Combine(buildTask.RootOutputPath, "statics");
            buildTask.MeasureReportPath = Path.Combine(buildTask.RootOutputPath, "reports");

            var result = buildTask.Execute();

            if (postExecute != null)
            {
                postExecute(buildTask);
            }

            if (!result)
            {
                Assert.Fail("No result.");
            }
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
            Console.WriteLine("!!! [Error]:" + e.Message + " at line [" + e.LineNumber + "] of file [" + e.File + "]");
        }
    }
}