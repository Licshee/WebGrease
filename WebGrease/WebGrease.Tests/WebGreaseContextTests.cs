// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebGreaseContextTests.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>

namespace Microsoft.WebGrease.Tests
{
    using System.IO;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using global::WebGrease;

    /// <summary>The web grease context tests.</summary>
    [TestClass]
    public class WebGreaseContextTests
    {
        #region Public Methods and Operators

        /// <summary>The test hashing.</summary>
        [TestMethod]
        [TestCategory(TestCategories.Hashing)]
        public void HashingAlgorithmTest()
        {
            const string Value = "RandomValue1";
            var valueFileName = Path.GetTempFileName();

            File.WriteAllText(valueFileName, Value);
            Assert.AreEqual(WebGreaseContext.ComputeFileHash(valueFileName), WebGreaseContext.ComputeContentHash(Value));

            File.WriteAllText(valueFileName, Value, Encoding.Default);
            Assert.AreEqual(WebGreaseContext.ComputeFileHash(valueFileName), WebGreaseContext.ComputeContentHash(Value, Encoding.Default));

            File.WriteAllText(valueFileName, Value, Encoding.UTF8);
            Assert.AreEqual(WebGreaseContext.ComputeFileHash(valueFileName), WebGreaseContext.ComputeContentHash(Value, Encoding.UTF8));
            Assert.AreNotEqual(WebGreaseContext.ComputeFileHash(valueFileName), WebGreaseContext.ComputeContentHash(Value));

            File.WriteAllText(valueFileName, Value, Encoding.UTF32);
            Assert.AreEqual(WebGreaseContext.ComputeFileHash(valueFileName), WebGreaseContext.ComputeContentHash(Value, Encoding.UTF32));
            Assert.AreNotEqual(WebGreaseContext.ComputeFileHash(valueFileName), WebGreaseContext.ComputeContentHash(Value));

            File.WriteAllText(valueFileName, Value, Encoding.Unicode);
            Assert.AreEqual(WebGreaseContext.ComputeFileHash(valueFileName), WebGreaseContext.ComputeContentHash(Value, Encoding.Unicode));
            Assert.AreNotEqual(WebGreaseContext.ComputeFileHash(valueFileName), WebGreaseContext.ComputeContentHash(Value));
        }   

        #endregion
    }
}