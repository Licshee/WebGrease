// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BackgroundPosition.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The units of background position
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.ImageAssemblyAnalysis.PropertyModel
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Ast;
    using Extensions;
    using ImageAssemble;
    using LogModel;
    using ImageAssembleException = ImageAssemblyAnalysis.ImageAssembleException;

    /// <summary>The units of background position</summary>
    internal enum Source
    {
        /// <summary>
        /// The left source
        /// </summary>
        Left, 

        /// <summary>
        /// The right source
        /// </summary>
        Right, 

        /// <summary>
        /// The center source
        /// </summary>
        Center, 

        /// <summary>
        /// The top source
        /// </summary>
        Top, 

        /// <summary>
        /// The bottom source
        /// </summary>
        Bottom, 

        /// <summary>
        /// The pixel source
        /// </summary>
        Px, 

        /// <summary>
        /// The percentage source
        /// </summary>
        Percentage, 

        /// <summary>
        /// The no units source
        /// </summary>
        NoUnits, 

        /// <summary>
        /// This is for forward compatibility post Css 2.1
        /// </summary>
        Unknown
    }

    /// <summary>Represents the Css "background-position" node
    /// Example:
    /// #selector
    /// {
    ///    background-position: 2px 3px;
    /// }</summary>
    internal sealed class BackgroundPosition
    {
        /// <summary>
        /// Initializes a new instance of the BackgroundPosition class
        /// </summary>
        internal BackgroundPosition()
        {
        }

        /// <summary>Initializes a new instance of the BackgroundPosition class</summary>
        /// <param name="declarationNode">The declaration node</param>
        internal BackgroundPosition(DeclarationNode declarationNode)
        {
            if (declarationNode == null)
            {
                throw new ArgumentNullException("declarationNode");
            }

            this.DeclarationNode = declarationNode;

            var expr = declarationNode.ExprNode;
            this.ParseTerm(expr.TermNode);
            expr.TermsWithOperators.ForEach(this.ParseTermWithOperator);
        }

        /// <summary>
        /// Gets the declaration node
        /// </summary>
        public DeclarationNode DeclarationNode { get; private set; }

        /// <summary>
        /// Gets the horizontal length
        /// </summary>
        internal float? X { get; private set; }

        /// <summary>
        /// Gets the vertical length
        /// </summary>
        internal float? Y { get; private set; }

        /// <summary>
        /// Gets the horizontal coordinate source
        /// </summary>
        internal Source? XSource { get; private set; }

        /// <summary>
        /// Gets the vertical coordinate source
        /// </summary>
        internal Source? YSource { get; private set; }

        /// <summary>
        /// Gets the horizontal term node
        /// </summary>
        internal TermNode XTermNode { get; private set; }

        /// <summary>
        /// Gets the vertical term node
        /// </summary>
        internal TermNode YTermNode { get; private set; }

        /// <summary>Adds the missing x, y to specify the background position</summary>
        /// <param name="updatedX">The updated X</param>
        /// <param name="updatedY">The updated Y</param>
        /// <param name="isXUpdated">The flag which checks if X is already updated</param>
        /// <param name="isYUpdated">The flag which checks if Y is already updated</param>
        /// <param name="indexX">The index at which X should be inserted</param>
        /// <param name="indexY">The index at which Y should be inserted</param>
        /// <param name="newTermsWithOperators">The updated terms</param>
        internal static void AddingMissingXAndY(float? updatedX, float? updatedY, bool isXUpdated, bool isYUpdated, int indexX, int indexY, List<TermWithOperatorNode> newTermsWithOperators)
        {
             // Per Css 2.1 - If only one value is specified, the second value is assumed to be 'center'
            string operatorX = null;
            string operatorY = null;

            var finalX = ImageAssembleConstants.Center;
            var finalY = ImageAssembleConstants.Center;

            // Per Css 2.1 - If both x and y are missing then reconcile the coordinates with 0 0
            if (!isXUpdated && !isYUpdated)
            {
                operatorX = updatedX.UnaryOperator();
                operatorY = updatedY.UnaryOperator();
                finalX = updatedX.Pixels();
                finalY = updatedY.Pixels();
            }

            if (!isXUpdated)
            {
                // This means that the coordinates were missing in the original declaration, add a new set
                newTermsWithOperators.Insert(indexX, new TermWithOperatorNode(ImageAssembleConstants.SingleSpace, new TermNode(operatorX, finalX, null, null, null)));
                indexY = indexX + 1;
            }

            if (!isYUpdated)
            {
                // Appends y just after the index where x coordinate was inserted.
                newTermsWithOperators.Insert(indexY, new TermWithOperatorNode(ImageAssembleConstants.SingleSpace, new TermNode(operatorY, finalY, null, null, null)));
            }
        }

        /// <summary>Adds a node with new updatedX, updatedY to specify the background position</summary>
        /// <param name="updatedX">The updated X</param>
        /// <param name="updatedY">The updated Y</param>
        /// <returns>The declaration node</returns>
        internal static DeclarationNode CreateNewDeclaration(float? updatedX, float? updatedY)
        {
            if (updatedX == null || (updatedX == 0 && updatedY == 0))
            {
                return null;
            }

            // Create a new term for coordinate x
            var termNodeX = new TermNode(updatedX.UnaryOperator(), updatedX.Pixels(), null, null, null);

            var termWithOperatorNodes = new List<TermWithOperatorNode>();

            // Create a new term with operator for coordinate y
            if (updatedY != null && updatedY != 0)
            {
                var termNodeY = new TermNode(updatedY.UnaryOperator(), updatedY.Pixels(), null, null, null);
                termWithOperatorNodes.Add(new TermWithOperatorNode(ImageAssembleConstants.SingleSpace, termNodeY));
            }

            // Create a new expression
            var expressionNode = new ExprNode(termNodeX, termWithOperatorNodes.AsReadOnly());

            // Create a new declaration node
            return new DeclarationNode(ImageAssembleConstants.BackgroundPosition, expressionNode, null);
        }

        /// <summary>Verify that both the horizontal and vertical units are specified in px units</summary>
        /// <param name="parent">The parent AST node</param>
        /// <param name="imageAssemblyAnalysisLog">The logging object</param>
        /// <returns>True if px units are used</returns>
        internal bool IsVerticalSpriteCandidate(AstNode parent, ImageAssemblyAnalysisLog imageAssemblyAnalysisLog)
        {
            // The following scenarios are candidate for image to be part of vertical sprite:
            // ==============================================================================
            // X and Y not present (which defaults to 0% 0% per Css 2.1 spec)
            // X has any value and Y is zero irrespective of any unit system (top, px, em, ex, %)
            // X has any value and Y is Top
            // X has any value and Y is Px
            if ((this.X == null && this.XSource == null && this.Y == null && this.YSource == null) ||
                (this.Y != null && this.Y.Value == 0) ||
                (this.YSource == Source.Px))
            {
                return true;
            }

            // Log diagnostics
            if (imageAssemblyAnalysisLog != null)
            {
                imageAssemblyAnalysisLog.Add(new ImageAssemblyAnalysis
                {
                    AstNode = parent, 
                    FailureReason = FailureReason.IncorrectPosition
                });
            }

            return false;
        }

        /// <summary>Determines if the image is right aligned.</summary>
        /// <returns>True if the image is right aligned.</returns>
        internal bool IsHorizontalRightAligned()
        {
            return (this.XSource == Source.Right && this.Y != null && this.Y.Value == 0) ||
                   (this.XSource == Source.Right && this.YSource == Source.Px) ||
                   (this.XSource == Source.Percentage && this.X != null && this.X.Value == 100 && this.Y != null && this.Y.Value == 0) ||
                   (this.XSource == Source.Percentage && this.X != null && this.X.Value == 100 && this.YSource == Source.Px);
        }

        /// <summary>Determines if the image is center aligned.</summary>
        /// <returns>True if the image is center aligned.</returns>
        internal bool IsHorizontalCenterAligned()
        {
            return (this.XSource == null && this.YSource == Source.Top) ||
                   (this.XSource == Source.Center && this.Y != null && this.Y.Value == 0) ||
                   (this.XSource == Source.Center && this.YSource == Source.Px) ||
                   (this.XSource == Source.Percentage && this.X != null && this.X.Value == 50 && this.Y != null && this.Y.Value == 0) ||
                   (this.XSource == Source.Percentage && this.X != null && this.X.Value == 50 && this.YSource == Source.Px);
        }

        /// <summary>Gets the image position based on the coordinate system</summary>
        /// <returns>The image position</returns>
        internal ImagePosition GetImagePositionInVerticalSprite()
        {
            if (this.IsHorizontalCenterAligned())
            {
                return ImagePosition.Center;
            }

            if (this.IsHorizontalRightAligned())
            {
                return ImagePosition.Right;
            }

            return ImagePosition.Left;
        }

        /// <summary>Parses the term AST node</summary>
        /// <param name="termNode">The AST node</param>
        internal void ParseTerm(TermNode termNode)
        {
            float length;

            if (!string.IsNullOrWhiteSpace(termNode.StringBasedValue))
            {
                const int SignInteger = 1;
                switch (termNode.StringBasedValue)
                {
                    case ImageAssembleConstants.Left:

// We are assigning x followed by y
                        // For term with node when 'center, left' is found it means 'center' was meant for y, SWAP!!
                        this.TrySwapXCoordinate();
                        this.AssignX(termNode, 0, SignInteger, Source.Left);
                        break;
                    case ImageAssembleConstants.Right:

// We are assigning x followed by y
                        // For term with node when 'center, right' is found it means 'center' was meant for y, SWAP!!
                        this.TrySwapXCoordinate();
                        this.AssignX(termNode, null, SignInteger, Source.Right);
                        break;
                    case ImageAssembleConstants.Center:
                        this.AssignXy(termNode, null, SignInteger, Source.Center);
                        break;
                    case ImageAssembleConstants.Top:
                        this.AssignY(termNode, 0, SignInteger, Source.Top);
                        break;
                    case ImageAssembleConstants.Bottom:
                        this.AssignY(termNode, null, SignInteger, Source.Bottom);
                        break;
                    default:
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(termNode.NumberBasedValue))
            {
                return;
            }

            // Parse the number based terms:
            // The 0 values (except 0%) would be available without suffix on the term nodes.
            // We are interested non zero values only if those are px or percentage.
            var termValue = termNode.NumberBasedValue;

            if (termValue.EndsWith(ImageAssembleConstants.Px, StringComparison.OrdinalIgnoreCase) &&
                termValue.Length > 2 &&
                float.TryParse(termValue.Substring(0, termValue.Length - 2), out length))
            {
                this.AssignXy(termNode, length, termNode.UnaryOperator.SignInt(), Source.Px);
            }
            else if (termValue.EndsWith(ImageAssembleConstants.Percentage, StringComparison.OrdinalIgnoreCase) &&
                     termValue.Length > 1 &&
                     float.TryParse(termValue.Substring(0, termValue.Length - 1), out length))
            {
                this.AssignXy(termNode, length, termNode.UnaryOperator.SignInt(), Source.Percentage);
            }
            else if (termValue.TryParseZeroBasedNumberValue())
            {
                // This is a case of zero with units specified (0 em/ex/px/cm/mm/in/pt/pc)
                this.AssignXy(termNode, 0, termNode.UnaryOperator.SignInt(), Source.NoUnits);
            }
            else if (float.TryParse(termValue, out length))
            {
                // This is a case of units not specified (0 em/ex/px/cm/mm/in/pt/pc)
                this.AssignXy(termNode, length, termNode.UnaryOperator.SignInt(), Source.NoUnits);
            }
            else
            {
                // Assign the length to null (This information is not relevant to image assembly)
                this.AssignXy(termNode, null, 1, Source.Unknown);
            }
        }

        /// <summary>Parses the termwithoperator AST node</summary>
        /// <param name="termWithOperatorNode">The AST to parse</param>
        internal void ParseTermWithOperator(TermWithOperatorNode termWithOperatorNode)
        {
            this.ParseTerm(termWithOperatorNode.TermNode);
        }

        /// <summary>Updates the term for horizontal position</summary>
        /// <param name="termNode">The term node</param>
        /// <param name="updatedTermNode">The new term node</param>
        /// <param name="updatedX">The new X position</param>
        /// <returns>Returns true if term is updated</returns>
        internal bool UpdateTermForX(TermNode termNode, out TermNode updatedTermNode, float? updatedX)
        {
            if (termNode == this.XTermNode)
            {
                // Lets not assume any spriting direction here.
                // Update X when it is 0 or in pixels
                if (this.X == 0 || this.XSource == Source.Px)
                {
                    var finalX = this.X + updatedX;

                    // Create a term with new x
                    updatedTermNode = new TermNode(finalX.UnaryOperator(), finalX.Pixels(), null, null, null);
                }
                else
                {
                    // No change in x and return the original term
                    updatedTermNode = termNode;
                }
                
                return true;
            }

            updatedTermNode = termNode;
            return false;
        }

        /// <summary>Updates the term for vertical position</summary>
        /// <param name="termNode">The term node</param>
        /// <param name="updatedTermNode">The new term node</param>
        /// <param name="updatedY">The new Y position</param>
        /// <returns>Returns true if term is updated</returns>
        internal bool UpdateTermForY(TermNode termNode, out TermNode updatedTermNode, float? updatedY)
        {
            if (termNode == this.YTermNode)
            {
                // Lets not assume any spriting direction here.
                // Update Y when it is in zero or pixels
                if (this.Y == 0 || this.YSource == Source.Px)
                {
                    var finalY = this.Y + updatedY;

                    // Create a term with new y
                    updatedTermNode = new TermNode(finalY.UnaryOperator(), finalY.Pixels(), null, null, null);
                }
                else
                {
                    // No change in y and return the original term
                    updatedTermNode = termNode;
                }
                
                return true;
            }

            updatedTermNode = termNode;
            return false;
        }

        /// <summary>Updates the background node with new x, y</summary>
        /// <example>The coordinates need to be expanded for long declaration:
        /// #selector
        /// {
        ///     background-position: top left;
        /// }</example>
        /// <param name="updatedX">The updated x</param>
        /// <param name="updatedY">The updated y</param>
        /// <returns>The new declaration node with updated values</returns>
        internal DeclarationNode UpdateBackgroundPositionNode(float? updatedX, float? updatedY)
        {
            if (this.DeclarationNode == null)
            {
                return null;
            }

            var isXUpdated = false;
            var isYUpdated = false;
            var indexX = 0;
            var indexY = 0;

            var updatedTermsWithOperators = new List<TermWithOperatorNode>();
            foreach (var termWithOperatorNode in this.DeclarationNode.DeclarationEnumerator())
            {
                TermNode updatedTermNode;

                // Try updating X
                if (!isXUpdated)
                {
                    isXUpdated = this.UpdateTermForX(termWithOperatorNode.TermNode, out updatedTermNode, updatedX);

                    if (isXUpdated)
                    {
                        if (isYUpdated)
                        {
                            // Insert just before Y (consider a scenario of top center to center 0)
                            updatedTermsWithOperators.Insert(indexX, new TermWithOperatorNode(termWithOperatorNode.Operator, updatedTermNode.CopyTerm()));
                        }
                        else
                        {
                            // Insert at the end
                            updatedTermsWithOperators.Add(new TermWithOperatorNode(termWithOperatorNode.Operator, updatedTermNode.CopyTerm()));
                            indexY = updatedTermsWithOperators.Count;
                        }

                        continue;
                    }
                }

                // Try updating Y
                if (!isYUpdated)
                {
                    isYUpdated = this.UpdateTermForY(termWithOperatorNode.TermNode, out updatedTermNode, updatedY);

                    if (isYUpdated)
                    {
                        if (isXUpdated)
                        {
                            // Insert just after X (consider a scenario of top center to center 0)
                            updatedTermsWithOperators.Insert(indexY, new TermWithOperatorNode(termWithOperatorNode.Operator, updatedTermNode.CopyTerm()));
                        }
                        else
                        {
                            // Insert at the end
                            updatedTermsWithOperators.Add(new TermWithOperatorNode(termWithOperatorNode.Operator, updatedTermNode.CopyTerm()));
                            indexX = updatedTermsWithOperators.Count - 1;
                        }

                        continue;
                    }
                }

                // Save the original term with operator (such as no-repeat)
                updatedTermsWithOperators.Add(termWithOperatorNode);
            }

            // Add any missing X or Y
            AddingMissingXAndY(updatedX, updatedY, isXUpdated, isYUpdated, indexX, indexY, updatedTermsWithOperators);

            return this.DeclarationNode.CreateDeclarationNode(updatedTermsWithOperators);
        }

        /// <summary>Swap the X values to Y in case of commutative relationship
        /// <example>
        /// The Css declaration can be expressed in a commutative manner.
        /// #foo
        /// {
        ///     background: url(foo.gif) center left; /* left is X and center is Y */
        ///     background: url(foo.gif) left center; /* left is X and center is Y */
        ///     background: url(foo.gif) center right; /* right is X and center is Y */
        ///     background: url(foo.gif) right center; /* right is X and center is Y */
        /// }
        /// Since we parse the values from left to right, the X may already been populated
        /// by "Center" which need to be promoted to Y.</example>
        /// </summary>
        private void TrySwapXCoordinate()
        {
            if (this.XSource != Source.Center)
            {
                return;
            }

            // Populate the X values in Y coordinate
            this.AssignY(this.XTermNode, this.X, 1, this.XSource.Value);
            this.XTermNode = null;
            this.X = null;
            this.XSource = null;
        }

        /// <summary>Assigns the length horizontal value</summary>
        /// <param name="termNode">The term node</param>
        /// <param name="offset">The length to assign</param>
        /// <param name="sign">The sign of term</param>
        /// <param name="source">The unit of length</param>
        private void AssignX(TermNode termNode, float? offset, int? sign, Source source)
        {
            if (this.XSource != null)
            {
                throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, CssStrings.TooManyLengthsError, termNode.PrettyPrint()));
            }

            this.XTermNode = termNode;
            if (offset != null && sign != null)
            {
                this.X = offset * sign;
            }

            this.XSource = source;
        }

        /// <summary>Assigns the length horizontal value</summary>
        /// <param name="termNode">The term node</param>
        /// <param name="offset">The length to assign</param>
        /// <param name="sign">The sign of term</param>
        /// <param name="source">The unit of length</param>
        private void AssignY(TermNode termNode, float? offset, int? sign, Source source)
        {
            if (this.YSource != null)
            {
                throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, CssStrings.TooManyLengthsError, termNode.PrettyPrint()));
            }

            this.YTermNode = termNode;
            if (offset != null && sign != null)
            {
                this.Y = offset * sign;
            }

            this.YSource = source;
        }

        /// <summary>Assigns the length to horizontal or vertical value</summary>
        /// <param name="termNode">The term node</param>
        /// <param name="offset">The length to assign</param>
        /// <param name="sign">The sign of term</param>
        /// <param name="source">The unit of length</param>
        private void AssignXy(TermNode termNode, float? offset, int? sign, Source source)
        {
            if (this.XSource == null)
            {
                this.AssignX(termNode, offset, sign, source);
            }
            else if (this.YSource == null)
            {
                this.AssignY(termNode, offset, sign, source);
            }
            else
            {
                throw new ImageAssembleException(string.Format(CultureInfo.CurrentUICulture, CssStrings.TooManyLengthsError, termNode.PrettyPrint()));
            }
        }
    }
}