// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RulesetNode.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   ruleset
//   : selectors_group
//   '{' S* declaration? [ ';' S* declaration? ]* '}' S*
//   ;
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.Ast
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;    
    using System.Collections.Specialized;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using Selectors;
    using Visitor;

    /// <summary>ruleset
    /// : selectors_group
    /// '{' S* declaration? [ ';' S* declaration? ]* '}' S*
    /// ;</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ruleset")]
    public sealed class RulesetNode : StyleSheetRuleNode
    {
        /// <summary>Initializes a new instance of the RulesetNode class</summary>
        /// <param name="selectorsGroupNode">Selectors group node.</param>
        /// <param name="declarations">The list of declarations.</param>
        public RulesetNode(SelectorsGroupNode selectorsGroupNode, ReadOnlyCollection<DeclarationNode> declarations, ReadOnlyCollection<ImportantCommentNode> importantComments)
        {
            Contract.Requires(selectorsGroupNode != null);

            // Member Initialization
            this.SelectorsGroupNode = selectorsGroupNode;
            this.Declarations = declarations ?? new List<DeclarationNode>(0).AsReadOnly();
            this.ImportantComments = importantComments ?? new List<ImportantCommentNode>(0).AsReadOnly();
        }

        /// <summary>
        /// Gets the important comments
        /// </summary>
        /// <value>The ImportantCommentNodes</value>
        public ReadOnlyCollection<ImportantCommentNode> ImportantComments { get; private set; }


        /// <summary>
        /// Gets the selectors group node.
        /// </summary>
        /// <value>The selectors group node.</value>
        public SelectorsGroupNode SelectorsGroupNode { get; private set; }

        /// <summary>
        /// Gets Declarations
        /// </summary>
        /// <value>Declarations dictionary</value>
        public ReadOnlyCollection<DeclarationNode> Declarations { get; private set; }

        /// <summary>
        /// Check membership of each declaration in the dictionary.
        /// </summary>
        /// <param name="declarationDictionary"></param>
        /// <returns></returns>
        public bool hasConflictingDelcaration(OrderedDictionary declarationDictionary)
        {
            foreach (var declaration in Declarations)
            {
                if (declarationDictionary.Contains(declaration.Property))
                {
                    return true;
                }                
            }
            return false;
        }

        /// <summary>Defines an accept operation</summary>
        /// <param name="nodeVisitor">The visitor to invoke</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        public override AstNode Accept(NodeVisitor nodeVisitor)
        {
            return nodeVisitor.VisitRulesetNode(this);
        } 
    }
}