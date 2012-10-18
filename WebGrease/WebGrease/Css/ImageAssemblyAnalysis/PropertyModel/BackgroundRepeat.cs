// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BackgroundRepeat.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The repeat enumeration
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.ImageAssemblyAnalysis.PropertyModel
{
    using System;
    using Ast;
    using Extensions;
    using LogModel;

    /// <summary>The repeat enumeration</summary>
    internal enum Repeat
    {
        /// <summary>
        /// The repeat mode
        /// </summary>
        Repeat, 

        /// <summary>
        /// The no repeat mode
        /// </summary>
        NoRepeat, 

        /// <summary>
        /// The repeat x mode
        /// </summary>
        RepeatX, 

        /// <summary>
        /// The repeat y mode
        /// </summary>
        RepeatY
    }

    /// <summary>Represents the CSS "background-repeat" declaration
    /// Example:
    /// #selector
    /// {
    ///    background-repeat: no-repeat;
    /// }</summary>
    internal sealed class BackgroundRepeat
    {
        /// <summary>
        /// Initializes a new instance of the BackgroundRepeat class
        /// </summary>
        internal BackgroundRepeat()
        {
        }

        /// <summary>Initializes a new instance of the BackgroundRepeat class</summary>
        /// <param name="declarationNode">The declaration node</param>
        internal BackgroundRepeat(DeclarationNode declarationNode)
        {
            if (declarationNode == null)
            {
                throw new ArgumentNullException("declarationNode");
            }

            var expr = declarationNode.ExprNode;
            this.ParseTerm(expr.TermNode);
            expr.TermsWithOperators.ForEach(this.ParseTermWithOperator);
        }

        /// <summary>
        /// Gets the background repeat value
        /// </summary>
        internal Repeat? RepeatValue { get; private set; }

        /// <summary>Verify that the no-repeat declaration is found</summary>
        /// <param name="parent">The parent AST node</param>
        /// <param name="imageAssemblyAnalysisLog">The logging object</param>
        /// <returns>True if no-repeat is used</returns>
        internal bool VerifyBackgroundNoRepeat(AstNode parent, ImageAssemblyAnalysisLog imageAssemblyAnalysisLog)
        {
            if (this.RepeatValue != Repeat.NoRepeat)
            {
                if (imageAssemblyAnalysisLog != null)
                {
                    // Log diagnostics
                    imageAssemblyAnalysisLog.Add(new ImageAssemblyAnalysis
                    {
                        AstNode = parent, 
                        FailureReason = FailureReason.NoRepeat
                    });
                }

                return false;
            }

            return true;
        }

        /// <summary>Parses the term AST node</summary>
        /// <param name="termNode">The AST node</param>
        internal void ParseTerm(TermNode termNode)
        {
            if (string.IsNullOrWhiteSpace(termNode.StringBasedValue))
            {
                return;
            }

            switch (termNode.StringBasedValue)
            {
                case ImageAssembleConstants.Repeat:
                    this.RepeatValue = Repeat.Repeat;
                    break;
                case ImageAssembleConstants.NoRepeat:
                    this.RepeatValue = Repeat.NoRepeat;
                    break;
                case ImageAssembleConstants.RepeatX:
                    this.RepeatValue = Repeat.RepeatX;
                    break;
                case ImageAssembleConstants.RepeatY:
                    this.RepeatValue = Repeat.RepeatY;
                    break;
                default:
                    break;
            }
        }

        /// <summary>Parses the termwithoperator AST node</summary>
        /// <param name="termWithOperatorNode">The AST to parse</param>
        internal void ParseTermWithOperator(TermWithOperatorNode termWithOperatorNode)
        {
            this.ParseTerm(termWithOperatorNode.TermNode);
        }
    }
}