using System;

namespace Microsoft.WebGrease.Tests
{
    using System.IO;
    using System.Linq;

    using Microsoft.Build.Framework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using global::WebGrease;
    using global::WebGrease.Build;
    using global::WebGrease.Extensions;
    using global::WebGrease.Tests;

    [TestClass]
    public class PerformanceTests
    {
        [TestMethod]
        public void TmxSdkPerformanceTest()
        {
            var testRoot = GetTestRoot(@"WebGrease.Tests\PerformanceTests\TmxSdk");
            var perfRoot = GetTestRoot(@"..\..\Performance");
            if (!Directory.Exists(perfRoot))
            {
                Directory.CreateDirectory(perfRoot);
            }

            double measure1 = 0;
            double measure2 = 0;

            Execute(testRoot, perfRoot, "precache", "Release",
                buildTask =>
                {
                    buildTask.RootOutputPath = Path.Combine(buildTask.ApplicationRootPath, "output1");
                    buildTask.ToolsTempPath = Path.Combine(buildTask.ApplicationRootPath, "temp1");
                    buildTask.CacheRootPath = Path.Combine(buildTask.ApplicationRootPath, "cache");
                    buildTask.CacheEnabled = true;
                },
                buildTask =>
                {
                    Assert.IsTrue(buildTask.totalMeasure.Any(tm => tm.IdParts.SequenceEqual(new[] { TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Sprite })), "Pre-cache run should be spriting");
                    Assert.IsTrue(buildTask.totalMeasure.Any(tm => tm.IdParts.SequenceEqual(new[] { TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Optimize })), "Pre-cache run should be optimizing");
                    Assert.IsTrue(buildTask.totalMeasure.Any(tm => tm.IdParts.SequenceEqual(new[] { TimeMeasureNames.CssFileSet })), "Pre-cache  run should have any css filesets");
                    Assert.IsTrue(buildTask.totalMeasure.Any(tm => tm.IdParts.SequenceEqual(new[] { TimeMeasureNames.JsFileSet })), "Pre-cache  run should have any css filesets");
                    measure1 = buildTask.totalMeasure.Sum(m => m.Duration);
                });

            Execute(testRoot, perfRoot, "postcache", "Release",
                buildTask =>
                {
                    buildTask.RootOutputPath = Path.Combine(buildTask.ApplicationRootPath, "output2");
                    buildTask.ToolsTempPath = Path.Combine(buildTask.ApplicationRootPath, "temp2");
                    buildTask.CacheRootPath = Path.Combine(buildTask.ApplicationRootPath, "cache");
                    buildTask.CacheEnabled = true;
                },
                buildTask =>
                {
                    Assert.IsFalse(buildTask.totalMeasure.Any(tm => tm.IdParts.SequenceEqual(new[] { TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Sprite })), "Post-cache run should not be spriting");
                    Assert.IsFalse(buildTask.totalMeasure.Any(tm => tm.IdParts.SequenceEqual(new[] { TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Optimize })), "Post-cache run should not be optimizing");
                    Assert.IsTrue(buildTask.totalMeasure.Any(tm => tm.IdParts.SequenceEqual(new[] { TimeMeasureNames.CssFileSet })), "Pre-cache  run should have any css filesets");
                    Assert.IsTrue(buildTask.totalMeasure.Any(tm => tm.IdParts.SequenceEqual(new[] { TimeMeasureNames.JsFileSet })), "Pre-cache  run should have any css filesets");
                    measure2 = buildTask.totalMeasure.Sum(m => m.Duration);
                });

            Execute(testRoot, perfRoot, "incremental", "Release",
                buildTask =>
                {
                    buildTask.RootOutputPath = Path.Combine(buildTask.ApplicationRootPath, "output2");
                    buildTask.ToolsTempPath = Path.Combine(buildTask.ApplicationRootPath, "temp2");
                    buildTask.CacheRootPath = Path.Combine(buildTask.ApplicationRootPath, "cache");
                    buildTask.Incremental = true;
                    buildTask.CacheEnabled = true;
                },
                buildTask =>
                {
                    Assert.IsFalse(buildTask.totalMeasure.Any(tm => tm.IdParts.SequenceEqual(new[] { TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Sprite })), "Post-cache run should not be spriting");
                    Assert.IsFalse(buildTask.totalMeasure.Any(tm => tm.IdParts.SequenceEqual(new[] { TimeMeasureNames.MinifyCssActivity, TimeMeasureNames.Optimize })), "Post-cache run should not be optimizing");
                    Assert.IsFalse(buildTask.totalMeasure.Any(tm => tm.IdParts.SequenceEqual(new[] { TimeMeasureNames.CssFileSet })), "Incremental run should not have any css filesets");
                    Assert.IsFalse(buildTask.totalMeasure.Any(tm => tm.IdParts.SequenceEqual(new[] { TimeMeasureNames.JsFileSet })), "Incremental run should not have any css filesets");
                    measure2 = buildTask.totalMeasure.Sum(m => m.Duration);
                });

            Assert.IsTrue(measure2 < measure1/2);

            DirectoryMatch(Path.Combine(testRoot, "output2"), Path.Combine(testRoot, "output1"));
            DirectoryMatch(Path.Combine(testRoot, "output1"), Path.Combine(testRoot, "output2"));

            // TODO: Assert folder output1 == output2
        }

