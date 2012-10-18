// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OptimizationVisitor.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Provides the optimize selectors visitor for the ASTs
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.Visitor
{
    using System.Collections;
    using System.Collections.Specialized;
    using System.Linq;
    using Ast;
    using Extensions;

    /// <summary>Provides the optimize selectors visitor for the ASTs</summary>
    public class OptimizationVisitor : NodeVisitor
    {
        /// <summary>The <see cref="StyleSheetNode"/> visit implementation</summary>
        /// <param name="styleSheet">The styleSheet AST node</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        public override AstNode VisitStyleSheetNode(StyleSheetNode styleSheet)
        {
            if (styleSheet == null)
            {
                return null;
            }

            // List of updated ruleset, media and page nodes
            var ruleSetMediaPageDictionary = new OrderedDictionary();

            styleSheet.StyleSheetRules.ForEach(ruleSetMediaPage =>
                                                         {
                                                             string hashKey;
                                                             var currentRuleSet = ruleSetMediaPage as RulesetNode;

                                                             if (currentRuleSet != null)
                                                             {
                                                                 // Ruleset node optimization.
                                                                 // Selectors concatenation is the hash key here
                                                                 hashKey = currentRuleSet.PrintSelector();

                                                                 // If a RuleSet exists already, then remove from
                                                                 // dictionary and add a new ruleset with the
                                                                 // declarations merged. Don't clobber the item in 
                                                                 // dictionary to preserve the order and keep the last
                                                                 // seen RuleSet
                                                                 if (ruleSetMediaPageDictionary.Contains(hashKey))
                                                                 {
                                                                     // Merge the declarations
                                                                     var newRuleSet = MergeDeclarations((RulesetNode)ruleSetMediaPageDictionary[hashKey], currentRuleSet);

                                                                     // Remove the old ruleset from old position
                                                                     ruleSetMediaPageDictionary.Remove(hashKey);

                                                                     // Add a new ruleset at later position
                                                                     ruleSetMediaPageDictionary.Add(hashKey, newRuleSet);
                                                                 }
                                                                 else
                                                                 {
                                                                     var newRuleSet = OptimizeRuleset(currentRuleSet);

                                                                     // Add the ruleset if there is atleast one unique declaration
                                                                     if (newRuleSet != null)
                                                                     {
                                                                        ruleSetMediaPageDictionary.Add(hashKey, newRuleSet);
                                                                     }
                                                                 }
                                                             }
                                                             else
                                                             {
                                                                 // Media or page node optimization.
                                                                 // Full ruleset node (media or page) is a hash key here.
                                                                 // By design, we don't optimize the ruleset inside media nodes.
                                                                 hashKey = ruleSetMediaPage.MinifyPrint();

                                                                 if (!ruleSetMediaPageDictionary.Contains(hashKey))
                                                                 {
                                                                     ruleSetMediaPageDictionary.Add(hashKey, ruleSetMediaPage);
                                                                 }
                                                                 else
                                                                 {
                                                                     ruleSetMediaPageDictionary[hashKey] = ruleSetMediaPage;
                                                                 }
                                                             }
                                                         });

            // Extract the updated list from dictionary
            var styleSheetRuleNodes = ruleSetMediaPageDictionary.Values.Cast<StyleSheetRuleNode>().ToList();

            // Create a new object with updated rulesets list.
            return new StyleSheetNode(styleSheet.CharSetString, styleSheet.Imports, styleSheet.Namespaces, styleSheetRuleNodes.AsSafeReadOnly());
        }

        /// <summary>Computes the merged set of RulesetNode based on the declarations</summary>
        /// <param name="sourceRuleset">The original ruleset</param>
        /// <param name="destinationRuleset">The new ruleset which should take the non matched declarations from <paramref name="sourceRuleset"/></param>
        /// <returns>The new ruleset with the merged declarations</returns>
        private static RulesetNode MergeDeclarations(RulesetNode sourceRuleset, RulesetNode destinationRuleset)
        {
            // First take the unique declarations from destinationRuleset and sourceRuleset for following scenario:
            // #foo
            // {
            // k1:v1
            // k1:v2
            // }
            // To
            // #foo
            // {
            // k1:v2;
            // }

            // Combine the declarations preserving the order (later key takes preference with in a scope of ruleset).
            // This could have been a simple dictionary merge but it would break the backward compatibility here with 
            // previous release of Csl. The unique declarations in destination are gathered first followed by unique 
            // declarations in source which are not found in destination declaration.
            // Note - This scenario is pretty bug prone not knowing the semantics of css here. A short-hand property can really
            // lead to fall apart of this optimization. Discuss with product owner about this.
            var uniqueDestinationDeclarations = UniqueDeclarations(destinationRuleset);
            var uniqueSourceDeclarations = UniqueDeclarations(sourceRuleset);

            // Append source selectors to destination now (with preserving order)
            foreach (DeclarationNode sourceDeclaration in uniqueSourceDeclarations.Values)
            {
                AddUpdateDictionary(uniqueDestinationDeclarations, sourceDeclaration, false);
            }

            // Convert dictionary to list
            var resultDeclarations = uniqueDestinationDeclarations.Values.Cast<DeclarationNode>().ToList();
            return new RulesetNode(destinationRuleset.SelectorsGroupNode, resultDeclarations.AsReadOnly());
        }

        /// <summary>Converts a ruleset node to dictionary preserving the order of nodes</summary>
        /// <param name="rulesetNode">The ruleset node to scan</param>
        /// <returns>The ordered dictionary</returns>
        private static OrderedDictionary UniqueDeclarations(RulesetNode rulesetNode)
        {
            var dictionary = new OrderedDictionary();
            rulesetNode.Declarations.ForEach(declarationNode => AddUpdateDictionary(dictionary, declarationNode, true));
            return dictionary;
        }

        /// <summary>Optimizes a ruleset node to remove the declarations preserving the order of nodes</summary>
        /// <param name="rulesetNode">The ruleset node to scan</param>
        /// <returns>The updated ruleset</returns>
        private static RulesetNode OptimizeRuleset(RulesetNode rulesetNode)
        {
            // Omit the complete ruleset if there are no declarations
            if (rulesetNode.Declarations.Count == 0)
            {
                return null;
            }

            var dictionary = UniqueDeclarations(rulesetNode);
            var resultDeclarations = dictionary.Values.Cast<DeclarationNode>().ToList();
            return new RulesetNode(rulesetNode.SelectorsGroupNode, resultDeclarations.AsReadOnly());
        }

        /// <summary>Adds or updates the dictionary after checking the presence of declaration property key</summary>
        /// <param name="dictionary">The dictionary to update</param>
        /// <param name="declarationNode">The declaration node</param>
        /// <param name="clobber">Determines if the exisitng entry should be overwritten</param>
        private static void AddUpdateDictionary(IDictionary dictionary, DeclarationNode declarationNode, bool clobber)
        {
            if (dictionary.Contains(declarationNode.Property))
            {
                if (clobber)
                {
                    dictionary[declarationNode.Property] = declarationNode;
                }
            }
            else
            {
                dictionary.Add(declarationNode.Property, declarationNode);
            }
        }
    }
}