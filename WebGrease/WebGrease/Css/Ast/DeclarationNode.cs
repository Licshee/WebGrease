// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeclarationNode.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   declaration
//   property ':' S* expr prio? | /* empty *
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.Ast
{
    using System.Diagnostics.Contracts;
    using Visitor;

    /// <summary>declaration
    /// property ':' S* expr prio? | /* empty */</summary>
    public sealed class DeclarationNode : AstNode
    {
        /// <summary>Initializes a new instance of the DeclarationNode class</summary>
        /// <param name="property">Delcaration Property</param>
        /// <param name="exprNode">Expression objecy</param>
        /// <param name="prio">Priority string</param>
        public DeclarationNode(string property, ExprNode exprNode, string prio)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(property));
            Contract.Requires(exprNode != null);

            // Member Initialization
            this.Property = property;
            this.ExprNode = exprNode;
            this.Prio = prio ?? string.Empty;
        }

        /// <summary>
        /// Gets the Property value
        /// </summary>
        /// <value>DeclarationNode Property value</value>
        public string Property { get; private set; }

        /// <summary>
        /// Gets the _expr value
        /// </summary>
        /// <value>Expression value</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Expr")]
        public ExprNode ExprNode { get; private set; }

        /// <summary>Gets the Prio.</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Prio")]
        public string Prio { get; private set; }

        /// <summary>Defines an accept operation</summary>
        /// <param name="nodeVisitor">The visitor to invoke</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        public override AstNode Accept(NodeVisitor nodeVisitor)
        {
            return nodeVisitor.VisitDeclarationNode(this);
        }
    }
}