        private void DirectoryMatch(string path1, string path2)
        {
            foreach (var file1 in Directory.GetFiles(path1))
            {
                var relativeFile = file1.MakeRelativeTo(path1.EnsureEndSeperatorChar());
                var file2 = Path.Combine(path2, relativeFile);
                Assert.IsTrue(File.Exists(file2), "File does not exist: {0}".InvariantFormat(file2));

                // Check if content is similar, but not fure measure files.
                if (!file1.EndsWith(".measure.txt") && !file1.EndsWith(".measure.csv"))
                {
                    Assert.AreEqual(
                        WebGreaseContext.ComputeFileHash(file1),
                        WebGreaseContext.ComputeFileHash(file2),
                        "Files do not match: {0} and {1}".InvariantFormat(file1, file2));
                }
            }

            foreach (var directory1 in Directory.GetDirectories(path1))
            {
                var relative = directory1.MakeRelativeTo(path1.EnsureEndSeperatorChar());
                this.DirectoryMatch(directory1, Path.Combine(path2, relative));
            }
        }

        private static void Execute(string testRoot, string perfRoot, string measureName, string configType, Action<WebGreaseTask> preExecute, Action<WebGreaseTask> postExecute)
        {
            var outputPath = ExecuteBuildTask("EVERYTHING", testRoot, preExecute, postExecute, configType);

            var time = DateTime.Now.ToString("yyMMdd_HHmmss");

            File.Copy(Path.Combine(outputPath, "TmxSdk.measure.txt"), Path.Combine(perfRoot, "TmxSdk.measure." + time + "." + measureName + ".txt"));
            File.Copy(Path.Combine(outputPath, "TmxSdk.measure.csv"), Path.Combine(perfRoot, "TmxSdk.measure." + time + "." + measureName + ".csv"));
        }

        private static string GetTestRoot(string path)
        {
            return Path.Combine(TestDeploymentPaths.TestDirectory, path);
        }

        private static string ExecuteBuildTask(string activity, string rootFolderForTest, Action<WebGreaseTask> preExecute, Action<WebGreaseTask> postExecute, string configType = null)
        {
            var buildEngineMock = new Mock<IBuildEngine>();
            var hasErrors = false;
            buildEngineMock
                .Setup(bem => bem.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback((BuildErrorEventArgs e) =>
                    {
                        hasErrors = true;
                        LogErrorEvent(e);
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
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, rootFolderForTest);

            buildTask.ConfigurationPath = sourceDirectory;
            if (configType != null)
            {
                buildTask.ConfigType = configType;
            }

            buildTask.ApplicationRootPath = sourceDirectory;
            buildTask.RootInputPath = Path.Combine(sourceDirectory, "input");
            buildTask.RootOutputPath = Path.Combine(sourceDirectory, "output\\sc");
            buildTask.LogFolderPath = Path.Combine(sourceDirectory, "output\\statics");

            preExecute(buildTask);

            if (!Directory.Exists(buildTask.LogFolderPath))
            {
                Directory.CreateDirectory(buildTask.LogFolderPath);
            }

            if (!Directory.Exists(buildTask.RootOutputPath))
            {
                Directory.CreateDirectory(buildTask.RootOutputPath);
            }

            if (!Directory.Exists(buildTask.CacheRootPath))
            {
                Directory.CreateDirectory(buildTask.CacheRootPath);
            }

            if (!Directory.Exists(buildTask.ToolsTempPath))
            {
                Directory.CreateDirectory(buildTask.ToolsTempPath);
            }

            buildTask.Measure = true;
            var result = buildTask.Execute();

            postExecute(buildTask);

            Assert.IsFalse(hasErrors, "Static file errors occurred while running performance run. None should happen.");
            if (!result)
            {
                Assert.Fail("No result.");
            }

            return buildTask.RootOutputPath;
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