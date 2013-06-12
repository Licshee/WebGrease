﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArgumentParser.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This class parses the command line arguments
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.ImageAssemble
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Css.ImageAssemblyAnalysis;

    /// <summary>This class parses the command line arguments</summary>
    internal static class ArgumentParser
    {
        #region Constants

        /// <summary>
        /// Input Directory option
        /// </summary>
        internal const string DirectoryName = "/inputdirectory:";

        /// <summary>
        /// Shorthand for Input directory
        /// </summary>
        internal const string ShorthandInputDirectory = "/i:";

        /// <summary>
        /// Input file string
        /// </summary>
        internal const string Paths = "/inputfilepaths:";

        /// <summary>
        /// Shorthand for Input File paths
        /// </summary>
        internal const string ShorthandInputFilePaths = "/f:";

        /// <summary>
        /// Output Directory option
        /// </summary>
        internal const string OutputDirectory = "/outputDirectory:";

        /// <summary>
        /// Sprite packing Scheme (Horizontal/Vertical)
        /// </summary>
        internal const string PackingScheme = "/packingscheme:";

        /// <summary>
        /// Shorthand for Packing Scheme
        /// </summary>
        internal const string ShorthandPackingScheme = "/ps:";

        /// <summary>
        /// Sprite assembled file name
        /// </summary>
        internal const string SpriteName = "/spriteimage:";

        /// <summary>
        /// Shorthand for Sprite Image name
        /// </summary>
        internal const string ShorthandSpriteImage = "/s:";

        /// <summary>
        /// Xml map log file name
        /// </summary>
        internal const string XmlMapName = "/logfile:";

        /// <summary>
        /// Shorthand for Log File name
        /// </summary>
        internal const string ShorthandLogFile = "/l:";

        /// <summary>
        /// Shorthand for Padding
        /// </summary>
        internal const string ShorthandPadding = "/p:";

        /// <summary>
        /// Padding between images (0 to 1024)
        /// </summary>
        internal const string Padding = "/padding:";

        /// <summary>
        /// Whether or not to dedup images
        /// </summary>
        internal const string Dedup = "/dedup:";

        /// <summary>
        /// Shorthand for Dedup
        /// </summary>
        internal const string ShorthandDedup = "/d:";

        /// <summary>
        /// Help Input parameter
        /// </summary>
        internal const string Question = "/?";

        /// <summary>
        /// Help Input parameter
        /// </summary>
        internal const string Help = "/help";

        /// <summary>
        /// Horizontal orientation
        /// </summary>
        internal const string Horizontal = "HORIZONTAL";

        /// <summary>
        /// Vertical orientation
        /// </summary>
        internal const string Vertical = "VERTICAL";

        /// <summary>
        /// Default Padding value
        /// </summary>
        internal const int DefaultPadding = 50;

        /// <summary>
        /// Minimum Padding value for JPEG images
        /// </summary>
        internal const int MinPadding = 0;

        /// <summary>
        /// Maximum Padding value for JPEG images
        /// </summary>
        internal const int MaxPadding = 1024;

        /// <summary>
        /// Default Name to be used for sprite image
        /// </summary>
        internal const string DefaultSpriteName = ""; // TODO: this is a work around for a unit test, which still uses a legacy logic path to image assembly. Refactor these tests to use the same procedure as the runtime code.

        /// <summary>
        /// Constant for Missing parameter for DirectoryName or FilePath
        /// </summary>
        private const string DirectoryOrFileName = DirectoryName + " or " + Paths;

        #endregion

        #region Fields

        /// <summary>
        /// List of InputImage collection
        /// </summary>
        private static IList<InputImage_Accessor> inputImageList;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Argument values collection data
        /// </summary>
        internal static Dictionary<string, string> ArgumentValueData
        {
            get;
            set;
        }

        /// <summary>
        /// Gets Missing Parameters value
        /// </summary>
        internal static ArrayList MissingParams
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets Readonly collection of InputImage List
        /// </summary>
        internal static ReadOnlyCollection<InputImage_Accessor> InputImageList
        {
            get
            {
                return inputImageList.ToList().AsReadOnly();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>Parses command line parameters and generates a list of image paths from it.</summary>
        /// <remarks>/inputdirectory and /inputfilepaths overrides inputImages parameter.
        /// i.e. When inputImages parameters is provided, its value will be used only when
        /// /inputdirectory and /inputfilepaths are not present in input parameters.</remarks>
        /// <param name="input">string array of input parameters.</param>
        /// <param name="inputImages">InputImage List that will be passed by ImageAssembleTask. For all other callers (Console App), this value will be NULL.</param>
        internal static void ParseArguments(string[] input, IList<InputImage_Accessor> inputImages)
        {
            var usage = string.Format(CultureInfo.CurrentCulture, ImageAssembleStrings.ToolUsageMessage, ArgumentParser.DefaultPadding.ToString(CultureInfo.CurrentCulture), ArgumentParser.MinPadding.ToString(CultureInfo.CurrentCulture), ArgumentParser.MaxPadding.ToString(CultureInfo.CurrentCulture));

            // Check if arguments are provided
            if (input == null || input.Length == 0)
            {
                throw new ImageAssembleException(string.Format(CultureInfo.CurrentCulture, ImageAssembleStrings.NoInputParametersMessage, usage));
            }
            else
            {
                // Check if duplicate input image parameters are present
                CheckImageFilesDuplicateParameter(input);

                // Perform some clean-up and/or initialization
                PreParsingLogic();

                // Set InputImageList if it is passed as parameter to this method
                if (inputImages != null)
                {
                    inputImageList = inputImages;
                }

                foreach (var ar in input)
                {
                    if (ar.StartsWith(DirectoryName, StringComparison.OrdinalIgnoreCase) || ar.StartsWith(ShorthandInputDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        // Directory name parsing
                        var dirName = ParseParameterValueFromArg(ar, DirectoryName, ShorthandInputDirectory);
                        ParseDirectoryValue(dirName, DirectoryName);
                        MissingParams.Remove(DirectoryOrFileName);
                    }
                    else if (ar.StartsWith(Paths, StringComparison.OrdinalIgnoreCase) || ar.StartsWith(ShorthandInputFilePaths, StringComparison.OrdinalIgnoreCase))
                    {
                        // Input string is semicolon separated file paths
                        // e.g. /i:C:\Users\v-niravd\Documents\Test\Images\Input\pic_karate.gif;C:\Users\v-niravd\Documents\Test\Images\Input\vaio.gif
                        var filePaths = ParseParameterValueFromArg(ar, Paths, ShorthandInputFilePaths);
                        ParseInputFilePaths(filePaths);
                        MissingParams.Remove(DirectoryOrFileName);
                    }
                    else if (ar.StartsWith(OutputDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        // Directory name parsing
                        var outputDir = string.Empty;
                        outputDir = ar.Substring(OutputDirectory.Length).Trim();
                        ParseDirectoryValue(outputDir, OutputDirectory);
                        MissingParams.Remove(OutputDirectory);
                    }
                    else if (ar.StartsWith(PackingScheme, StringComparison.OrdinalIgnoreCase) || ar.StartsWith(ShorthandPackingScheme, StringComparison.OrdinalIgnoreCase))
                    {
                        // Packing type parsing
                        var packing = ParseParameterValueFromArg(ar, PackingScheme, ShorthandPackingScheme);

                        if (!string.IsNullOrEmpty(packing))
                        {
                            if (packing.Equals(Horizontal, StringComparison.OrdinalIgnoreCase) || packing.Equals(Vertical, StringComparison.OrdinalIgnoreCase))
                            {
                                ArgumentValueData[PackingScheme] = packing;
                            }
                            else
                            {
                                ArgumentValueData.Remove(PackingScheme);
                                throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.InvalidInputParameterValueMessage, PackingScheme));
                            }
                        }
                        else
                        {
                            ArgumentValueData.Remove(PackingScheme);
                            throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.ValueMissingForInputParameterMessage, PackingScheme));
                        }
                    }
                    else if (ar.StartsWith(SpriteName, StringComparison.OrdinalIgnoreCase) || ar.StartsWith(ShorthandSpriteImage, StringComparison.OrdinalIgnoreCase))
                    {
                        // Sprite name parsing
                        var spriteName = ParseParameterValueFromArg(ar, SpriteName, ShorthandSpriteImage);

                        if (!string.IsNullOrEmpty(spriteName))
                        {
                            ArgumentValueData[SpriteName] = spriteName;
                        }
                        else
                        {
                            ArgumentValueData.Remove(SpriteName);
                            throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.ValueMissingForInputParameterMessage, SpriteName));
                        }
                    }
                    else if (ar.StartsWith(XmlMapName, StringComparison.OrdinalIgnoreCase) || ar.StartsWith(ShorthandLogFile, StringComparison.OrdinalIgnoreCase))
                    {
                        // Xml map file name parsing
                        var mapFileName = ParseParameterValueFromArg(ar, XmlMapName, ShorthandLogFile);
                        MissingParams.Remove(XmlMapName);
                        ParseLogFile(mapFileName);
                    }
                    else if (ar.StartsWith(ArgumentParser.Padding, StringComparison.OrdinalIgnoreCase) || ar.StartsWith(ShorthandPadding, StringComparison.OrdinalIgnoreCase))
                    {
                        // Padding parsing
                        var padding = ParseParameterValueFromArg(ar, ArgumentParser.Padding, ShorthandPadding);
                        MissingParams.Remove(ArgumentParser.Padding);
                        ParsePaddingValue(padding);
                    }
                    else if (ar.StartsWith(Dedup, StringComparison.OrdinalIgnoreCase) || ar.StartsWith(ShorthandDedup, StringComparison.OrdinalIgnoreCase))
                    {
                        // Dedup parsing
                        var dedup = ParseParameterValueFromArg(ar, ArgumentParser.Dedup, ShorthandDedup);
                        MissingParams.Remove(ArgumentParser.Dedup);
                        ParseDedupValue(dedup);
                    }
                    else if (ar.StartsWith(Question, StringComparison.OrdinalIgnoreCase) || ar.StartsWith(Help, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    else
                    {
                        // unknown input parameter
                        var paramName = ar.Substring(0, ar.IndexOf(":", StringComparison.OrdinalIgnoreCase) + 1);
                        throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.InvalidInputParameterMessage, paramName));
                    }
                }

                // Check missing parameters
                CheckMissingParameters(usage);
            }
        }

        /// <summary>Parses packing type from input param value.</summary>
        /// <param name="param">Input param value</param>
        /// <returns>SpritePackingType value</returns>
        internal static SpritePackingType ParseSpritePackingType(string param)
        {
            SpritePackingType packingType;
            switch (param.ToUpperInvariant())
            {
                case Horizontal:
                    packingType = SpritePackingType.Horizontal;
                    break;
                case Vertical:
                    packingType = SpritePackingType.Vertical;
                    break;
                default:
                    packingType = SpritePackingType.Vertical;
                    break;
            }

            return packingType;
        }

        #endregion

        #region Private Methods

        /// <summary>Parses parameter value from argument.</summary>
        /// <param name="arg">Commandline argument passed.</param>
        /// <param name="fullName">Fullname of parameter to be parsed.</param>
        /// <param name="shortHand">Shorthand name of parameter to be parsed.</param>
        /// <returns>Value of parameter.</returns>
        private static string ParseParameterValueFromArg(string arg, string fullName, string shortHand)
        {
            // Directory name parsing
            var paramValue = string.Empty;

            if (arg.StartsWith(fullName, StringComparison.OrdinalIgnoreCase))
            {
                paramValue = arg.Substring(fullName.Length).Trim();
            }
            else
            {
                paramValue = arg.Substring(shortHand.Length).Trim();
            }

            return paramValue;
        }

        /// <summary>Parses image file paths.</summary>
        /// <param name="filePaths">Semi-colon separated list of file paths.</param>
        private static void ParseInputFilePaths(string filePaths)
        {
            if (string.IsNullOrEmpty(filePaths) || filePaths.Trim().Length == 0)
            {
                throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.ValueMissingForInputParameterMessage, Paths));
            }

            const string Left = "L";
            const string Right = "R";

            // The code below generates InputImageList from the filepath and position
            // pair values provided as input.
            var imageList = new List<InputImage_Accessor>();
            var fileNames = filePaths.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var pathValue in fileNames)
            {
                var path = pathValue.Trim();
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.Contains("|"))
                    {
                        var pairValue = path.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                        // Check filepath and position are not empty
                        if (pairValue.Length == 2 && pairValue[0].Trim().Length > 0 && pairValue[1].Trim().Length > 0)
                        {
                            // Throw an exception if invalid value is provided for Image Position
                            if (!pairValue[1].Trim().Equals(Right, StringComparison.OrdinalIgnoreCase) && !pairValue[1].Trim().Equals(Left, StringComparison.OrdinalIgnoreCase))
                            {
                                throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.InvalidImagePositionMessage, pairValue[1], pairValue[0], string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.ImagePositionValues, Left, Right)));
                            }

                            var inputImage = new InputImage_Accessor(pairValue[0].Trim());
                            inputImage.Position = pairValue[1].Trim().Equals(Right, StringComparison.OrdinalIgnoreCase) ? ImagePosition.Right : ImagePosition.Left;

                            // Verify bug# 956706 (Image Assemble Tool: When specifiying individual input file paths, duplicates are not detected and are drawn to the assembled image)
                            var result = imageList.Where(il => il.AbsoluteImagePath.Equals(inputImage.AbsoluteImagePath, StringComparison.OrdinalIgnoreCase) && il.Position == inputImage.Position);
                            if (result != null && result.Count() > 0)
                            {
                                // If Image already exists then throw exception for duplicate input files
                                throw new ImageAssembleException(string.Format(CultureInfo.CurrentCulture, ImageAssembleStrings.DuplicateInputFilePathsMessage, Paths, inputImage.AbsoluteImagePath));
                            }
                            else
                            {
                                imageList.Add(inputImage);
                            }
                        }
                        else
                        {
                            throw new ImageAssembleException(string.Format(CultureInfo.CurrentCulture, ImageAssembleStrings.InputFilesPathAndPositionMessage, pathValue));
                        }
                    }
                    else
                    {
                        throw new ImageAssembleException(string.Format(CultureInfo.CurrentCulture, ImageAssembleStrings.InputFilesMissingPositionMessage, pathValue));
                    }
                }
            }

            // If there are not InputImage in the list then throw an exception
            if (imageList.Count == 0)
            {
                throw new ImageAssembleException(ImageAssembleStrings.NoInputFileToProcessMessage);
            }

            // Thrown an exception if there are image file paths provides that do not exist.
            var notExist = imageList.Where(il => !File.Exists(il.AbsoluteImagePath)).ToList();
            if (notExist != null && notExist.Count > 0)
            {
                var sb = new StringBuilder(string.Format(CultureInfo.CurrentCulture, ImageAssembleStrings.IgnoredFilesMessage));
                sb.Append("\n");

                foreach (var ne in notExist)
                {
                    sb.Append(ne.AbsoluteImagePath + "\n");
                }

                throw new ImageAssembleException(sb.ToString());
            }

            // Set InputImageList for the class
            inputImageList = imageList;
        }

        /// <summary>Parses padding value from input parameter.</summary>
        /// <param name="padding">Padding value string.</param>
        private static void ParsePaddingValue(string padding)
        {
            var padValue = 0;

            // If value is provided then parse it
            // else throw an exception
            if (!string.IsNullOrEmpty(padding))
            {
                // Try parsing the value passed
                // If contains invalid value then throw an exception
                if (int.TryParse(padding.Trim(), out padValue))
                {
                    // Check if the padding is outside valid limits (0 to 1024)
                    // If it is, throw an exception
                    if (padValue < ArgumentParser.MinPadding || padValue > ArgumentParser.MaxPadding)
                    {
                        throw new ImageAssembleException(string.Format(CultureInfo.CurrentCulture, ImageAssembleStrings.InvalidPaddingValueMessage, padding, ArgumentParser.MinPadding.ToString(CultureInfo.CurrentCulture), ArgumentParser.MaxPadding.ToString(CultureInfo.CurrentCulture)));
                    }

                    ArgumentValueData[Padding] = padValue.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    ArgumentValueData.Remove(Padding);
                    throw new ImageAssembleException(string.Format(CultureInfo.CurrentCulture, ImageAssembleStrings.InvalidInputParameterValueMessage, Padding));
                }
            }
            else
            {
                ArgumentValueData.Remove(Padding);
                throw new ImageAssembleException(string.Format(CultureInfo.CurrentCulture, ImageAssembleStrings.ValueMissingForInputParameterMessage, Padding));
            }
        }

        /// <summary>Parses dedup value from input parameter.</summary>
        /// <param name="dedup">Dedup value string.</param>
        private static void ParseDedupValue(string dedup)
        {
            var dedupValue = false;


            // If value is provided then parse it
            if (!string.IsNullOrEmpty(dedup))
            {
                // Try parsing the value passed
                // If contains invalid value then throw an exception
                if (bool.TryParse(dedup.Trim(), out dedupValue))
                {
                    ArgumentValueData[Dedup] = dedupValue.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    ArgumentValueData.Remove(Dedup);
                    throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.InvalidInputParameterValueMessage, Dedup));
                }
            }
            else
            {
                ArgumentValueData.Remove(Dedup);
                throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.ValueMissingForInputParameterMessage, Dedup));
            }
        }

        /// <summary>Parses directory value from input parameter.</summary>
        /// <param name="dirName">Directory value string.</param>
        /// <param name="paramName">Input parameter name.</param>
        private static void ParseDirectoryValue(string dirName, string paramName)
        {
            var formatMessageDoesnotExist = string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.DirectoryDoesNotExistMessage, dirName, paramName);
            var formatMessageNotSpecified = string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.ValueMissingForInputParameterMessage, paramName);
            var flagInputDirectory = true;

            if (paramName.Equals(OutputDirectory, StringComparison.OrdinalIgnoreCase))
            {
                flagInputDirectory = false;
                formatMessageDoesnotExist = string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.DirectoryDoesNotExistMessage, dirName, paramName);
                formatMessageNotSpecified = string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.ValueMissingForInputParameterMessage, paramName);
            }

            if (!string.IsNullOrEmpty(dirName))
            {
                if (Directory.Exists(dirName))
                {
                    if (flagInputDirectory)
                    {
                        inputImageList = ConvertToInputImageList(Directory.GetFiles(dirName));

                        // Fix for Bug# 956100
                        // If there are not images present then thrown an exception
                        if (InputImageList.Count == 0)
                        {
                            throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.NoInputFilesMessage, dirName));
                        }
                    }
                    else
                    {
                        ArgumentValueData.Add(OutputDirectory, dirName);
                    }
                }
                else
                {
                    throw new ImageAssembleException(formatMessageDoesnotExist);
                }
            }
            else
            {
                throw new ImageAssembleException(formatMessageNotSpecified);
            }
        }

        /// <summary>Parses log file name from input value</summary>
        /// <param name="mapFileName">Log file name.</param>
        private static void ParseLogFile(string mapFileName)
        {
            if (string.IsNullOrEmpty(mapFileName))
            {
                throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.ValueMissingForInputParameterMessage, XmlMapName));
            }
            else
            {
                var directory = Path.GetDirectoryName(mapFileName);
                if (!string.IsNullOrEmpty(directory))
                {
                    // Create all directories
                    Directory.CreateDirectory(directory);
                }

                ArgumentValueData.Add(XmlMapName, mapFileName);
            }
        }

        /// <summary>Checks missing input parameters and if found, raises an exception</summary>
        /// <param name="usageMessage">Tool usage message</param>
        private static void CheckMissingParameters(string usageMessage)
        {
            // This check is in case the Tool is invoked from ImageAssembleTask and it has set
            // InputImageList without providing Input Directory or FilePath parameter (which are mandatory).
            // This call will be considered valid as we have got the image data to proceed.
            if (ArgumentParser.MissingParams.Contains(DirectoryOrFileName) && inputImageList != null)
            {
                // If there are not InputImage in the parameter passed then throw an exception
                // Else use that values (remove missing parameters)
                if (inputImageList.Count == 0)
                {
                    throw new ImageAssembleException(ImageAssembleStrings.InputImageListNoImageMessage);
                }
                else
                {
                    ArgumentParser.MissingParams.Remove(DirectoryOrFileName);
                }
            }

            // If any mandatory parameter is missing throw exception for the same
            if (ArgumentParser.MissingParams.Count > 0)
            {
                // Generate string of missing parameter names
                var missingParamNames = new StringBuilder();
                foreach (string name in ArgumentParser.MissingParams)
                {
                    missingParamNames.Append(name + "\n");
                }

                throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.MissingInputParameterMessage, missingParamNames) + "\n" + usageMessage);
            }
        }

        /// <summary>Checks if both input image files parameters are specified. If so,
        /// raises an exception.</summary>
        /// <param name="input">Arguments passed to the tool.</param>
        private static void CheckImageFilesDuplicateParameter(string[] input)
        {
            var inputDirectorySpecified = false;

            // Check if Directory name is specified
            var result = input.Where(inp => inp.IndexOf(DirectoryName, StringComparison.OrdinalIgnoreCase) != -1 || inp.IndexOf(ShorthandInputDirectory, StringComparison.OrdinalIgnoreCase) != -1);
            if (result != null && result.Count() > 0)
            {
                inputDirectorySpecified = true;
            }

            // Check if Image File paths are also specified
            var pathsResult = input.Where(inp => inp.IndexOf(Paths, StringComparison.OrdinalIgnoreCase) != -1 || inp.IndexOf(ShorthandInputFilePaths, StringComparison.OrdinalIgnoreCase) != -1);
            if (pathsResult != null && pathsResult.Count() > 0 && inputDirectorySpecified)
            {
                throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, ImageAssembleStrings.InputFilesDuplicateParameterMessage, DirectoryName, Paths));
            }
        }

        /// <summary>Converts string array to List of InputImage. The conversion sets Image position for
        /// Individual Images to Left.</summary>
        /// <param name="imagePaths">String array of Image paths.</param>
        /// <returns>List of InputImage objects.</returns>
        internal static List<InputImage_Accessor> ConvertToInputImageList(string[] imagePaths)
        {
            if (imagePaths == null)
            {
                throw new ArgumentNullException("imagePaths");
            }

            var inputImages = new List<InputImage_Accessor>();

            foreach (var imagePath in imagePaths)
            {
                if (!string.IsNullOrEmpty(imagePath))
                {
                    var inputImage = new InputImage_Accessor(imagePath);
                    inputImages.Add(inputImage);
                }
            }

            return inputImages;
        }

        /// <summary>Performs static members initialization / clean-up.</summary>
        private static void PreParsingLogic()
        {
            // If ArgumentValueData is not initialized then initialize it
            // else clear it.
            if (ArgumentValueData == null)
            {
                ArgumentValueData = new Dictionary<string, string>();
            }
            else
            {
                ArgumentValueData.Clear();
            }

            // Add default packing type value as Vertical
            ArgumentValueData.Add(PackingScheme, Vertical);

            // Add default sprite name
            ArgumentValueData.Add(SpriteName, DefaultSpriteName);

            // Add default dedup as False
            ArgumentValueData.Add(Dedup, "False");

            // Add default value for Padding
            ArgumentValueData.Add(Padding, DefaultPadding.ToString(CultureInfo.InvariantCulture));

            // Clear InputImageList if it is not null
            if (inputImageList != null && inputImageList.Count > 0)
            {
                inputImageList.Clear();
            }

            // Arraylist that will hold the missing parameters. First all mandatory parameters
            // are added to it and as they are parsed removed from the list so the remaining
            // members in arraylist at the end of parsing are missing.
            if (MissingParams == null)
            {
                MissingParams = new ArrayList();
            }
            else
            {
                MissingParams.Clear();
            }

            MissingParams.Add(DirectoryOrFileName);
            MissingParams.Add(OutputDirectory);
            MissingParams.Add(XmlMapName);
            MissingParams.Add(ArgumentParser.Padding);
        }

        #endregion
    }
}
