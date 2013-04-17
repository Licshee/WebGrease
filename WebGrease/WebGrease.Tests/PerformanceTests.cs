using System;

namespace Microsoft.WebGrease.Tests
{
    using System.IO;

    using Microsoft.Build.Framework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using global::WebGrease.Build;
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

            var outputPath = ExecuteBuildTask(
                "EVERYTHING",
                testRoot,
                buildTask => { });

            var time = DateTime.Now.ToString("yyMMdd_HHmmss");

            File.Copy(Path.Combine(outputPath, "TmxSdk.measure.txt"), Path.Combine(perfRoot, "TmxSdk.measure." + time + ".txt"));
            File.Copy(Path.Combine(outputPath, "TmxSdk.measure.csv"), Path.Combine(perfRoot, "TmxSdk.measure." + time + ".csv"));
        }

        private static string GetTestRoot(string path)
        {
            return Path.Combine(TestDeploymentPaths.TestDirectory, path);
        }

        private static string ExecuteBuildTask(string activity, string rootFolderForTest, Action<WebGreaseTask> extraSettings, string configType = null)
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
            buildTask.ToolsTempPath = Path.GetTempFileName();

            File.Delete(buildTask.ToolsTempPath);

            if (!Directory.Exists(buildTask.LogFolderPath))
            {
                Directory.CreateDirectory(buildTask.LogFolderPath);
            }

            if (!Directory.Exists(buildTask.RootOutputPath))
            {
                Directory.CreateDirectory(buildTask.RootOutputPath);
            }

            buildTask.Measure = true;

            extraSettings(buildTask);
            var result = buildTask.Execute();

            Assert.IsFalse(hasErrors, "Static file errors occurred while running performance run. None should happen.");
            if (!result)
            {
                return null;
            }

            Directory.Delete(buildTask.ToolsTempPath, true);

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