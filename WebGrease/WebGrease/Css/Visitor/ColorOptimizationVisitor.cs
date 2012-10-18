// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColorOptimizationVisitor.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Provides the implementation of CSS color optimization visitor. It
//   visits all AST nodes for CSS files and optimizes for colors.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.Visitor
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using Ast;

    /// <summary>Provides the implementation of CSS float optimization visitor. It
    /// visits all AST nodes for CSS files and optimizes for colors.</summary>
    public sealed class ColorOptimizationVisitor : NodeTransformVisitor
    {
        /// <summary>
        /// Matches 6-digit RGB color value where both r digits are the same, both
        /// g digits are the same, and both b digits are the same (but r, g, and b
        /// values are not necessarily the same). Used to identify #rrggbb values
        /// that can be collapsed to #rgb
        /// </summary>
        private static readonly Regex ColorGroupCapture = new Regex(@"^\#(?<r>[0-9a-f])\k<r>(?<g>[0-9a-f])\k<g>(?<b>[0-9a-f])\k<b>$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>The <see cref="Ast.TermNode"/> visit implementation</summary>
        /// <param name="termNode">The term AST node</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "This is by design.")]
        public override AstNode VisitTermNode(TermNode termNode)
        {
            if (termNode == null)
            {
                throw new ArgumentNullException("termNode");
            }

            var hexColor = termNode.Hexcolor;
            if (!string.IsNullOrWhiteSpace(hexColor))
            {
                var match = ColorGroupCapture.Match(hexColor);
                if (match.Success)
                {
                    hexColor = string.Format(CultureInfo.InvariantCulture, "#{0}{1}{2}", match.Result("${r}"), match.Result("${g}"), match.Result("${b}"));
                }

                hexColor = hexColor.ToLowerInvariant();
            }

            return new TermNode(termNode.UnaryOperator, termNode.NumberBasedValue, termNode.StringBasedValue, hexColor, termNode.FunctionNode);
        }
    }
}