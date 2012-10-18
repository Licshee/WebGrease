namespace ImageAssemble.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WebGrease.ImageAssemble;

    /// <summary>
    /// This is a test class for ImageMapTest and is intended
    /// to contain all ImageMapTest Unit Tests
    /// </summary>
    [TestClass]
    public class ImageMapTest
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// A test for ImageMap Constructor
        /// </summary>
        [TestMethod]
        public void ImageMapConstructorTest()
        {
            var target = new ImageMap_Accessor();
            Assert.IsNull(target.xdoc);
            Assert.IsNull(target.root);
            Assert.IsTrue(string.IsNullOrEmpty(target.mapFileName));
        }
    }
}
