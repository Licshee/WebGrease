namespace ImageAssemble.Tests
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WebGrease.Css.ImageAssemblyAnalysis;
    using WebGrease.ImageAssemble;

    /// <summary>This is a test class for ArgumentParserTest and is intended
    /// to contain all ArgumentParserTest Unit Tests</summary>
    [TestClass]
    public class ArgumentParserTest
    {
        /// <summary>
        /// Sprite packing type (Horizontal/Vertical)
        /// </summary>
        private const string PackingScheme = "/packingscheme:";

        /// <summary>
        /// Output Directory option
        /// </summary>
        public const string OutputDirectory = "/outputDirectory:";

        /// <summary>
        /// Sprite assembled file name
        /// </summary>
        private const string SpriteName = "/spriteimage:";

        /// <summary>
        /// Horizontal orientation
        /// </summary>
        public const string Horizontal = "HORIZONTAL";

        /// <summary>
        /// Vertical orientation
        /// </summary>
        public const string Vertical = "VERTICAL";

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for ParseArguments</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest1()
        {
            string[] args = null;
            ArgumentParser_Accessor.ParseArguments(args, null);
        }

        /// <summary>The parse test 1_1.</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest1_1()
        {
            var args = new string[] { };
            ArgumentParser_Accessor.ParseArguments(args, null);
        }

        /// <summary>A test for ParseArguments</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest2()
        {
            var args = new[] { @"/inputdirectory:" };
            ArgumentParser_Accessor.ParseArguments(args, null);
        }

        /// <summary>A test for ParseArguments</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest2_1()
        {
            var args = new[] { @"/inputdirectory:" + Environment.CurrentDirectory };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(args, null);
        }

        /// <summary>A test for ParseArguments</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest2_2()
        {
            var args = new[] { @"/i:" };
            ArgumentParser_Accessor.ParseArguments(args, null);
        }

        /// <summary>A test for ParseArguments</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest2_3()
        {
            var args = new[] { @"/i:" + Environment.CurrentDirectory };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(args, null);
        }

        /// <summary>A test for ParseArguments</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest3()
        {
            var args = new[] { @"/inputfilepaths:"};
            ArgumentParser_Accessor.ParseArguments(args, null);
        }

        /// <summary>A test for ParseArguments</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest3_1()
        {
            var args = new[] { @"/inputfilepaths:" + Path.Combine(Environment.CurrentDirectory, "11.gif") };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(args, null);
        }

        /// <summary>A test for ParseArguments</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest3_2()
        {
            var args = new[] { @"/f:" };
            ArgumentParser_Accessor.ParseArguments(args, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest3_3()
        {
            var input = new[] { @"/f:" + Path.Combine(Environment.CurrentDirectory, "11.gif") };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest4()
        {
            var input = new[] { @"/spriteimage:" };
            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest4_1()
        {
            var input = new[] { @"/s:" };
            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest5()
        {
            var input = new[] { @"/logfile:" };
            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest5_1()
        {
            var input = new[] { @"/logfile:" + Path.Combine(Environment.CurrentDirectory, "LogFile.xml") };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest5_2()
        {
            var input = new[] { @"/l:" };
            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest5_3()
        {
            var input = new[] { @"/l:" + Path.Combine(Environment.CurrentDirectory, "LogFile.xml") };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest6()
        {
            var input = new[] { @"/outputdirectory:" };
            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest6_1()
        {
            var input = new[] { @"/outputdirectory:" + Environment.CurrentDirectory };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest7()
        {
            var input = new[] { @"/padding:" };
            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest7_1()
        {
            var input = new[] { @"/padding:5" };
            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>The parse test_ negative padding value.</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest_NegativePaddingValue()
        {
            var input = new[] { @"/f:" + this.GetImagePath("11.gif") + "|L;" + this.GetImagePath("D18A4E36CE14D7C371DD69509C13AA.png") + "|L;" + this.GetImagePath("F8EFA4EA57AAB97285B2EC97127DF3.jpg") + "|L", "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/padding:-10" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>The parse test_ padding out of range value.</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest_PaddingOutOfRangeValue()
        {
            var input = new[] { @"/f:" + this.GetImagePath("11.gif") + "|L;" + this.GetImagePath("D18A4E36CE14D7C371DD69509C13AA.png") + "|L;" + this.GetImagePath("F8EFA4EA57AAB97285B2EC97127DF3.jpg") + "|L", "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/padding:1025" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest8()
        {
            var input = new[] { @"/?" };
            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest9()
        {
            var input = new[] { @"/test:" };
            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        public void ParseTest9_1()
        {
            var input = new[] { @"/f:" + this.GetImagePath("11.gif") + "|L;" + this.GetImagePath("D18A4E36CE14D7C371DD69509C13AA.png") + "|L;" + this.GetImagePath("F8EFA4EA57AAB97285B2EC97127DF3.jpg") + "|L", "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/padding:5" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
            Assert.AreEqual(Vertical, ArgumentParser_Accessor.ArgumentValueData[PackingScheme], true);
            Assert.AreEqual(ArgumentParser_Accessor.DefaultSpriteName, ArgumentParser_Accessor.ArgumentValueData[SpriteName]);
            Assert.AreEqual(5, int.Parse(ArgumentParser_Accessor.ArgumentValueData[ArgumentParser_Accessor.Padding]));
            Assert.AreEqual(6, ArgumentParser_Accessor.ArgumentValueData.Count);
            Assert.IsNotNull(ArgumentParser_Accessor.inputImageList);
            Assert.IsTrue(ArgumentParser_Accessor.inputImageList.Count == 3);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        public void ParseTest9_2()
        {
            var input = new[] { @"/f:" + this.GetImagePath("11.gif") + "|L;" + this.GetImagePath("D18A4E36CE14D7C371DD69509C13AA.png") + "|R;" + this.GetImagePath("F8EFA4EA57AAB97285B2EC97127DF3.jpg") + "|R", "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/p:5", "/packingscheme:horizontal", "/spriteimage:MySprite" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
            Assert.AreEqual(Horizontal, ArgumentParser_Accessor.ArgumentValueData[PackingScheme], true);
            Assert.AreEqual("MySprite", ArgumentParser_Accessor.ArgumentValueData[SpriteName]);
            Assert.AreEqual(5, int.Parse(ArgumentParser_Accessor.ArgumentValueData[ArgumentParser_Accessor.Padding]));
            Assert.AreEqual(6, ArgumentParser_Accessor.ArgumentValueData.Count);
            Assert.IsNotNull(ArgumentParser_Accessor.inputImageList);
            Assert.IsTrue(ArgumentParser_Accessor.inputImageList.Count == 3);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        public void ParseTest9_3()
        {
            var input = new[] { @"/f:" + this.GetImagePath("11.gif") + "|R;" + this.GetImagePath("D18A4E36CE14D7C371DD69509C13AA.png") + "|R;" + this.GetImagePath("F8EFA4EA57AAB97285B2EC97127DF3.jpg") + "|L", "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/p:0", "/packingscheme:horizontal", "/spriteimage:MySprite" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
            Assert.AreEqual(Horizontal, ArgumentParser_Accessor.ArgumentValueData[PackingScheme], true);
            Assert.AreEqual("MySprite", ArgumentParser_Accessor.ArgumentValueData[SpriteName]);
            Assert.AreEqual(0, int.Parse(ArgumentParser_Accessor.ArgumentValueData[ArgumentParser_Accessor.Padding]));
            Assert.AreEqual(6, ArgumentParser_Accessor.ArgumentValueData.Count);
            Assert.IsNotNull(ArgumentParser_Accessor.inputImageList);
            Assert.IsTrue(ArgumentParser_Accessor.inputImageList.Count == 3);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        public void ParseTest9_4()
        {
            var input = new[] { @"/f:" + this.GetImagePath("11.gif") + "|L;" + this.GetImagePath("D18A4E36CE14D7C371DD69509C13AA.png") + "|L;" + this.GetImagePath("F8EFA4EA57AAB97285B2EC97127DF3.jpg") + "|L", "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/p:100", "/packingscheme:horizontal", "/spriteimage:MySprite" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
            Assert.AreEqual(Horizontal, ArgumentParser_Accessor.ArgumentValueData[PackingScheme], true);
            Assert.AreEqual("MySprite", ArgumentParser_Accessor.ArgumentValueData[SpriteName]);
            Assert.AreEqual(100, int.Parse(ArgumentParser_Accessor.ArgumentValueData[ArgumentParser_Accessor.Padding]));
            Assert.AreEqual(6, ArgumentParser_Accessor.ArgumentValueData.Count);
            Assert.IsNotNull(ArgumentParser_Accessor.inputImageList);
            Assert.IsTrue(ArgumentParser_Accessor.inputImageList.Count == 3);
        }

        /// <summary>Value not provided for Padding</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest9_5()
        {
            var input = new[] { @"/f:" + this.GetImagePath("11.gif") + "|L;" + this.GetImagePath("D18A4E36CE14D7C371DD69509C13AA.png") + "|L;" + this.GetImagePath("F8EFA4EA57AAB97285B2EC97127DF3.jpg") + "|L", "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/p:", "/packingscheme:horizontal", "/spriteimage:MySprite" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>Missing parameter Padding</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest9_5_1()
        {
            var input = new[] { @"/f:" + this.GetImagePath("11.gif") + "|L;" + this.GetImagePath("D18A4E36CE14D7C371DD69509C13AA.png") + "|L;" + this.GetImagePath("F8EFA4EA57AAB97285B2EC97127DF3.jpg") + "|L", "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/packingscheme:horizontal", "/spriteimage:MySprite" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>Invalid value for parameter Padding</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest9_5_2()
        {
            var input = new[] { @"/f:" + this.GetImagePath("11.gif") + "|L;" + this.GetImagePath("D18A4E36CE14D7C371DD69509C13AA.png") + "|L;" + this.GetImagePath("F8EFA4EA57AAB97285B2EC97127DF3.jpg") + "|L", "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/p:wrongvalue", "/packingscheme:horizontal", "/spriteimage:MySprite" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>Single Image file</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest9_6()
        {
            var input = new[] { @"/f:" + this.GetImagePath("11.gif") + "|L;" + this.GetImagePath("D18A4E36CE14D7C371DD69509C13AA.png") + "|L;" + this.GetImagePath("F8EFA4EA57AAB97285B2EC97127DF3.jpg") + "|L", "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/padding:5", "/packingscheme:test", "/spriteimage:MySprite" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>Duplicate Image file</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest9_7()
        {
            var input = new[] { @"/f:" + this.GetImagePath("11.gif") + "|L;" + this.GetImagePath("D18A4E36CE14D7C371DD69509C13AA.png") + "|L;" + this.GetImagePath("11.gif") + "|L", "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/padding:5", "/packingscheme:test", "/spriteimage:MySprite" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>Not providing Image Position</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest9_8_1()
        {
            var input = new[] { @"/f:" + this.GetImagePath("11.gif") + ";" + this.GetImagePath("D18A4E36CE14D7C371DD69509C13AA.png") + ";" + this.GetImagePath("11.gif"), "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/padding:5", "/packingscheme:Vertical", "/spriteimage:MySprite" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>Providing wrong Image Position</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest9_8_2()
        {
            var input = new[] { @"/f:" + this.GetImagePath("11.gif") + "|A;" + this.GetImagePath("D18A4E36CE14D7C371DD69509C13AA.png") + "|R;" + this.GetImagePath("11.gif") + "|L", "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/padding:5", "/packingscheme:Vertical", "/spriteimage:MySprite" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>Providing blank Image Position</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest9_8_3()
        {
            var input = new[] { @"/f:" + this.GetImagePath("11.gif") + "| ;" + this.GetImagePath("D18A4E36CE14D7C371DD69509C13AA.png") + "|;" + this.GetImagePath("11.gif") + "|", "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/padding:5", "/packingscheme:Vertical", "/spriteimage:MySprite" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>Providing blank Image Position</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest9_8_4()
        {
            var input = new[] { @"/f: ;; ", "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/padding:5", "/packingscheme:Vertical", "/spriteimage:MySprite" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest10()
        {
            // missing padding
            var input = new[] { @"/inputdirectory:" + Environment.CurrentDirectory, "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest10_1()
        {
            // missing logfile
            var input = new[] { @"/inputdirectory:" + Environment.CurrentDirectory, "/outputdirectory:" + Environment.CurrentDirectory, "/padding:5" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest10_2()
        {
            // missing outputdirectory
            var input = new[] { @"/inputdirectory:" + Environment.CurrentDirectory, "/logfile:test.xml", "/padding:5" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest10_3()
        {
            // missing inputdirectory
            var input = new[] { @"/outputdirectory:" + Environment.CurrentDirectory, "/logfile:test.xml", "/padding:5" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest11()
        {
            var input = new[] { @"/inputdirectory:C:\Test_is_here" , "/outputdirectory:" + Environment.CurrentDirectory, "/logfile:ReplaceLog.xml", "/padding:2" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest11_1()
        {
            // output dir doesnot exist
            var input = new[] { @"/f:" + this.GetImagePath("11.gif") + "|L", @"/outputdirectory:C:\DoesnotExist", "/logfile:ReplaceLog.xml", "/padding:2" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest12()
        {
            var input = new[] { @"/inputdirectory:C:\Temp", "/spriteimage:AssembledImage.gif", "/logfile:ReplaceLog.xml", "/pack:Horizontal" };
            if (ArgumentParser_Accessor.ArgumentValueData != null)
            {
                ArgumentParser_Accessor.ArgumentValueData.Clear();
            }

            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest13()
        {
            var input = new[] { @"/ps:" };
            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for Parse</summary>
        [TestMethod]
        [ExpectedException(typeof(ImageAssembleException))]
        public void ParseTest13_1()
        {
            var input = new[] { @"/packingscheme:" };
            ArgumentParser_Accessor.ParseArguments(input, null);
        }

        /// <summary>A test for ParseSpritePackingType</summary>
        [TestMethod]
        public void ParseSpritePackingTypeTest2()
        {
            var param = "vertical";
            var expected = SpritePackingType_Accessor.Vertical;
            SpritePackingType_Accessor actual;
            actual = ArgumentParser_Accessor.ParseSpritePackingType(param);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>A test for ParseSpritePackingType</summary>
        [TestMethod]
        public void ParseSpritePackingTypeTest3()
        {
            var param = "Auto";
            var expected = SpritePackingType_Accessor.Vertical;
            SpritePackingType_Accessor actual;
            actual = ArgumentParser_Accessor.ParseSpritePackingType(param);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>A test for ParseSpritePackingType</summary>
        [TestMethod]
        public void ParseSpritePackingTypeTest5()
        {
            var param = "wrongone";
            var expected = SpritePackingType_Accessor.Vertical;
            SpritePackingType_Accessor actual;
            actual = ArgumentParser_Accessor.ParseSpritePackingType(param);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>The get image path.</summary>
        /// <param name="imageName">The image name.</param>
        /// <returns>The get image path.</returns>
        public string GetImagePath(string imageName)
        {
            return Path.Combine(Environment.CurrentDirectory, "InputImages", imageName);
        }
    }
}
