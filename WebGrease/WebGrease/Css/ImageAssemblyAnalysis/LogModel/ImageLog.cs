// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImageLog.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Represents the image log generated from image assembler tool
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.ImageAssemblyAnalysis.LogModel
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Xml.Linq;
    using Extensions;

    /// <summary>Represents the image log generated from image assembler tool</summary>
    internal class ImageLog
    {
        /// <summary>Initializes a new instance of the ImageLog class</summary>
        /// <param name="logPath">The log path which has the information for input and output files</param>
        internal ImageLog(string logPath)
        {
            if (string.IsNullOrWhiteSpace(logPath))
            {
                throw new ArgumentNullException("logPath");
            }

            if (!File.Exists(logPath))
            {
                throw new FileNotFoundException(string.Format(CultureInfo.CurrentUICulture, CssStrings.FileNotFoundError, logPath));
            }

            // List input file object
            this.InputImages = new List<AssembledImage>();

            var document = XDocument.Load(logPath);

            if (document.Root != null)
            {
                // For each output child element
                document.Root
                    .Elements(ImageAssembleConstants.OutputElementName)
                    .ForEach(outputElement =>
                                 {
                                     var fileAttribute = outputElement.Attribute(ImageAssembleConstants.FileAttributeName);

                                     // This is a case of images ignored by image assembler
                                     if (fileAttribute == null)
                                     {
                                         return;
                                     }

                                     // Output image file path
                                     var outputFilePath = fileAttribute.Value;

                                     if (string.IsNullOrWhiteSpace(outputFilePath))
                                     {
                                         // This is a case of images ignored by image assembler
                                         return;
                                     }

                                     // TODO - Spec. issue: Shall we support http:// paths?
                                     outputFilePath = outputFilePath.GetFullPathWithLowercase();

                                     // Validate the output file
                                     if (!File.Exists(outputFilePath))
                                     {
                                         throw new FileNotFoundException(string.Format(CultureInfo.CurrentUICulture, CssStrings.FileNotFoundError, outputFilePath));
                                     }

                                     outputElement.Descendants(ImageAssembleConstants.InputElementName).ForEach(inputElement =>
                                                                                                                    {
                                                                                                                        // Load the input image object
                                                                                                                        var inputImage = new AssembledImage(inputElement);

                                                                                                                        // Validate the original file path
                                                                                                                        var originalFilePath = inputImage.OriginalFilePath;

                                                                                                                        // Original file path cannot be empty
                                                                                                                        if (string.IsNullOrWhiteSpace(originalFilePath))
                                                                                                                        {
                                                                                                                            throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, CssStrings.OriginalFileElementEmptyError, logPath));
                                                                                                                        }

                                                                                                                        // Note - Spec. issue: Shall we support http:// paths?
                                                                                                                        originalFilePath = originalFilePath.GetFullPathWithLowercase();

                                                                                                                        // Validate the original file
                                                                                                                        if (!File.Exists(originalFilePath))
                                                                                                                        {
                                                                                                                            throw new FileNotFoundException(string.Format(CultureInfo.CurrentUICulture, CssStrings.FileNotFoundError, originalFilePath));
                                                                                                                        }

                                                                                                                        // Assign the full paths in input object
                                                                                                                        inputImage.OriginalFilePath = originalFilePath;
                                                                                                                        inputImage.OutputFilePath = outputFilePath;

                                                                                                                        // Add the input object to dictionary
                                                                                                                        this.InputImages.Add(inputImage);
                                                                                                                    });
                                 });
            }
        }

        /// <summary>
        /// Gets the list of output images
        /// </summary>
        internal List<AssembledImage> InputImages { get; private set; }
    }
}