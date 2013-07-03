// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StyleSheetRuleOrCommentNode.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The stylesheetruleorcommentnode.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.Ast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// StyleSheetRuleOrCommentNode
    /// </summary>
    public class StyleSheetRuleOrCommentNode : StyleSheetRuleNode
    {
        /// <summary>
        /// The StylesheetRuleOrCommentNode
        /// </summary>
        /// <param name="comment"> Important comment node</param>
        /// <param name="isComment"> Whether it is comment or not.</param>
        public StyleSheetRuleOrCommentNode(ImportantCommentNode comment, bool isComment)
        {
            this.ImportantCommentNode = comment;
            IsCommentNode = isComment;
        }

        /// <summary>
        /// Get the comment node if it is comment
        /// </summary>
        public ImportantCommentNode ImportantCommentNode { get; private set; }

        /// <summary>
        /// Whether the class is comment or style sheet
        /// </summary>
        public bool IsCommentNode { get; set; }

        public override AstNode Accept(Visitor.NodeVisitor nodeVisitor)
        {
            if (IsCommentNode)
            {
                return new StyleSheetRuleOrCommentNode((ImportantCommentNode)this.ImportantCommentNode.Accept(nodeVisitor), true);
            }
            return base.Accept(nodeVisitor);
        }
    }
}
