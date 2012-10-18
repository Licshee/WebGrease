// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImageAssemblyUpdateVisitor.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Provides the implementation for ImageAssembly update visitor
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.Visitor
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Ast;
    using Ast.MediaQuery;
    using Extensions;
    using ImageAssemble;
    using ImageAssemblyAnalysis.LogModel;
    using ImageAssemblyAnalysis.PropertyModel;
    using ImageAssembleException = ImageAssemblyAnalysis.ImageAssembleException;

    /// <summary>Provides the implementation for ImageAssembly update visitor</summary>
    public class ImageAssemblyUpdateVisitor : NodeVisitor
    {
        /// <summary>
        /// The css path
        /// </summary>
        private readonly string _cssPath;

        /// <summary>
        /// The input images computed from log file
        /// </summary>
        private readonly List<AssembledImage> _inputImages;

        /// <summary>Initializes a new instance of the ImageAssemblyUpdateVisitor class</summary>
        /// <param name="cssPath">The css file path which would be used to configure the image path</param>
        /// <param name="logFiles">The log path which contains the image map after spriting</param>
        public ImageAssemblyUpdateVisitor(string cssPath, IEnumerable<string> logFiles)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(cssPath));
            Contract.Requires(logFiles != null);

            // Normalize css path
            _cssPath = cssPath.GetFullPathWithLowercase();

            try
            {
                // Validate the log path and load the input dictionary after validation
                // Key = input/original image full path in lower case
                // Value = object which has properties such as, assembled image path, x, y coordinates etc.
                _inputImages = new List<AssembledImage>();
                foreach (var logFile in logFiles)
                {
                    if (File.Exists(logFile))
                    {
                        // Lazy load file is optional
                        _inputImages.AddRange(new ImageLog(logFile).InputImages);
                    }
                }
            }
            catch (Exception exception)
            {
                throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, CssStrings.InnerExceptionFile, string.Join(CssConstants.Comma.ToString(), logFiles)), exception);
            }
        }

        /// <summary>The <see cref="StyleSheetNode"/> visit implementation</summary>
        /// <param name="styleSheet">The styleSheet AST node</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        public override AstNode VisitStyleSheetNode(StyleSheetNode styleSheet)
        {
            var updatedStyleSheetRuleNodes = new List<StyleSheetRuleNode>();
            styleSheet.StyleSheetRules.ForEach(styleSheetRuleNode => updatedStyleSheetRuleNodes.Add((StyleSheetRuleNode)styleSheetRuleNode.Accept(this)));

            return new StyleSheetNode(styleSheet.CharSetString, styleSheet.Imports, styleSheet.Namespaces, updatedStyleSheetRuleNodes.AsReadOnly());
        }

        /// <summary>The <see cref="RulesetNode"/> visit implementation</summary>
        /// <param name="rulesetNode">The ruleset AST node</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        public override AstNode VisitRulesetNode(RulesetNode rulesetNode)
        {
            return new RulesetNode(rulesetNode.SelectorsGroupNode, this.UpdateDeclarations(rulesetNode.Declarations, rulesetNode));
        }

        /// <summary>The <see cref="MediaNode"/> visit implementation</summary>
        /// <param name="mediaNode">The media AST node</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        public override AstNode VisitMediaNode(MediaNode mediaNode)
        {
            var updatedRulesets = new List<RulesetNode>();
            var updatedPageNodes = new List<PageNode>();
            mediaNode.Rulesets.ForEach(rulesetNode => updatedRulesets.Add((RulesetNode)rulesetNode.Accept(this)));
            mediaNode.PageNodes.ForEach(pageNode => updatedPageNodes.Add((PageNode)pageNode.Accept(this)));

            return new MediaNode(mediaNode.MediaQueries, updatedRulesets.AsReadOnly(), updatedPageNodes.AsReadOnly());
        }

        /// <summary>The <see cref="PageNode"/> visit implementation</summary>
        /// <param name="pageNode">The page AST node</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        public override AstNode VisitPageNode(PageNode pageNode)
        {
            return new PageNode(pageNode.PseudoPage, this.UpdateDeclarations(pageNode.Declarations, pageNode));
        }

        /// <summary>Update the declarations </summary>
        /// <param name="declarationNodes">The list of declaration nodes</param>
        /// <param name="originalDeclarationNode">The original declaration node</param>
        /// <param name="updatedDeclarationNode">The updated declaration node</param>
        private static void UpdateDeclarations(IList<DeclarationNode> declarationNodes, DeclarationNode originalDeclarationNode, DeclarationNode updatedDeclarationNode)
        {
            declarationNodes[declarationNodes.IndexOf(originalDeclarationNode)] = updatedDeclarationNode;
        }

        /// <summary>Updates the list of declarations with the updated value from
        /// image assembly log.
        /// Example Css with shorthand declaration:
        /// #selector
        /// {
        ///   background: url(../../i/02/3118D8F3781159C8341246BBF2B4CA.gif) no-repeat -10px -200px;
        /// }
        /// Example Css with long declarations:
        /// #selector
        /// {
        ///   background-repeat: no-repeat;
        ///   background-position: -10px  -200px;
        ///   background-image: url(../../i/02/3118D8F3781159C8341246BBF2B4CA.gif);
        /// }</summary>
        /// <param name="declarationNodes">The list of declarations</param>
        /// <param name="parent">The parent AST node</param>
        /// <returns>The list of updated declarations</returns>
        private ReadOnlyCollection<DeclarationNode> UpdateDeclarations(ReadOnlyCollection<DeclarationNode> declarationNodes, AstNode parent)
        {
            try
            {
                // Populate the nodes from AST extension
                Background backgroundNode;
                BackgroundImage backgroundImageNode;
                BackgroundPosition backgroundPositionNode;

                // There is no background node found in set of declarations, return without any change
                if (!declarationNodes.TryGetBackgroundDeclaration(null, parent, out backgroundNode, out backgroundImageNode, out backgroundPositionNode, null, null, null))
                {
                    // No change, return the original collection
                    return declarationNodes;
                }

                // At this point, there should be atleast one "background" or "background-image" node found.
                // In addition, there can be an optional "background-position" node
                // Initialize a cloned set of declarations (The original AST collection is immutable by design)
                var updatedDeclarations = new List<DeclarationNode>(declarationNodes);


// Empty object to be discovered from log
                AssembledImage assembledImage;

                if (backgroundNode != null)
                {
                    // Short hand declaration found:
                    // #selector
                    // {
                    // background: url(../../i/02/3118D8F3781159C8341246BBF2B4CA.gif) no-repeat -10px -200px;
                    // }
                    // Query the log file and see if there is an assembled node for 
                    // the background image
                    if (!this.TryGetAssembledImage(backgroundNode.Url, backgroundNode.BackgroundPosition, out assembledImage))
                    {
                        return declarationNodes;
                    }

                    // Update the declaration node with new values in AST
                    var updatedDeclaration = backgroundNode.UpdateBackgroundNode(assembledImage.RelativeOutputFilePath, assembledImage.X, assembledImage.Y);

                    // Update the declaration list
                    UpdateDeclarations(updatedDeclarations, backgroundNode.DeclarationAstNode, updatedDeclaration);
                }
                else if (backgroundImageNode != null)
                {
                    // Long declaration found for background-image:
                    // #selector
                    // {
                    // background-image: url(../../i/02/3118D8F3781159C8341246BBF2B4CA.gif);
                    // }
                    // Query the log file and see if there is an assembled node for 
                    // the background image
                    if (!this.TryGetAssembledImage(backgroundImageNode.Url, backgroundPositionNode, out assembledImage))
                    {
                        // Return without update with a group of declarations
                        return declarationNodes;
                    }

                    // Update the declaration node with new values in AST
                    var updatedDeclaration = backgroundImageNode.UpdateBackgroundImageNode(assembledImage.RelativeOutputFilePath);

                    // Update the list of declarations
                    UpdateDeclarations(updatedDeclarations, backgroundImageNode.DeclarationNode, updatedDeclaration);

                    if (backgroundPositionNode != null)
                    {
                        // Long declaration found for background-position:
                        // #selector
                        // {
                        // background-position: -10px  -200px;
                        // }
                        // Update the declaration node with new values in AST
                        updatedDeclaration = backgroundPositionNode.UpdateBackgroundPositionNode(assembledImage.X, assembledImage.Y);

                        // Update the list of declarations
                        UpdateDeclarations(updatedDeclarations, backgroundPositionNode.DeclarationNode, updatedDeclaration);
                    }
                    else
                    {
                        // If there is no declaration found for "background-position",
                        // Create a new declaration node in AST for "background-position" declaration
                        var newDeclaration = BackgroundPosition.CreateNewDeclaration(assembledImage.X, assembledImage.Y);

                        if (newDeclaration != null)
                        {
                            updatedDeclarations.Add(newDeclaration);
                        }
                    }
                }

                return updatedDeclarations.AsReadOnly();
            }
            catch (Exception exception)
            {
                throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, CssStrings.InnerExceptionSelector, parent.PrettyPrint()), exception);
            }
        }

        /// <summary>Gets the assembled image information from the dictionary</summary>
        /// <param name="parsedImagePath">The parsed url</param>
        /// <param name="backgroundPosition">The background position node</param>
        /// <param name="assembledImage">The assembled image path</param>
        /// <returns>The assembled image object</returns>
        private bool TryGetAssembledImage(string parsedImagePath, BackgroundPosition backgroundPosition, out AssembledImage assembledImage)
        {
            assembledImage = null;

            if (_inputImages == null)
            {
                return false;
            }

            // TODO - Spec. issue: What are the supported path formats supported in CSS?
            // For optimization, should the relative paths be enforced?

            // Get the full path of parsed image file (convert from ../../ to absolute path)
            parsedImagePath = parsedImagePath.MakeAbsoluteTo(_cssPath);

            var imagePosition = ImagePosition.Left;
            if (backgroundPosition != null)
            {
                imagePosition = backgroundPosition.GetImagePositionInVerticalSprite();
            }

            // Try to locate the input image in the list
            assembledImage = _inputImages.Where(inputImage => inputImage.ImagePosition == imagePosition && inputImage.OriginalFilePath == parsedImagePath).FirstOrDefault();

            if (assembledImage != null &&
                assembledImage.OutputFilePath != null)
            {
                assembledImage.RelativeOutputFilePath = assembledImage.OutputFilePath.MakeRelativeTo(_cssPath);
                return true;
            }

            return false;
        }
    }
}