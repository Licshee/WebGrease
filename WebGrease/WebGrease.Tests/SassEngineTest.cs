namespace Microsoft.WebGrease.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using global::WebGrease.Activities;
    using global::WebGrease.Preprocessing.Sass;
    using global::WebGrease.Tests;

    /// <summary>
    /// This is a test class for using MinifyCss and is intended to contain all MinifyCss Unit Tests
    /// </summary>
    [TestClass]
    public class SassEngineTest
    {
        [TestMethod]
        public void TestScssImports()
        {
            var sassFile = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\SassTest\Test1\Input\stylesheet1.scss");
            var result = ProcessSass(File.ReadAllText(sassFile), sassFile);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(result));
        }

        [TestMethod]
        public void TestTokens()
        {
            var tests = new Dictionary<string, string>
                {
                    {
                        ".someClass1 { #{token('%SOMETOKENNAME%')}:token('%SOMETOKENVALUE%') }",
                        ".someClass1 {\r\n  %SOMETOKENNAME%: %SOMETOKENVALUE%; }\r\n"
                    },
                    {
                        ".someClass2 { #{token(\"%SOMETOKENNAME%\")}:token(\"%SOMETOKENVALUE%\") }",
                        ".someClass2 {\r\n  %SOMETOKENNAME%: %SOMETOKENVALUE%; }\r\n"
                    },
                };

            foreach (var test in tests)
            {
                Assert.AreEqual(test.Value, ProcessSass(test.Key, "test.scss"));
            }
        }

        [TestMethod]
        public void TestScssVariables()
        {
            const string Input = @"$blue: #3bbfce;
$margin: 16px;
.content-navigation {
  border-color: $blue;
  color:
	darken($blue, 9%);
}

.border {
  padding: $margin / 2;
  margin: $margin / 2;
  border-color: $blue;
}";

            const string Result = ".content-navigation {\r\n  border-color: #3bbfce;\r\n  color: #2ca2af; }\r\n\r\n.border {\r\n  padding: 8px;\r\n  margin: 8px;\r\n  border-color: #3bbfce; }\r\n";

            Assert.AreEqual(Result, ProcessSass(Input, "test.scss"));
        }


        [TestMethod]
        public void TestScssImport()
        {
            const string Include = @"$color: blue;";
            const string Result = ".body {\r\n  color: blue; }\r\n";

            using (new TempFile(Include, "toinclude.scss"))
            {
                var input = "@import 'toinclude.scss'; .body{color:$color}";
                Assert.AreEqual(Result, ProcessSass(input, "test.scss"));
            }
        }

        private static string ProcessSass(string content, string filename, LogExtendedError logExtendedError = null)
        {
            return new SassPreprocessingEngine()
                .Process(
                    content, 
                    filename, 
                    null, 
                    (s1) => { }, 
                    (s1, s2, s3) => { }, 
                    logExtendedError ?? ((s1, s2, s3, s4, s5, s6, s7, s8, errorMessage) => { }));
        }

        [TestMethod]
        public void TestScssSmoke()
        {
            const string Input = @".error {
    border: 1px #f00 solid;
    background: #fdd;
}
.error.intrusion {
    font-size: 1.3em;
    font-weight: bold;
}

.badError {
    @extend .error;
    border-width: 3px;
}";

            Assert.IsFalse(string.IsNullOrWhiteSpace(ProcessSass(Input, "test.scss")));
        }

        [TestMethod]
        public void TestSassNegativeSmoke()
        {
            const string Input = ".foo bar[val=\"//\"]\n { %%baz: bang; }";
            string errorResult = null;
            string errorFile = null;
            int errorLine = 0;
            ProcessSass(Input, "test.scss", (s1, s2, s3, file, line, s6, s7, s8, errorMessage) => { errorResult = errorMessage; errorFile = file; errorLine = line ?? 0; });
            Assert.IsTrue(errorResult.Contains("Syntax"));
            Assert.AreEqual("test.scss", errorFile);
            Assert.AreEqual(errorLine, 2);
        }

        [TestMethod]
        public void TestSassNegativeIncludeSmoke()
        {
            const string Include = "\r\n\r\n\r\naaaaa$color: blue;";
            using (var includeFile = new TempFile(Include, "toinclude.scss"))
            {
                var input = "@import 'toinclude.scss'; .body{color:$color}";
                string errorResult = null;
                string errorFile = null;
                int errorLine = 0;
                ProcessSass(input, "test.scss", (s1, s2, s3, file, line, s6, s7, s8, errorMessage) => { errorResult = errorMessage; errorFile = file; errorLine = line ?? 0; });
                Assert.IsTrue(errorResult.Contains("Syntax"));
                Assert.AreEqual(new FileInfo(includeFile.Filename).FullName, errorFile);
                Assert.AreEqual(4, errorLine);
            }
        }
    }
}