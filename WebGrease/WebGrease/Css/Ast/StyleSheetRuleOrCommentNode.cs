using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebGrease.Css.Ast
{
    class StyleSheetRuleOrCommentNode : StyleSheetRuleNode
    {
        public StyleSheetRuleOrCommentNode(ImportantCommentNode comment, bool isComment)
        {
            this.CommentNode = comment;
            IsCommentNode = isComment;
        }

        /// <summary>
        /// Get the comment node if it is comment
        /// </summary>
        public ImportantCommentNode CommentNode { get; private set; }

        /// <summary>
        /// Whether the class is comment or style sheet
        /// </summary>
        public bool IsCommentNode { get; set; }

        public override AstNode Accept(Visitor.NodeVisitor nodeVisitor)
        {
            if (IsCommentNode)
            {
                return new StyleSheetRuleOrCommentNode((ImportantCommentNode)this.CommentNode.Accept(nodeVisitor), true);
            }
            return base.Accept(nodeVisitor);
        }
    }
}
