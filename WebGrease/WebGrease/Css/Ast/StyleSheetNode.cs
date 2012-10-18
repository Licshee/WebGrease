// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StyleSheetNode.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   styleSheet:
//   [ CHARSET_SYM STRING ';' ]?
//   [S|CDO|CDC]* [ import [S|CDO|CDC]* ]* [ namespace [S|CDO|CDC]* ]*
//   [ [ ruleset | media | page | keyframes ] [S|CDO|CDC]* ]*
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.Ast
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Visitor;

    /// <summary>styleSheet:
    /// [ CHARSET_SYM STRING ';' ]? 
    /// [S|CDO|CDC]* [ import [S|CDO|CDC]* ]* [ namespace [S|CDO|CDC]* ]*
    /// [ [ ruleset | media | page | keyframes ] [S|CDO|CDC]* ]*</summary>
    public sealed class StyleSheetNode : AstNode
    {
        /// <summary>Initializes a new instance of the StyleSheetNode class</summary>
        /// <param name="charSetString">Character String</param>
        /// <param name="imports">Imports list</param>
        /// <param name="namespaces">The list of namespaces</param>
        /// <param name="styleSheetRules">StyleSheet nodes dictionary</param>
        public StyleSheetNode(string charSetString, ReadOnlyCollection<ImportNode> imports, ReadOnlyCollection<NamespaceNode> namespaces, ReadOnlyCollection<StyleSheetRuleNode> styleSheetRules)
        {
            this.CharSetString = charSetString ?? string.Empty;
            this.Imports = imports ?? new List<ImportNode>(0).AsReadOnly();
            this.Namespaces = namespaces ?? new List<NamespaceNode>(0).AsReadOnly();
            this.StyleSheetRules = styleSheetRules ?? new List<StyleSheetRuleNode>(0).AsReadOnly();
        }

        /// <summary>
        /// Gets Character Set string
        /// </summary>
        public string CharSetString { get; private set; }

        /// <summary>
        /// Gets Imports list
        /// </summary>
        public ReadOnlyCollection<ImportNode> Imports { get; private set; }

        /// <summary>Gets the list of namespaces.</summary>
        public ReadOnlyCollection<NamespaceNode> Namespaces { get; private set; }

        /// <summary>
        /// Gets the styleSheet rules
        /// </summary>
        public ReadOnlyCollection<StyleSheetRuleNode> StyleSheetRules { get; private set; }

        /// <summary>Defines an accept operation</summary>
        /// <param name="nodeVisitor">The visitor to invoke</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        public override AstNode Accept(NodeVisitor nodeVisitor)
        {
            return nodeVisitor.VisitStyleSheetNode(this);
        }
    }
}