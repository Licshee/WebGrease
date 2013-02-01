// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FloatOptimizationVisitor.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Provides the implementation of CSS float optimization visitor. It
//   visits all AST nodes for CSS files and optimizes following case:
//   1. 0.0 to 0
//   2. 1.0 to 1
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.Visitor
{
    using System;
    using System.Text.RegularExpressions;
    using Ast;
    using Extensions;

    /// <summary>Provides the implementation of CSS float optimization visitor. It
    /// visits all AST nodes for CSS files and optimizes following case.</summary>
    public sealed class FloatOptimizationVisitor : NodeTransformVisitor
    {
        /// <summary>
        /// The compiled regular expression for identifying numbers with units
        /// </summary>
        private static readonly Regex NumberBasedValue = new Regex(@"^(([0-9]+)([\.]?[0-9]*))(\s*[a-z%]*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>The <see cref="Ast.TermNode"/> visit implementation</summary>
        /// <param name="termNode">The term AST node</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        public override AstNode VisitTermNode(TermNode termNode)
        {
            if (termNode == null)
            {
                throw new ArgumentNullException("termNode");
            }

            var funcNode = termNode.FunctionNode;
            var numberBasedValue = termNode.NumberBasedValue;
            if (!string.IsNullOrWhiteSpace(numberBasedValue))
            {
                var match = NumberBasedValue.Match(numberBasedValue);
                if (match.Success)
                {
                    var fullNumber = match.Result("$1").ParseFloat(); // Say - 001.2400


// No units, just return the zero
                    if (fullNumber == 0)
                    {
                        return new TermNode(termNode.UnaryOperator, "0", termNode.StringBasedValue, termNode.Hexcolor, termNode.FunctionNode);
                    }

                    var leftNumber = match.Result("$2").TrimStart("0".ToCharArray()); // Say 001
                    var rightNumber = match.Result("$3").TrimEnd("0".ToCharArray()); // Say .2400
                    if (rightNumber == CssConstants.Dot.ToString())
                    {
                        rightNumber = string.Empty;
                    }

                    var units = match.Result("$4"); // Say %
                    return new TermNode(termNode.UnaryOperator, string.Concat(leftNumber, rightNumber, units), termNode.StringBasedValue, termNode.Hexcolor, termNode.FunctionNode);
                }
            }
            else if (funcNode != null)
            {
                // this visitor should never convert a function node to anything other than
                // a function node, so just force the conversion.
                funcNode = (FunctionNode)funcNode.Accept(this);
            }

            return new TermNode(termNode.UnaryOperator, numberBasedValue, termNode.StringBasedValue, termNode.Hexcolor, funcNode);
        }
    }
}