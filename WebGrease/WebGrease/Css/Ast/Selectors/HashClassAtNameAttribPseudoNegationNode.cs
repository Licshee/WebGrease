// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HashClassAtNameAttribPseudoNegationNode.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The HashClassAtNameAttribPseudoNegationNode node
//   hashclassattribpseudonegation
//   : hash | class | atname | attrib | pseudo | negation
//   ;
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.Ast.Selectors
{
    using Visitor;

    /// <summary>The HashClassAtNameAttribPseudoNegationNode class</summary>
    public sealed class HashClassAtNameAttribPseudoNegationNode : AstNode
    {
        /// <summary>The exception message.</summary>
        private const string ExceptionMessage = "Only a single value out of hash or class or at name or attrib node or pseudo node or negation node can be not null.";

        /// <summary>Initializes a new instance of the HashClassAtNameAttribPseudoNegationNode class
        /// [ HASH | class | atname | attrib | pseudo ]</summary>
        /// <param name="hash">The hash.</param>
        /// <param name="cssClass">The css Class.</param>
        /// <param name="atName">The at name selector.</param>
        /// <param name="attribNode">The attrib Node.</param>
        /// <param name="pseudoNode">The pseudo Node.</param>
        /// <param name="negationNode">The negation Node.</param>
        public HashClassAtNameAttribPseudoNegationNode(string hash, string cssClass, string atName, AttribNode attribNode, PseudoNode pseudoNode, NegationNode negationNode)
        {
            if (!string.IsNullOrWhiteSpace(hash))
            {
                if (!string.IsNullOrWhiteSpace(cssClass) || !string.IsNullOrWhiteSpace(atName) || attribNode != null || pseudoNode != null || negationNode != null)
                {
                    throw new AstException(ExceptionMessage);
                }
            }

            if (!string.IsNullOrWhiteSpace(cssClass))
            {
                if (!string.IsNullOrWhiteSpace(atName) || attribNode != null || pseudoNode != null || negationNode != null)
                {
                    throw new AstException(ExceptionMessage);
                }
            }

            if (!string.IsNullOrWhiteSpace(atName))
            {
                if (attribNode != null || pseudoNode != null || negationNode != null)
                {
                    throw new AstException(ExceptionMessage);
                }
            }

            if (attribNode != null)
            {
                if (pseudoNode != null || negationNode != null)
                {
                    throw new AstException(ExceptionMessage);
                }
            }

            if (pseudoNode != null)
            {
                if (negationNode != null)
                {
                    throw new AstException(ExceptionMessage);
                }
            }

            this.Hash = hash;
            this.CssClass = cssClass;
            this.AtName = atName;
            this.AttribNode = attribNode;
            this.PseudoNode = pseudoNode;
            this.NegationNode = negationNode;
        }

        /// <summary>Gets the hash.</summary>
        public string Hash { get; private set; }

        /// <summary>Gets the Css Class.</summary>
        public string CssClass { get; private set; }

        /// <summary>Gets the at name selector.</summary>
        public string AtName { get; private set; }

        /// <summary>Gets the Attrib Node.</summary>
        public AttribNode AttribNode { get; private set; }

        /// <summary>Gets the Pseudo Node.</summary>
        public PseudoNode PseudoNode { get; private set; }

        /// <summary>Gets the Negation Node.</summary>
        public NegationNode NegationNode { get; private set; }

        /// <summary>Defines an accept operation</summary>
        /// <param name="nodeVisitor">The visitor to invoke</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        public override AstNode Accept(NodeVisitor nodeVisitor)
        {
            return nodeVisitor.VisitHashClassAtNameAttribPseudoNegationNode(this);
        }
    }
}
