// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImageMap.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Represents Log file that is used to store the Assemble/non-assemble file with other details.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.ImageAssemble
{
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>Represents Log file that is used to store the Assemble/non-assemble file with other details.</summary>
    /// <example><![CDATA[
    /// <?xml version="1.0" encoding="utf-8"?>
    /// <images padding="5">
    /// <output file="c:\temp\output\spriteimage.gif">
    /// <input>
    /// <originalfile>c:\images\input\1.gif</originalfile>
    /// <width>55</width>
    /// <height>45</height>
    /// <xposition>0</xposition>
    /// <yposition>0</yposition>
    /// </input>
    /// <input>
    /// <originalfile>c:\images\input\2.gif</originalfile>
    /// <width>55</width>
    /// <height>45</height>
    /// <xposition>-60</xposition>
    /// <yposition>-70</yposition>
    /// </input>
    /// </output>
    /// <output file="c:\temp\output\spriteimage.jpg">
    /// <input>
    /// <originalfile>c:\images\input\1.jpg</originalfile>
    /// <width>55</width>
    /// <height>45</height>
    /// <xposition>0</xposition>
    /// <yposition>0</yposition>
    /// </input>
    /// <input>
    /// <originalfile>c:\images\input\2.jpeg</originalfile>
    /// <width>55</width>
    /// <height>45</height>
    /// <xposition>-60</xposition>
    /// <yposition>-70</yposition>
    /// </input>
    /// </output>
    /// <output file="c:\temp\output\spriteimage.png">
    /// <input>
    /// <originalfile>c:\images\input\1.png</originalfile>
    /// <width>55</width>
    /// <height>45</height>
    /// <xposition>0</xposition>
    /// <yposition>0</yposition>
    /// </input>
    /// <input>
    /// <originalfile>c:\images\input\2.png</originalfile>
    /// <width>55</width>
    /// <height>45</height>
    /// <xposition>-60</xposition>
    /// <yposition>-70</yposition>
    /// </input>
    /// </output>
    /// <output file="">
    /// <input>
    /// <originalfile>c:\images\input\2.gif</originalfile>
    /// <comment>Animated GIF</comment>
    /// </input>
    /// </output>
    /// </images>
    /// ]]></example>
    internal sealed class ImageMap
    {
        /// <summary>
        /// Xml Version
        /// </summary>
        private const string XmlVersion = "1.0";

        /// <summary>
        /// Xml Encoding
        /// </summary>
        private const string XmlEncoding = "UTF-8";

        /// <summary>
        /// Stand-alone flag
        /// </summary>
        private const string XmlStandAlone = "yes";

        /// <summary>
        /// Root node name
        /// </summary>
        private const string RootNode = "images";

        /// <summary>
        /// Image node name
        /// </summary>
        private const string ImageNode = "input";

        /// <summary>
        /// Original file node name
        /// </summary>
        private const string OriginalFile = "originalfile";

        /// <summary>
        /// Generated file node name
        /// </summary>
        private const string GeneratedFile = "file";

        /// <summary>
        /// Width node name
        /// </summary>
        private const string Width = "width";

        /// <summary>
        /// Height node name
        /// </summary>
        private const string Height = "height";

        /// <summary>
        /// X-Position node name
        /// </summary>
        private const string XPosition = "xposition";

        /// <summary>
        /// Y-Position node name
        /// </summary>
        private const string YPosition = "yposition";

        /// <summary>
        /// Position of Input image for Vertical Packing Scheme (L for Left / R for Right).
        /// In case of other Packing Schemes, it should be empty string.
        /// </summary>
        private const string PositionInSprite = "positioninsprite";

        /// <summary>
        /// Input node
        /// </summary>
        private const string InputNode = "input";

        /// <summary>
        /// Output node
        /// </summary>
        private const string OutputNode = "output";

        /// <summary>
        /// Comment node
        /// </summary>
        private const string CommentNode = "comment";

        /// <summary>
        /// Padding attribute
        /// </summary>
        private const string Padding = "padding";

        /// <summary>
        /// Log file name
        /// </summary>
        private readonly string mapFileName;

        /// <summary>
        /// Root element of Log Xml file
        /// </summary>
        private readonly XElement root;

        /// <summary>
        /// XDocument used for generating the log
        /// </summary>
        private readonly XDocument xdoc;

        /// <summary>
        /// Current Output node
        /// </summary>
        private XElement currentOutputNode;

        /// <summary>
        /// Node containing files that are not assembled
        /// </summary>
        private XElement notAssembledNode;

        /// <summary>Initializes a new instance of the ImageMap class.</summary>
        /// <param name="mapFileName">Xml log file name to save this log.</param>
        internal ImageMap(string mapFileName)
        {
            this.xdoc = new XDocument();
            this.xdoc.Declaration = new XDeclaration(XmlVersion, XmlEncoding, XmlEncoding);
            this.root = new XElement(RootNode);
            this.xdoc.AddFirst(this.root);
            this.mapFileName = mapFileName;
        }

        /// <summary>
        /// Prevents a default instance of the ImageMap class from being created.
        /// </summary>
        private ImageMap()
        {
        }

        /// <summary>Override method to log the file name that cannot be assembled.</summary>
        /// <remarks>e.g. Animated GIF files will not be assembled.</remarks>
        /// <param name="notAssembledFile">Not assembled file name</param>
        /// <param name="comment">Comments, if any</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308", Justification = "This is by design as all the node names are used in lower case.")]
        internal void AppendToXml(string notAssembledFile, string comment)
        {
            // Fix for bug# 962020
            // Add not assembled node only if there are files which are not assembled
            if (this.notAssembledNode == null)
            {
                this.notAssembledNode = new XElement(OutputNode);
                this.notAssembledNode.SetAttributeValue(GeneratedFile, string.Empty);
                this.root.Add(this.notAssembledNode);
            }

            var inputNode = new XElement(InputNode);
            inputNode.Add(new XElement(OriginalFile, notAssembledFile.ToLowerInvariant()));
            inputNode.Add(new XElement(CommentNode, comment));

            this.notAssembledNode.Add(inputNode);
        }

        /// <summary>Override method to log the assembled file with details.</summary>
        /// <param name="originalFile">Original file name</param>
        /// <param name="genFile">Assembled file name</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <param name="posX">X-position in assembled image</param>
        /// <param name="posY">Y-position in assembled image</param>
        /// <param name="comment">comment</param>
        /// <param name="addOutputNode">Bool flag for adding output node</param>
        /// <param name="posSprite">Nullable ImagePosition for current image in the sprite (Left / Right). For horizontal packing scheme provide null.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308", Justification = "This is by design as all the node names are used in lower case.")]
        internal void AppendToXml(string originalFile, string genFile, int width, int height, int posX, int posY, string comment, bool addOutputNode, ImagePosition? posSprite)
        {
            if (addOutputNode)
            {
                this.currentOutputNode = new XElement(OutputNode);
                this.currentOutputNode.SetAttributeValue(GeneratedFile, genFile);
                this.root.Add(this.currentOutputNode);
            }

            var element = new XElement(ImageNode);
            element.Add(new XElement(OriginalFile, originalFile.ToLowerInvariant()));
            element.Add(new XElement(Width, width.ToString(CultureInfo.InvariantCulture)));
            element.Add(new XElement(Height, height.ToString(CultureInfo.InvariantCulture)));
            element.Add(new XElement(XPosition, posX.ToString(CultureInfo.InvariantCulture)));
            element.Add(new XElement(YPosition, posY.ToString(CultureInfo.InvariantCulture)));
            if (!string.IsNullOrEmpty(comment))
            {
                element.Add(new XElement(CommentNode, comment));
            }

            if (posSprite.HasValue)
            {
                element.Add(new XElement(PositionInSprite, posSprite.ToString()));
            }

            this.currentOutputNode.Add(element);
        }

        /// <summary>adding padding value attribute to root node</summary>
        /// <param name="padding">Padding value as string</param>
        internal void AppendPadding(string padding)
        {
            this.root.SetAttributeValue(Padding, padding);
        }

        /// <summary>Saves log as XML file</summary>
        internal void SaveXmlMap()
        {
            this.xdoc.Save(this.mapFileName);
        }

        /// <summary>Updates Assembled Image name with new Name passed.</summary>
        /// <param name="oldName">Existing file name</param>
        /// <param name="newName">New file name</param>
        /// <returns>True if successful else false</returns>
        internal bool UpdateAssembledImageName(string oldName, string newName)
        {
            var isUpdated = false;

            // Select the output node with existing file name
            var node = from outNode in this.root.Elements("output")
                       where outNode.Attribute("file") != null && oldName == outNode.Attribute("file").Value
                       select outNode;

            if (node.Count() > 0)
            {
                var elem = node.First();
                // No need for checking file attribute exist or not as
                // it is already filtered above
                elem.Attribute("file").Value = newName;
                this.SaveXmlMap();
                isUpdated = true;
            }

            return isUpdated;
        }
    }
}
