// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileHasherActivityTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for FileHasherActivityTest and is intended
//   to contain all FileHasherActivityTest Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Tests
{
    using System.IO;
    using Activities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WebGrease.Configuration;

    /// <summary>This is a test class for FileHasherActivityTest and is intended
    /// to contain all FileHasherActivityTest Unit Tests</summary>
    [TestClass]
    public class FileHasherActivityTest
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for file hasher keeping source directories structure in tact.</summary>
        [TestMethod]
        public void FileHasherActivityPreserveSourceDirectoryTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\FileHasherActivityTest\Input");
            var fileHasherActivity = new FileHasherActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            fileHasherActivity.SourceDirectories.Add(sourceDirectory);
            var destinationDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\FileHasherActivityTest\Output\FileHasherActivityPreserveSourceDirectory");
            fileHasherActivity.DestinationDirectory = destinationDirectory;
            fileHasherActivity.CreateExtraDirectoryLevelFromHashes = false;
            fileHasherActivity.ShouldPreserveSourceDirectoryStructure = true;
            fileHasherActivity.BasePrefixToRemoveFromOutputPathInLog = destinationDirectory;
            fileHasherActivity.LogFileName = Path.Combine(destinationDirectory, "FileHasherActivityPreserveSourceDirectory.log.xml");
            fileHasherActivity.Execute();

            Assert.IsTrue(Directory.Exists(destinationDirectory));
            Assert.IsTrue(Directory.Exists(Path.Combine(destinationDirectory, "C1")));
            Assert.IsTrue(File.Exists(Path.Combine(destinationDirectory, "C1", "ba4027675b202b7bf6f15085cb3344e3.gif")));
            Assert.IsTrue(Directory.Exists(Path.Combine(destinationDirectory, "C1", "C3")));
            Assert.IsTrue(File.Exists(Path.Combine(destinationDirectory, "C1", "C3", "dbd30b957cfadf9e684dc8ef0ce3f2a8.gif")));
            Assert.IsTrue(Directory.Exists(Path.Combine(destinationDirectory, "C2")));
            Assert.IsTrue(File.Exists(Path.Combine(destinationDirectory, "C2", "083b261ab91fa0d8c12e22d898238840.gif")));
            Assert.IsTrue(File.Exists(fileHasherActivity.LogFileName));
        }

        /// <summary>A test for file hasher keeping source directories structure not in tact.</summary>
        [TestMethod]
        public void FileHasherActivityNoPreserveSourceDirectoryTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\FileHasherActivityTest\Input");
            var fileHasherActivity = new FileHasherActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            fileHasherActivity.SourceDirectories.Add(sourceDirectory);
            var destinationDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\FileHasherActivityTest\Output\FileHasherActivityNoPreserveSourceDirectory");
            fileHasherActivity.DestinationDirectory = destinationDirectory;
            fileHasherActivity.CreateExtraDirectoryLevelFromHashes = true;
            fileHasherActivity.ShouldPreserveSourceDirectoryStructure = false;
            fileHasherActivity.BasePrefixToRemoveFromOutputPathInLog = destinationDirectory;
            fileHasherActivity.LogFileName = Path.Combine(destinationDirectory, "FileHasherActivityPreserveSourceDirectory.log.xml");
            fileHasherActivity.Execute();

            Assert.IsTrue(Directory.Exists(destinationDirectory));
            Assert.IsTrue(Directory.Exists(Path.Combine(destinationDirectory, "08")));
            Assert.IsTrue(Directory.Exists(Path.Combine(destinationDirectory, "ba")));
            Assert.IsTrue(Directory.Exists(Path.Combine(destinationDirectory, "db")));
            Assert.IsTrue(File.Exists(fileHasherActivity.LogFileName));
        }

        /// <summary>A test for file hasher respecting file filters.</summary>
        [TestMethod]
        public void FileHasherActivityFilterTest()
        {
            var sourceDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\FileHasherActivityTest\Input");
            var fileHasherActivity = new FileHasherActivity(new WebGreaseContext(new WebGreaseConfiguration()));
            fileHasherActivity.SourceDirectories.Add(sourceDirectory);
            var destinationDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"WebGrease.Tests\FileHasherActivityTest\Output\FileHasherActivityFilterTest");
            fileHasherActivity.DestinationDirectory = destinationDirectory;
            fileHasherActivity.CreateExtraDirectoryLevelFromHashes = false;
            fileHasherActivity.ShouldPreserveSourceDirectoryStructure = true;
            fileHasherActivity.BasePrefixToRemoveFromOutputPathInLog = destinationDirectory;
            fileHasherActivity.LogFileName = Path.Combine(destinationDirectory, "FileHasherActivityFilterTest.log.xml");
            fileHasherActivity.FileTypeFilter = "*.png";
            fileHasherActivity.Execute();

            Assert.IsTrue(Directory.Exists(destinationDirectory));
            Assert.IsTrue(Directory.Exists(Path.Combine(destinationDirectory, "C1")));
            Assert.IsFalse(File.Exists(Path.Combine(destinationDirectory, "C1", "ba4027675b202b7bf6f15085cb3344e3.gif")));
            Assert.IsTrue(File.Exists(Path.Combine(destinationDirectory, "C1", "163af4596ea7d10cadda5233fe6f1282.png")));
            Assert.IsTrue(File.Exists(fileHasherActivity.LogFileName));
        }
    }
}
