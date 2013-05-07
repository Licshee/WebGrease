using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebGrease.Tests
{
    using System.IO;
    using System.Xml.Linq;
    using Css;

    using WebGrease.Extensions;

    [TestClass]
    public class CLITests
    {
        [TestMethod]
        public void CLIUsageDisplayTest()
        {
            // unknown argument test
            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                int result = Program.Main(new[] { "-?" });

                Assert.IsTrue(result == 1);
                Assert.IsTrue(sw.ToString().Contains("Usage"));
            }

            // no arguments
            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                int result = Program.Main(null);

                Assert.IsTrue(result == 1);
                Assert.IsTrue(sw.ToString().Contains("Usage"));
            }
        }

        [TestMethod]
        public void CLIMinificationTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\CLITests");
            var destDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\CLITests");
            var inputPath = Path.Combine(sourceDirectory, @"Input\Case1\");
            var outputPath = Path.Combine(destDirectory, @"Output\Case1\");

            var args = new[] { "-m", "-in:" + inputPath, "-out:" + outputPath };

            int result = Program.Main(args);

            Assert.IsTrue(result == 0);

            var outputJs = Path.Combine(outputPath, "test1.min.js");
            var outputCss = Path.Combine(outputPath, "test1.min.css");

            Assert.IsTrue(File.Exists(outputJs));
            var jsText = File.ReadAllText(outputJs);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(jsText));
            Assert.IsTrue(jsText == "(function(n){document.write(n)})(jQuery);");

            Assert.IsTrue(File.Exists(outputCss));
            var cssText = File.ReadAllText(outputCss);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(cssText));
            Assert.IsTrue(!cssText.Contains("Exclude"));

        }


        [TestMethod]
        public void CLIRelativePathMinificationTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\CLITests");
            var destDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\CLITests");
            var inputPath = Path.Combine(sourceDirectory, @"Input\Case1\");
            var outputPath = Path.Combine(destDirectory, @"Output\Case1\");

            // make paths relative
            inputPath = inputPath.MakeRelativeTo(Environment.CurrentDirectory);
            outputPath = outputPath.MakeRelativeTo(Environment.CurrentDirectory);


            var args = new[] { "-m", "-in:" + inputPath, "-out:" + outputPath };

            int result = Program.Main(args);

            Assert.IsTrue(result == 0);

            var outputJs = Path.GetFullPath(Path.Combine(outputPath, "test1.min.js"));
            var outputCss = Path.GetFullPath(Path.Combine(outputPath, "test1.min.css"));

            Assert.IsTrue(File.Exists(outputJs));
            var jsText = File.ReadAllText(outputJs);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(jsText));
            Assert.IsTrue(jsText == "(function(n){document.write(n)})(jQuery);");

            Assert.IsTrue(File.Exists(outputCss));
            var cssText = File.ReadAllText(outputCss);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(cssText));
            Assert.IsTrue(!cssText.Contains("Exclude"));

        }

        [TestMethod]
        public void CLIValidationTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\MinifyJSActivityTest");
            var destDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\CLITests");
            var inputFile = Path.Combine(sourceDirectory, @"Input\Case2\test1.js");
            var outputFile = Path.Combine(destDirectory, @"Output\Case2\test1.js");

            var args = new[] { "-v", "-in:" + inputFile, "-out:" + outputFile };

            int statusCode = Program.Main(args);


            // There should be no unhandled exceptions (even though the js is flawed).
            Assert.AreEqual(0, statusCode);
        }

        [TestMethod]
        public void CLIBundlingTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\AssemblerActivityTest");
            var destDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\CLITests");
            var inputFile = Path.Combine(sourceDirectory, @"Input\Case1\*.js");
            var outputFile = Path.Combine(destDirectory, @"Output\Case2\test1.js");

            var args = new[] { "-b", "-in:" + inputFile, "-out:" + outputFile };

            int result = Program.Main(args);

            Assert.IsTrue(result == 0);
            Assert.IsTrue(File.Exists(outputFile));
            var jsText = File.ReadAllText(outputFile);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(jsText));

            Assert.IsTrue(jsText.Contains("var name = \"script1.js\""));
            Assert.IsTrue(jsText.Contains("var name = \"script2.js\""));
        }

        [TestMethod]
        public void CLIHashingTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\CLITests");
            var destDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\CLITests");
            var inputPath = Path.Combine(sourceDirectory, @"Input\Case1\");
            var outputPath = Path.Combine(destDirectory, @"Output\Case1\");

            var args = new[] { "-a", "-in:" + inputPath, "-out:" + outputPath };

            int result = Program.Main(args);

            Assert.IsTrue(result == 0);
            Assert.IsTrue(Directory.Exists(outputPath + "02"));
            Assert.IsTrue(File.Exists(Path.Combine(outputPath, "02", "953f56f041bd5d7e7456a0eec1c112.js")));
            Assert.IsTrue(Directory.Exists(outputPath + "aa"));
            Assert.IsTrue(File.Exists(Path.Combine(outputPath, "aa", "ee066eb71c584d9ad387a6c77c2c05.css")));
        }

        [TestMethod]
        public void CLIImageSpriteTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\CLITests");
            var destDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\CLITests");
            var inputPath = Path.Combine(sourceDirectory, @"Input\Case2\");
            var outputPath = Path.Combine(destDirectory, @"Output\Case2\");

            var args = new[] { "-s", "-in:" + inputPath, "-out:" + outputPath };

            int result = Program.Main(args);

            Assert.IsTrue(result == 0);
            Assert.IsTrue(Directory.Exists(Path.Combine(outputPath, "images")));

            // mapping file (so we can look up the target name of the assembled image, as the generated image can be different based on gdi dll versions)
            var mapFilePath = Path.Combine(outputPath, "SpriteTest.scan.css.scan.xml");
            var testImage = "media.gif";

            Assert.IsTrue(File.Exists(mapFilePath));
            // verify our test file is in the xml file and get the source folder and assembled file name.
            string relativePath;
            using (var fs = File.OpenRead(mapFilePath))
            {
                var mapFile = XDocument.Load(fs);
                var inputElement = mapFile.Root.Descendants()
                    // get at the input elements
                    .Descendants().Where(e => e.Name == "input")
                    // now at the source file name
                    .Descendants().FirstOrDefault(i => i.Name == "originalfile" && i.Value.Contains(testImage));

                // get the output 
                var outputElement = inputElement.Parent.Parent;

                // get the input path from the location of the css file and the output path where the destination file is.
                var imageInputPath = Path.GetDirectoryName(inputElement.Value).ToLowerInvariant();
                var imageOutputPath = outputElement.Attribute("file").Value.ToLowerInvariant();

                // diff the paths to get the relative path (as found in the final file)
                relativePath = imageOutputPath.MakeRelativeTo(imageInputPath);
            }
            var spritedCssFile = Path.Combine(outputPath, "spritetest.css");
            Assert.IsTrue(File.Exists(spritedCssFile));
            var text = File.ReadAllText(spritedCssFile);
            Assert.IsTrue(text.Contains("background:0 0 url(" + relativePath + ") no-repeat;"));
        }
    }
}
