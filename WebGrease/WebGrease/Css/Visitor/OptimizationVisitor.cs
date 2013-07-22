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
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using Ast;
    using Extensions;

    using WebGrease.Css.Ast.MediaQuery;

    /// <summary>Provides the optimize selectors visitor for the ASTs</summary>
    internal class OptimizationVisitor : NodeVisitor
    {
        /// <summary>Gets or sets a value indicating whether should merge media queries.</summary>
        internal bool ShouldMergeMediaQueries { get; set; }

        /// <summary>The <see cref="StyleSheetNode"/> visit implementation</summary>
        /// <param name="styleSheet">The styleSheet AST node</param>
        /// <returns>The modified AST node if modified otherwise the original node</returns>
        public override AstNode VisitStyleSheetNode(StyleSheetNode styleSheet)
        {
            if (styleSheet == null)
            {
                return null;
            }

            var ruleSetMediaPageDictionary = this.GetMergedNodeDictionary(styleSheet.StyleSheetRules);

            // Extract the updated list from dictionary
            var styleSheetRuleNodes = ruleSetMediaPageDictionary.Values.Cast<StyleSheetRuleNode>().ToList();

            // Create a new object with updated rulesets list.
            return new StyleSheetNode(
                styleSheet.CharSetString,
                styleSheet.Dpi,
                styleSheet.Imports,
                styleSheet.Namespaces,
                styleSheetRuleNodes.AsSafeReadOnly());
        }

        /// <summary>The optimize ruleset node.</summary>
        /// <param name="currentRuleSet">The current rule set.</param>
        /// <param name="ruleSetMediaPageDictionary">The rule set media page dictionary.</param>
        private static void OptimizeRulesetNode(RulesetNode currentRuleSet, OrderedDictionary ruleSetMediaPageDictionary, OrderedDictionary rulesetHashKeysDictionary)
        {
            string primaryHashKey;

            // Ruleset node optimization.
            // Selectors concatenation is the hash key here
            // Update: Now this is only primary hash key.
            primaryHashKey = currentRuleSet.PrintSelector();
            string hashKey = currentRuleSet.PrintSelector();

            if (rulesetHashKeysDictionary.Contains(primaryHashKey))
            {
                hashKey = ((List<string>)rulesetHashKeysDictionary[primaryHashKey]).Last();
            }
            else
            {
                rulesetHashKeysDictionary.Add(primaryHashKey, new List<string>());
                (rulesetHashKeysDictionary[primaryHashKey] as List<string>).Add(primaryHashKey);
            }
            // If a RuleSet exists already, then remove from
            // dictionary and add a new ruleset with the
            // declarations merged. Don't clobber the item in 
            // dictionary to preserve the order and keep the last
            // seen RuleSet
            if (ShouldCollapseTheNewRuleset(hashKey, ruleSetMediaPageDictionary, currentRuleSet))
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
                    // Generates an unique hashkey again, if hashKey already exists.
                    while (ruleSetMediaPageDictionary.Contains(hashKey))
                    {
                        hashKey = GenerateRandomkey();
                    }
                    ruleSetMediaPageDictionary.Add(hashKey, newRuleSet);
                    (rulesetHashKeysDictionary[primaryHashKey] as List<string>).Add(hashKey);
                }
            }
        }

        /// <summary>
        /// Generates 8 digit random key.
        /// </summary>
        /// <returns>8 digit random key.</returns>
        private static string GenerateRandomkey()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new string(
                Enumerable.Repeat(chars, 8)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray());
            return result;
        }

        /// <summary>
        /// Determines if we should collapse with the old ruleset.
        /// </summary>
        /// <param name="hashKey"> hash key of last same ruleset occurs</param>
        /// <param name="ruleSetMediaPageDictionary">Dictionary of hash keys and RulesetNodes</param>
        /// <param name="currentRuleSet">RulesetNode currently tracking.</param>
        /// <returns>Whether we should collapse or not.</returns>
        private static bool ShouldCollapseTheNewRuleset(string hashKey, OrderedDictionary ruleSetMediaPageDictionary, RulesetNode currentRuleSet)
        {
            if (ruleSetMediaPageDictionary.Contains(hashKey))
            {
                // Dictionary for declarations
                OrderedDictionary declarationNodeDictionary = new OrderedDictionary();
                var styleSheetRuleNodes = ruleSetMediaPageDictionary.Values.Cast<StyleSheetRuleNode>().ToList();

                // Iterate through all rulesetnodes.
                for (int i = styleSheetRuleNodes.Count - 1; i > -1; i--)
                {
                    var previousStyleSheetRulesetNode = styleSheetRuleNodes.ElementAt(i);

                    // If the node is actually RulesetNode
                    if (currentRuleSet.GetType().IsAssignableFrom(previousStyleSheetRulesetNode.GetType()))
                    {
                        // Previous Ruleset Node
                        var previousRulesetNode =previousStyleSheetRulesetNode as RulesetNode;

                        // If Ruleset node has same selectors set
                        if(previousRulesetNode.PrintSelector().Equals(currentRuleSet.PrintSelector()))
                        {
                            return true;
                        }

                        // Add each declaration of the previous ruleset in the dictionary
                        foreach (var declaration in previousRulesetNode.Declarations)
                        {
                            string hashKeyForDeclaration = declaration.Property;
                            if (!declarationNodeDictionary.Contains(hashKeyForDeclaration))
                            {
                                declarationNodeDictionary[hashKeyForDeclaration] = declaration;
                            }
                        }

                        // Check if the last same RulesetNode has same declaration property              
                        var lastRuleSet=(RulesetNode) ruleSetMediaPageDictionary[hashKey];
                        if (lastRuleSet.hasConflictingDelcaration(declarationNodeDictionary))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            return false;
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
            // previous release of Csl. The unique declarations in source not found in the destination are gathered first 
            // followed by unique declarations in destination.
            // Example:
            //  div
            //  {
            //      k1:v1;
            //      k2:v2;
            //  }
            //  div
            //  {
            //      k1:v3;
            //      k3:v4;
            //  }
            //
            // becomes:
            //
            //  div
            //  {
            //      k2:v2;
            //      k1:v3;
            //      k3:v4;
            //  }
            //
            // Vendor specific values are suopposed to live next to each other.
            // display: -ms-grid;
            // display: -moz-box;
            // display: block;
            // should not be merged.
            // Logic is that non vendor values get merged and vendor values are kept as duplicates, except for vendor properties.
            var uniqueDestinationDeclarations = UniqueDeclarations(destinationRuleset);
            var targetDeclarations = UniqueDeclarations(sourceRuleset);
            foreach (DeclarationNode newDeclaration in uniqueDestinationDeclarations.Values)
            {
                AddDeclaration(targetDeclarations, newDeclaration);
            }

            // Convert dictionary to list
            var resultDeclarations = targetDeclarations.Values.Cast<DeclarationNode>().ToList();
            return new RulesetNode(destinationRuleset.SelectorsGroupNode, resultDeclarations.AsReadOnly(), sourceRuleset.ImportantComments);
        }

        /// <summary>The add declaration.</summary>
        /// <param name="uniqueSourceDeclarations">The unique source declarations.</param>
        /// <param name="newDeclaration">The new declaration.</param>
        private static void AddDeclaration(OrderedDictionary uniqueSourceDeclarations, DeclarationNode newDeclaration)
        {
            var uniquePropertyKey = GetUniquePropertyKey(newDeclaration);
            if (uniqueSourceDeclarations.Contains(uniquePropertyKey))
            {
                var previousDeclarationNode = uniqueSourceDeclarations[uniquePropertyKey] as DeclarationNode;
                if (HasImportantFlag(previousDeclarationNode) && !HasImportantFlag(newDeclaration))
                {
                    return;
                }

                uniqueSourceDeclarations.Remove(uniquePropertyKey);
            }

            uniqueSourceDeclarations.Add(uniquePropertyKey, newDeclaration);
        }

        /// <summary>Checks if the declaration has !important.</summary>
        /// <param name="declarationNode">The previous declaration node.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private static bool HasImportantFlag(DeclarationNode declarationNode)
        {
            return declarationNode.Prio.Equals(CssConstants.Important);
        }

        /// <summary>Gets the unique property key taking into account both property and value vendor prefixes.</summary>
        /// <param name="declarationNode">The declaration node.</param>
        /// <returns>The unique key for the property<see cref="string"/>.</returns>
        private static string GetUniquePropertyKey(DeclarationNode declarationNode)
        {
            var propertyName = declarationNode.Property;
            var propertyVendorPrefix = GetVendorPrefix(propertyName);
            if (!string.IsNullOrWhiteSpace(propertyVendorPrefix))
            {
                return propertyName;
            }

            var stringBasedValue = declarationNode.ExprNode.TermNode.StringBasedValue;
            if (!string.IsNullOrWhiteSpace(stringBasedValue))
            {
                var vendorPrefix = GetVendorPrefix(stringBasedValue);
                if (!string.IsNullOrWhiteSpace(vendorPrefix))
                {
                    return vendorPrefix + propertyName;
                }
            }

            return propertyName;
        }

        /// <summary>Gets the vendor prefix for the string base value.</summary>
        /// <param name="stringBasedValue">The string based value.</param>
        /// <returns>The vendor prefix <see cref="string"/>, for example: "-moz-" or "-ms-".</returns>
        private static string GetVendorPrefix(string stringBasedValue)
        {
            if (stringBasedValue.StartsWith("-", StringComparison.OrdinalIgnoreCase))
            {
                var indexOfSecondDash = stringBasedValue.IndexOf("-", 2, StringComparison.OrdinalIgnoreCase);
                if (indexOfSecondDash < stringBasedValue.Length - 1)
                {
                    return stringBasedValue.Substring(0, indexOfSecondDash + 1);
                }
            }

            return null;
        }

        /// <summary>Converts a ruleset node to dictionary preserving the order of nodes</summary>
        /// <param name="rulesetNode">The ruleset node to scan</param>
        /// <returns>The ordered dictionary</returns>
        private static OrderedDictionary UniqueDeclarations(RulesetNode rulesetNode)
        {
            var dictionary = new OrderedDictionary();
            foreach (var declarationNode in rulesetNode.Declarations)
            {
                AddDeclaration(dictionary, declarationNode);
            }

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
            return new RulesetNode(rulesetNode.SelectorsGroupNode, resultDeclarations.AsReadOnly(), rulesetNode.ImportantComments);
        }

        /// <summary>Gets merged node dictionary for the given stylesheet rule nodes.</summary>
        /// <param name="styleSheetRuleNodes">The style sheet rule nodes.</param>
        /// <returns>The <see cref="OrderedDictionary"/>.</returns>
        private OrderedDictionary GetMergedNodeDictionary(IEnumerable<StyleSheetRuleNode> styleSheetRuleNodes)
        {
            // List of updated ruleset, media and page nodes
            var ruleSetMediaPageDictionary = new OrderedDictionary();
            var ruleSetHashKeysDictionary = new OrderedDictionary();

            foreach (var styleSheetRuleNode in styleSheetRuleNodes)
            {
                var rulesetNode = styleSheetRuleNode as RulesetNode;
                if (rulesetNode != null)
                {
                    // Optimize rulesets
                    OptimizeRulesetNode(rulesetNode, ruleSetMediaPageDictionary, ruleSetHashKeysDictionary);
                    continue;
                }

                if (this.ShouldMergeMediaQueries)
                {
                    var mediaNode = styleSheetRuleNode as MediaNode;
                    if (mediaNode != null)
                    {
                        // Optimize media queries, will call this method for its rulesets and pagenodes.
                        this.OptimizeMediaQuery(mediaNode, ruleSetMediaPageDictionary);
                        continue;
                    }
                }

                // Page node optimization.
                // Full ruleset node (media or page) is a hash key here.
                // By design, we don't optimize the ruleset inside media nodes.
                string hashKey = styleSheetRuleNode.MinifyPrint();
                if (!ruleSetMediaPageDictionary.Contains(hashKey))
                {
                    ruleSetMediaPageDictionary.Add(hashKey, styleSheetRuleNode);
                }
                else
                {
                    ruleSetMediaPageDictionary[hashKey] = styleSheetRuleNode;
                }
            }

            return ruleSetMediaPageDictionary;
        }

        /// <summary>The optimize media query.</summary>
        /// <param name="mediaNode">The media node.</param>
        /// <param name="ruleSetMediaPageDictionary">The rule set media page dictionary.</param>
        private void OptimizeMediaQuery(MediaNode mediaNode, OrderedDictionary ruleSetMediaPageDictionary)
        {
            var mediaNodeHashKey = mediaNode.PrintSelector();
            var pageNodes = mediaNode.PageNodes.ToList();
            var rulesetNodes = mediaNode.Rulesets.ToList();

            if (ruleSetMediaPageDictionary.Contains(mediaNodeHashKey))
            {
                var previousNode = ruleSetMediaPageDictionary[mediaNodeHashKey] as MediaNode;
                if (previousNode != null)
                {
                    pageNodes = previousNode.PageNodes.Concat(pageNodes).ToList();
                    rulesetNodes = previousNode.Rulesets.Concat(rulesetNodes).ToList();
                }

                ruleSetMediaPageDictionary.Remove(mediaNodeHashKey);
            }

            ruleSetMediaPageDictionary.Add(
                mediaNodeHashKey,
                new MediaNode(
                    mediaNode.MediaQueries,
                    this.GetMergedNodeDictionary(rulesetNodes).Values.Cast<RulesetNode>().ToList().AsSafeReadOnly(),
                    pageNodes.ToSafeReadOnlyCollection()));
        }
    }
}