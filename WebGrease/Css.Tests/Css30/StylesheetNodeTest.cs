// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ParserTest.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   This is a test class for ParserTest and is intended
//   to contain all ParserTest Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Css.Tests.Css30
{
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WebGrease.Tests;

    using TestSuite;
    using WebGrease.Css;
    using WebGrease.Css.Ast;
    using WebGrease.Css.Ast.Animation;
    using WebGrease.Css.Ast.MediaQuery;
    using WebGrease.Css.Ast.Selectors;

    /// <summary>
    /// This is a test class for ParserTest and is intended
    /// to contain all ParserTest Unit Tests
    /// </summary>
    [TestClass]
    public class ParserTest
    {
        /// <summary>The base directory.</summary>
        private static readonly string BaseDirectory;

        /// <summary>The expect directory.</summary>
        private static readonly string ActualDirectory;

        /// <summary>Initializes static members of the <see cref="ParserTest"/> class.</summary>
        static ParserTest()
        {
            BaseDirectory = Path.Combine(TestDeploymentPaths.TestDirectory, @"css.tests\css30\parser");
            ActualDirectory = Path.Combine(BaseDirectory, @"actual");
        }

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>A test for important selectors</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void ImportantTest()
        {
            const string FileName = @"important.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);

            var styleSheetRules = styleSheetNode.StyleSheetRules;
            Assert.IsNotNull(styleSheetRules);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for gradient</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void GradientTest()
        {
            const string FileName = @"gradient.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);

            var styleSheetRules = styleSheetNode.StyleSheetRules;
            Assert.IsNotNull(styleSheetRules);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for data uri</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void DataUriTest()
        {
            const string FileName = @"datauri.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);

            var styleSheetRules = styleSheetNode.StyleSheetRules;
            Assert.IsNotNull(styleSheetRules);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for background</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void BackgroundTest()
        {
            const string FileName = @"background.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);

            var styleSheetRules = styleSheetNode.StyleSheetRules;
            Assert.IsNotNull(styleSheetRules);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for border</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void BorderTest()
        {
            const string FileName = @"border.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);

            var styleSheetRules = styleSheetNode.StyleSheetRules;
            Assert.IsNotNull(styleSheetRules);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for colors</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void ColorsTest()
        {
            const string FileName = @"colors.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);

            var styleSheetRules = styleSheetNode.StyleSheetRules;
            Assert.IsNotNull(styleSheetRules);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for font face</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void FontFaceTest()
        {
            const string FileName = @"fontface.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);

            var styleSheetRules = styleSheetNode.StyleSheetRules;
            Assert.IsNotNull(styleSheetRules);
            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for animations</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void AnimationsTest()
        {
            const string FileName = @"animations.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);

            var styleSheetRules = styleSheetNode.StyleSheetRules;
            Assert.IsNotNull(styleSheetRules);
            Assert.IsTrue(styleSheetRules.Count == 10);

            var keyFramesNode = styleSheetRules[0] as KeyFramesNode;
            Assert.IsNotNull(keyFramesNode);
            Assert.IsTrue(keyFramesNode.KeyFramesSymbol == "@keyframes");
            Assert.IsTrue(keyFramesNode.StringValue == "'bounce'");
            var keyFramesBlockNodes = keyFramesNode.KeyFramesBlockNodes;
            Assert.IsNotNull(keyFramesBlockNodes.Count == 5);

            // First Key Frame
            var keyFramesBlockNode = keyFramesBlockNodes[0];
            Assert.IsNotNull(keyFramesBlockNode);

            var declarationNodes = keyFramesBlockNode.DeclarationNodes;
            Assert.IsTrue(declarationNodes.Count == 2);

            var declarationNode = declarationNodes[0];
            Assert.IsNotNull(declarationNode);
            Assert.IsTrue(declarationNode.Property == "top");
            var expr = declarationNode.ExprNode;
            Assert.IsNotNull(expr);
            Assert.IsTrue(expr.TermNode.NumberBasedValue == "100px");

            declarationNode = declarationNodes[1];
            Assert.IsNotNull(declarationNode);
            Assert.IsTrue(declarationNode.Property == "animation-timing-function");
            expr = declarationNode.ExprNode;
            Assert.IsNotNull(expr);
            Assert.IsTrue(expr.TermNode.StringBasedValue == "ease-out");

            var keyFramesSelectors = keyFramesBlockNode.KeyFramesSelectors;
            Assert.IsTrue(keyFramesSelectors.Count == 2);

            var keyFramesSelector = keyFramesBlockNode.KeyFramesSelectors[0];
            Assert.IsNotNull(keyFramesSelector);
            Assert.IsTrue(keyFramesSelector == "from");

            keyFramesSelector = keyFramesBlockNode.KeyFramesSelectors[1];
            Assert.IsNotNull(keyFramesSelector);
            Assert.IsTrue(keyFramesSelector == "25%");

            // Second Key Frame
            keyFramesBlockNode = keyFramesBlockNodes[1];
            Assert.IsNotNull(keyFramesBlockNode);

            declarationNodes = keyFramesBlockNode.DeclarationNodes;
            Assert.IsTrue(declarationNodes.Count == 2);

            declarationNode = declarationNodes[0];
            Assert.IsNotNull(declarationNode);
            Assert.IsTrue(declarationNode.Property == "top");
            expr = declarationNode.ExprNode;
            Assert.IsNotNull(expr);
            Assert.IsTrue(expr.TermNode.NumberBasedValue == "50px");

            declarationNode = declarationNodes[1];
            Assert.IsNotNull(declarationNode);
            Assert.IsTrue(declarationNode.Property == "animation-timing-function");
            expr = declarationNode.ExprNode;
            Assert.IsNotNull(expr);
            Assert.IsTrue(expr.TermNode.StringBasedValue == "ease-in");

            keyFramesSelectors = keyFramesBlockNode.KeyFramesSelectors;
            Assert.IsTrue(keyFramesSelectors.Count == 1);

            keyFramesSelector = keyFramesBlockNode.KeyFramesSelectors[0];
            Assert.IsNotNull(keyFramesSelector);
            Assert.IsTrue(keyFramesSelector == "25%");

            // Third Key Frame
            keyFramesBlockNode = keyFramesBlockNodes[2];
            Assert.IsNotNull(keyFramesBlockNode);

            declarationNodes = keyFramesBlockNode.DeclarationNodes;
            Assert.IsTrue(declarationNodes.Count == 2);

            declarationNode = declarationNodes[0];
            Assert.IsNotNull(declarationNode);
            Assert.IsTrue(declarationNode.Property == "top");
            expr = declarationNode.ExprNode;
            Assert.IsNotNull(expr);
            Assert.IsTrue(expr.TermNode.NumberBasedValue == "100px");

            declarationNode = declarationNodes[1];
            Assert.IsNotNull(declarationNode);
            Assert.IsTrue(declarationNode.Property == "animation-timing-function");
            expr = declarationNode.ExprNode;
            Assert.IsNotNull(expr);
            Assert.IsTrue(expr.TermNode.StringBasedValue == "ease-out");

            keyFramesSelectors = keyFramesBlockNode.KeyFramesSelectors;
            Assert.IsTrue(keyFramesSelectors.Count == 1);

            keyFramesSelector = keyFramesBlockNode.KeyFramesSelectors[0];
            Assert.IsNotNull(keyFramesSelector);
            Assert.IsTrue(keyFramesSelector == "50%");

            // Fourth Key Frame
            keyFramesBlockNode = keyFramesBlockNodes[3];
            Assert.IsNotNull(keyFramesBlockNode);

            declarationNodes = keyFramesBlockNode.DeclarationNodes;
            Assert.IsTrue(declarationNodes.Count == 2);

            declarationNode = declarationNodes[0];
            Assert.IsNotNull(declarationNode);
            Assert.IsTrue(declarationNode.Property == "top");
            expr = declarationNode.ExprNode;
            Assert.IsNotNull(expr);
            Assert.IsTrue(expr.TermNode.NumberBasedValue == "75px");

            declarationNode = declarationNodes[1];
            Assert.IsNotNull(declarationNode);
            Assert.IsTrue(declarationNode.Property == "animation-timing-function");
            expr = declarationNode.ExprNode;
            Assert.IsNotNull(expr);
            Assert.IsTrue(expr.TermNode.StringBasedValue == "ease-in");

            keyFramesSelectors = keyFramesBlockNode.KeyFramesSelectors;
            Assert.IsTrue(keyFramesSelectors.Count == 1);

            keyFramesSelector = keyFramesBlockNode.KeyFramesSelectors[0];
            Assert.IsNotNull(keyFramesSelector);
            Assert.IsTrue(keyFramesSelector == "75%");

            // Fifth Key Frame
            keyFramesBlockNode = keyFramesBlockNodes[4];
            Assert.IsNotNull(keyFramesBlockNode);

            declarationNodes = keyFramesBlockNode.DeclarationNodes;
            Assert.IsTrue(declarationNodes.Count == 1);

            declarationNode = declarationNodes[0];
            Assert.IsNotNull(declarationNode);
            Assert.IsTrue(declarationNode.Property == "top");
            expr = declarationNode.ExprNode;
            Assert.IsNotNull(expr);
            Assert.IsTrue(expr.TermNode.NumberBasedValue == "100px");

            keyFramesSelectors = keyFramesBlockNode.KeyFramesSelectors;
            Assert.IsTrue(keyFramesSelectors.Count == 1);

            keyFramesSelector = keyFramesBlockNode.KeyFramesSelectors[0];
            Assert.IsNotNull(keyFramesSelector);
            Assert.IsTrue(keyFramesSelector == "to");

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for charset string</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void CharSetStringTest()
        {
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, @"charset1.css")));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsTrue(styleSheetNode.CharSetString == "'iso-8859-1'");
            MinificationVerifier.VerifyMinification(BaseDirectory, @"charset1.css");
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, @"charset2.css");

            styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, @"charset2.css")));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsTrue(styleSheetNode.CharSetString == "'foo'");
            MinificationVerifier.VerifyMinification(BaseDirectory, @"charset2.css");
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, @"charset2.css");
        }

        /// <summary>A test for imports</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void ImportTest()
        {
            const string FileName = @"import1.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsTrue(styleSheetNode.Imports != null);
            Assert.IsTrue(styleSheetNode.Imports.Count == 11);
            Assert.IsTrue(styleSheetNode.Imports[0].AllowedImportDataType == AllowedImportData.String);
            Assert.IsTrue(styleSheetNode.Imports[0].ImportDataValue == "\"foo.css\"");
            Assert.IsTrue(styleSheetNode.Imports[0].MediaQueries == null || styleSheetNode.Imports[0].MediaQueries.Count == 0);

            Assert.IsTrue(styleSheetNode.Imports[1].AllowedImportDataType == AllowedImportData.Uri);
            Assert.IsTrue(styleSheetNode.Imports[1].ImportDataValue == "url(\"bar.css\")");
            Assert.IsNotNull(styleSheetNode.Imports[1].MediaQueries);
            Assert.IsTrue(styleSheetNode.Imports[1].MediaQueries.Count == 4);
            var mediaQuery = styleSheetNode.Imports[1].MediaQueries[0];
            Assert.IsNotNull(mediaQuery);
            Assert.IsTrue(mediaQuery.MediaType == "foo");

            Assert.IsTrue(styleSheetNode.Imports[2].AllowedImportDataType == AllowedImportData.Uri);
            Assert.IsTrue(styleSheetNode.Imports[2].ImportDataValue == "url(\"foobar.css\")");
            Assert.IsTrue(styleSheetNode.Imports[2].MediaQueries == null || styleSheetNode.Imports[2].MediaQueries.Count == 0);

            Assert.IsTrue(styleSheetNode.Imports[3].AllowedImportDataType == AllowedImportData.Uri);
            Assert.IsTrue(styleSheetNode.Imports[3].ImportDataValue == "url(\"barfoo.css\")");
            Assert.IsTrue(styleSheetNode.Imports[3].MediaQueries == null || styleSheetNode.Imports[3].MediaQueries.Count == 0);

            Assert.IsTrue(styleSheetNode.Imports[4].AllowedImportDataType == AllowedImportData.String);
            Assert.IsTrue(styleSheetNode.Imports[4].ImportDataValue == "\"foo.css\"");
            Assert.IsTrue(styleSheetNode.Imports[4].MediaQueries == null || styleSheetNode.Imports[4].MediaQueries.Count == 0);

            Assert.IsTrue(styleSheetNode.Imports[5].AllowedImportDataType == AllowedImportData.String);
            Assert.IsTrue(styleSheetNode.Imports[5].ImportDataValue == "\"\\0000E9 dition.css\"");
            Assert.IsTrue(styleSheetNode.Imports[5].MediaQueries == null || styleSheetNode.Imports[5].MediaQueries.Count == 0);

            Assert.IsTrue(styleSheetNode.Imports[6].AllowedImportDataType == AllowedImportData.String);
            Assert.IsNotNull(styleSheetNode.Imports[6].ImportDataValue);
            Assert.IsTrue(styleSheetNode.Imports[6].MediaQueries == null || styleSheetNode.Imports[6].MediaQueries.Count == 0);

            Assert.IsTrue(styleSheetNode.Imports[7].AllowedImportDataType == AllowedImportData.String);
            Assert.IsTrue(styleSheetNode.Imports[7].ImportDataValue == "\"\\0928 \\093f \\0924 \\093f \\0928 .css\"");
            Assert.IsTrue(styleSheetNode.Imports[7].MediaQueries == null || styleSheetNode.Imports[7].MediaQueries.Count == 0);

            Assert.IsTrue(styleSheetNode.Imports[8].AllowedImportDataType == AllowedImportData.String);
            Assert.IsTrue(styleSheetNode.Imports[8].ImportDataValue == "\"नितिन.css?foo=1&bar=2\"");
            Assert.IsTrue(styleSheetNode.Imports[8].MediaQueries == null || styleSheetNode.Imports[8].MediaQueries.Count == 0);

            Assert.IsTrue(styleSheetNode.Imports[9].AllowedImportDataType == AllowedImportData.Uri);
            Assert.IsTrue(styleSheetNode.Imports[9].ImportDataValue == "url(\"\\0000E9 dition.css\")");
            Assert.IsTrue(styleSheetNode.Imports[9].MediaQueries == null || styleSheetNode.Imports[9].MediaQueries.Count == 0);

            Assert.IsTrue(styleSheetNode.Imports[10].AllowedImportDataType == AllowedImportData.Uri);
            Assert.IsTrue(styleSheetNode.Imports[10].ImportDataValue == "url(\"*.css\")");
            Assert.IsTrue(styleSheetNode.Imports[10].MediaQueries == null || styleSheetNode.Imports[10].MediaQueries.Count == 0);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for namespaces</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void NamespaceTest()
        {
            const string FileName = @"namespace.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);

            var namespaces = styleSheetNode.Namespaces;
            Assert.IsNotNull(namespaces);
            Assert.IsTrue(namespaces.Count == 4);

            ////
            //// @namespace empty "";
            ////
            var ns = namespaces[0];
            Assert.IsNotNull(ns);
            Assert.IsTrue(ns.Prefix == "empty");
            Assert.IsTrue(ns.Value == "\"\"");

            ////
            //// @namespace "";
            ////
            ns = namespaces[1];
            Assert.IsNotNull(ns);
            Assert.IsNull(ns.Prefix);
            Assert.IsTrue(ns.Value == "\"\"");

            ////
            //// @namespace "http://www.w3.org/1999/xhtml";
            ////
            ns = namespaces[2];
            Assert.IsNotNull(ns);
            Assert.IsNull(ns.Prefix);
            Assert.IsTrue(ns.Value == "\"http://www.w3.org/1999/xhtml\"");

            ////
            //// @namespace "http://www.w3.org/1999/xhtml";
            ////
            ns = namespaces[3];
            Assert.IsNotNull(ns);
            Assert.IsTrue(ns.Prefix == "svg");
            Assert.IsTrue(ns.Value == "\"http://www.w3.org/2000/svg\"");

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for media</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void MediaTest()
        {
            const string FileName = @"media1.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.IsTrue(styleSheetNode.StyleSheetRules.Count == 13);

            // @media print 
            // {
            // body { font-size: 10pt }
            // }
            var mediaNode = styleSheetNode.StyleSheetRules[0] as MediaNode;
            Assert.IsNotNull(mediaNode);
            var mediaQueries = mediaNode.MediaQueries;
            Assert.IsNotNull(mediaQueries);
            Assert.IsTrue(mediaQueries.Count == 1);
            var mediaQuery = mediaQueries[0];
            Assert.IsNotNull(mediaQuery);
            Assert.IsTrue(mediaQuery.MediaType == "print");
            var rulesets = mediaNode.Rulesets;
            Assert.IsNotNull(rulesets);
            Assert.IsTrue(rulesets.Count == 1);
            var ruleset = rulesets[0];
            Assert.IsNotNull(ruleset);
            var selectorsGroupNode = ruleset.SelectorsGroupNode;
            Assert.IsNotNull(selectorsGroupNode);
            var selectors = selectorsGroupNode.SelectorNodes;
            Assert.IsNotNull(selectors);
            Assert.IsTrue(selectors.Count == 1);
            var selector = selectors[0];
            Assert.IsNotNull(selector);
            var simpleSelector = selector.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelector);
            var typeSelectorNode = simpleSelector.TypeSelectorNode;
            Assert.IsNotNull(typeSelectorNode);
            Assert.IsTrue(typeSelectorNode.ElementName == "body");
            var declarations = ruleset.Declarations;
            Assert.IsNotNull(declarations);
            Assert.IsTrue(declarations.Count == 1);
            var declaration = declarations[0];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "font-size");

            mediaNode = styleSheetNode.StyleSheetRules[1] as MediaNode;
            Assert.IsNotNull(mediaNode);
            mediaQueries = mediaNode.MediaQueries;
            Assert.IsNotNull(mediaQueries);
            Assert.IsTrue(mediaQueries.Count == 1);
            mediaQuery = mediaQueries[0];
            Assert.IsNotNull(mediaQuery);
            Assert.IsTrue(mediaQuery.MediaType == "screen");

            mediaNode = styleSheetNode.StyleSheetRules[2] as MediaNode;
            Assert.IsNotNull(mediaNode);
            mediaQueries = mediaNode.MediaQueries;
            Assert.IsNotNull(mediaQueries);
            Assert.IsTrue(mediaQueries.Count == 2);
            mediaQuery = mediaQueries[0];
            Assert.IsNotNull(mediaQuery);
            Assert.IsTrue(mediaQuery.MediaType == "screen");
            mediaQuery = mediaQueries[1];
            Assert.IsNotNull(mediaQuery);
            Assert.IsTrue(mediaQuery.MediaType == "print");
            rulesets = mediaNode.Rulesets;
            Assert.IsNotNull(rulesets);
            Assert.IsTrue(rulesets.Count == 3);

            mediaNode = styleSheetNode.StyleSheetRules[3] as MediaNode;
            Assert.IsNotNull(mediaNode);
            mediaQueries = mediaNode.MediaQueries;
            Assert.IsNotNull(mediaQueries);
            Assert.IsTrue(mediaQueries.Count == 1);
            mediaQuery = mediaQueries[0];
            Assert.IsNotNull(mediaQuery);
            Assert.IsTrue(mediaQuery.MediaType == "screen");
            Assert.IsTrue(mediaQuery.OnlyText == "only");
            rulesets = mediaNode.Rulesets;
            Assert.IsNotNull(rulesets);
            Assert.IsTrue(rulesets.Count == 0);

            mediaNode = styleSheetNode.StyleSheetRules[4] as MediaNode;
            Assert.IsNotNull(mediaNode);
            mediaQueries = mediaNode.MediaQueries;
            Assert.IsNotNull(mediaQueries);
            Assert.IsTrue(mediaQueries.Count == 1);
            mediaQuery = mediaQueries[0];
            Assert.IsNotNull(mediaQuery);
            Assert.IsTrue(mediaQuery.MediaType == "screen");
            Assert.IsTrue(mediaQuery.NotText == "not");
            rulesets = mediaNode.Rulesets;
            Assert.IsNotNull(rulesets);
            Assert.IsTrue(rulesets.Count == 0);

            mediaNode = styleSheetNode.StyleSheetRules[5] as MediaNode;
            Assert.IsNotNull(mediaNode);
            mediaQueries = mediaNode.MediaQueries;
            Assert.IsNotNull(mediaQueries);
            Assert.IsTrue(mediaQueries.Count == 1);
            mediaQuery = mediaQueries[0];
            Assert.IsNotNull(mediaQuery);
            Assert.IsTrue(mediaQuery.MediaType == "screen");
            Assert.IsTrue(mediaQuery.OnlyText == "only");
            var mediaExpressions = mediaQuery.MediaExpressions;
            Assert.IsNotNull(mediaExpressions);
            Assert.IsTrue(mediaExpressions.Count == 1);
            var mediaExpression = mediaExpressions[0];
            Assert.IsTrue(mediaExpression.MediaFeature == "color");
            var expr = mediaExpression.ExprNode;
            Assert.IsNull(expr);
            rulesets = mediaNode.Rulesets;
            Assert.IsNotNull(rulesets);
            Assert.IsTrue(rulesets.Count == 0);

            mediaNode = styleSheetNode.StyleSheetRules[6] as MediaNode;
            Assert.IsNotNull(mediaNode);
            mediaQueries = mediaNode.MediaQueries;
            Assert.IsNotNull(mediaQueries);
            Assert.IsTrue(mediaQueries.Count == 1);
            mediaQuery = mediaQueries[0];
            Assert.IsNotNull(mediaQuery);
            Assert.IsTrue(mediaQuery.MediaType == "screen");
            Assert.IsTrue(mediaQuery.NotText == "not");
            mediaExpressions = mediaQuery.MediaExpressions;
            Assert.IsNotNull(mediaExpressions);
            Assert.IsTrue(mediaExpressions.Count == 1);
            mediaExpression = mediaExpressions[0];
            Assert.IsTrue(mediaExpression.MediaFeature == "color");
            expr = mediaExpression.ExprNode;
            Assert.IsNull(expr);
            rulesets = mediaNode.Rulesets;
            Assert.IsNotNull(rulesets);
            Assert.IsTrue(rulesets.Count == 0);

            mediaNode = styleSheetNode.StyleSheetRules[7] as MediaNode;
            Assert.IsNotNull(mediaNode);
            mediaQueries = mediaNode.MediaQueries;
            Assert.IsNotNull(mediaQueries);
            Assert.IsTrue(mediaQueries.Count == 1);
            mediaQuery = mediaQueries[0];
            Assert.IsNotNull(mediaQuery);
            Assert.IsTrue(mediaQuery.MediaType == "screen");
            Assert.IsTrue(mediaQuery.OnlyText == "only");
            mediaExpressions = mediaQuery.MediaExpressions;
            Assert.IsNotNull(mediaExpressions);
            Assert.IsTrue(mediaExpressions.Count == 2);
            mediaExpression = mediaExpressions[0];
            Assert.IsTrue(mediaExpression.MediaFeature == "color");
            expr = mediaExpression.ExprNode;
            Assert.IsNull(expr);
            mediaExpression = mediaExpressions[1];
            Assert.IsTrue(mediaExpression.MediaFeature == "device-aspect-ratio");
            expr = mediaExpression.ExprNode;
            Assert.IsNotNull(expr);
            Assert.IsTrue(expr.TermNode.NumberBasedValue == "16");
            var termsWithOperators = expr.TermsWithOperators;
            Assert.IsNotNull(termsWithOperators);
            Assert.IsTrue(termsWithOperators.Count == 1);
            var termWithOperator = termsWithOperators[0];
            Assert.IsNotNull(termWithOperator);
            Assert.IsTrue(termWithOperator.Operator == "/");
            Assert.IsTrue(termWithOperator.TermNode.NumberBasedValue == "9");
            rulesets = mediaNode.Rulesets;
            Assert.IsNotNull(rulesets);
            Assert.IsTrue(rulesets.Count == 0);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for @media with nested @page rules</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void MediaWithPageTest()
        {
            const string FileName = @"media2.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));
            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);
            var rules = styleSheetNode.StyleSheetRules;
            Assert.IsNotNull(rules);
            Assert.AreEqual(rules.Count, 5);

            var nestedNode = rules[2] as MediaNode;
            Assert.IsNotNull(nestedNode);
            Assert.AreEqual(nestedNode.MediaQueries.Count, 1);
            Assert.AreEqual(nestedNode.PageNodes.Count, 1);
            Assert.AreEqual(nestedNode.Rulesets.Count, 1);

            var nestedNode2 = rules[3] as MediaNode;
            Assert.AreEqual(nestedNode2.MediaQueries.Count, 1);
            Assert.AreEqual(nestedNode2.PageNodes.Count, 1);
            Assert.AreEqual(nestedNode2.Rulesets.Count, 2);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for @document rules</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void DocumentTest()
        {
            const string FileName = @"document.css";
            var fileInfo = new FileInfo(Path.Combine(ActualDirectory, FileName));
            var styleSheetNode = CssParser.Parse(fileInfo);
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.AreEqual(styleSheetNode.StyleSheetRules.Count, 5);
            var actualNode = styleSheetNode.StyleSheetRules[0] as DocumentQueryNode;
            Assert.IsNotNull(actualNode);
            var expectedName = "url-prefix()";
            Assert.AreEqual(actualNode.MatchFunctionName, expectedName, "expected name '{0}' but was '{1}'", expectedName, actualNode.MatchFunctionName);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for page</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void PageTest()
        {
            const string FileName = @"page1.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.IsTrue(styleSheetNode.StyleSheetRules.Count == 2);

            // @page
            // { 
            // margin: 2cm 
            // }
            var pageNode = styleSheetNode.StyleSheetRules[0] as PageNode;
            Assert.IsNotNull(pageNode);
            var declarations = pageNode.Declarations;
            Assert.IsNotNull(declarations);
            Assert.IsTrue(declarations.Count == 1);
            var declaration = declarations[0];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "margin");
            var expression = declaration.ExprNode;
            Assert.IsNotNull(expression);
            var term = expression.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "2cm");

            // @page :left 
            // {
            // margin-left: 4cm;
            // margin-right: 3cm;
            // }
            pageNode = styleSheetNode.StyleSheetRules[1] as PageNode;
            Assert.IsNotNull(pageNode);
            Assert.IsTrue(pageNode.PseudoPage == ":left");
            declarations = pageNode.Declarations;
            Assert.IsNotNull(declarations);
            Assert.IsTrue(declarations.Count == 2);
            declaration = declarations[0];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "margin-left");
            expression = declaration.ExprNode;
            Assert.IsNotNull(expression);
            term = expression.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "4cm");
            declaration = declarations[1];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "margin-right");
            expression = declaration.ExprNode;
            Assert.IsNotNull(expression);
            term = expression.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "3cm");

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for all selectors</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void SelectorAllTest()
        {
            const string FileName = "selectorall.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for simple selector</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void SelectorElementNameTest()
        {
            const string FileName = "selectorelementname.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.IsTrue(styleSheetNode.StyleSheetRules.Count == 9);

            ////
            //// |div
            ////
            var rulesetNode = styleSheetNode.StyleSheetRules[0] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            var selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            var simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            var typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelector);
            Assert.IsTrue(typeSelector.ElementName == "div");

            var namespacePrefixNode = typeSelector.SelectorNamespacePrefixNode;
            Assert.IsNotNull(namespacePrefixNode);
            Assert.IsTrue(namespacePrefixNode.Prefix == string.Empty);

            var universalSelectorNode = simpleSelectorSequenceNode.UniversalSelectorNode;
            Assert.IsNull(universalSelectorNode);

            var hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsTrue(hashClassAttribPseudoNegationNodes == null || hashClassAttribPseudoNegationNodes.Count == 0);


            ////
            //// ns|div
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[1] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelector);
            Assert.IsTrue(typeSelector.ElementName == "div");

            namespacePrefixNode = typeSelector.SelectorNamespacePrefixNode;
            Assert.IsNotNull(namespacePrefixNode);
            Assert.IsTrue(namespacePrefixNode.Prefix == "ns");

            universalSelectorNode = simpleSelectorSequenceNode.UniversalSelectorNode;
            Assert.IsNull(universalSelectorNode);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsTrue(hashClassAttribPseudoNegationNodes == null || hashClassAttribPseudoNegationNodes.Count == 0);

            ////
            //// *
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[2] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            universalSelectorNode = simpleSelectorSequenceNode.UniversalSelectorNode;
            Assert.IsNotNull(universalSelectorNode);
            Assert.IsNull(universalSelectorNode.SelectorNamespacePrefixNode);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsTrue(hashClassAttribPseudoNegationNodes == null || hashClassAttribPseudoNegationNodes.Count == 0);

            ////
            //// |*
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[3] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            universalSelectorNode = simpleSelectorSequenceNode.UniversalSelectorNode;
            Assert.IsNotNull(universalSelectorNode);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsTrue(hashClassAttribPseudoNegationNodes == null || hashClassAttribPseudoNegationNodes.Count == 0);

            ////
            //// ns|*
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[4] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            universalSelectorNode = simpleSelectorSequenceNode.UniversalSelectorNode;
            Assert.IsNotNull(universalSelectorNode);
            Assert.IsNotNull(universalSelectorNode.SelectorNamespacePrefixNode);
            Assert.IsTrue(universalSelectorNode.SelectorNamespacePrefixNode.Prefix == "ns");

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsTrue(hashClassAttribPseudoNegationNodes == null || hashClassAttribPseudoNegationNodes.Count == 0);

            ////
            //// div
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[5] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelector);
            Assert.IsTrue(typeSelector.ElementName == "div");

            universalSelectorNode = simpleSelectorSequenceNode.UniversalSelectorNode;
            Assert.IsNull(universalSelectorNode);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsTrue(hashClassAttribPseudoNegationNodes == null || hashClassAttribPseudoNegationNodes.Count == 0);

            ////
            //// div #ul .li [ns|someattr="www.msn.com"] ::foo(test1 + test2) :not(bar)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[6] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelector);
            Assert.IsTrue(typeSelector.ElementName == "div");

            universalSelectorNode = simpleSelectorSequenceNode.UniversalSelectorNode;
            Assert.IsNull(universalSelectorNode);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            // hash
            var hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            Assert.IsTrue(hashClassAttribPseudoNegationNode.Hash == "#ul");

            var combinatorSimpleSelectorSequenceNodes = selectorNode.CombinatorSimpleSelectorSequenceNodes;
            Assert.IsNotNull(combinatorSimpleSelectorSequenceNodes);
            Assert.IsTrue(combinatorSimpleSelectorSequenceNodes.Count == 4);

            // class
            var combinatorSimpleSelectorSequenceNode = combinatorSimpleSelectorSequenceNodes[0];
            Assert.IsNotNull(combinatorSimpleSelectorSequenceNode);
            simpleSelectorSequenceNode = combinatorSimpleSelectorSequenceNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);
            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);
            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            Assert.IsTrue(hashClassAttribPseudoNegationNode.CssClass == ".li");

            // attrib
            combinatorSimpleSelectorSequenceNode = combinatorSimpleSelectorSequenceNodes[1];
            Assert.IsNotNull(combinatorSimpleSelectorSequenceNode);
            simpleSelectorSequenceNode = combinatorSimpleSelectorSequenceNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);
            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);
            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            var attribNode = hashClassAttribPseudoNegationNode.AttribNode;
            Assert.IsNotNull(attribNode);
            Assert.IsTrue(attribNode.Ident == "someattr");
            var ns = attribNode.SelectorNamespacePrefixNode;
            Assert.IsNotNull(ns);
            Assert.IsTrue(ns.Prefix == "ns");
            var operatorValue = attribNode.OperatorAndValueNode;
            Assert.IsNotNull(operatorValue);
            Assert.IsTrue(operatorValue.AttribOperatorKind == AttribOperatorKind.Equal);
            Assert.IsTrue(operatorValue.IdentOrString == "\"www.msn.com\"");

            // pseudo
            combinatorSimpleSelectorSequenceNode = combinatorSimpleSelectorSequenceNodes[2];
            Assert.IsNotNull(combinatorSimpleSelectorSequenceNode);
            simpleSelectorSequenceNode = combinatorSimpleSelectorSequenceNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);
            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);
            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            var pseudoNode = hashClassAttribPseudoNegationNode.PseudoNode;
            Assert.IsNotNull(pseudoNode);
            Assert.IsTrue(pseudoNode.NumberOfColons == 2);
            var functionalPseudoNode = pseudoNode.FunctionalPseudoNode;
            Assert.IsNotNull(functionalPseudoNode);
            Assert.IsTrue(functionalPseudoNode.FunctionName == "foo");
            var selectorExpressionNode = functionalPseudoNode.SelectorExpressionNode;
            Assert.IsNotNull(selectorExpressionNode);
            Assert.IsNotNull(selectorExpressionNode.SelectorExpressions);
            Assert.IsTrue(selectorExpressionNode.SelectorExpressions.Count == 3);
            Assert.IsTrue(selectorExpressionNode.SelectorExpressions[0] == "test1");
            Assert.IsTrue(selectorExpressionNode.SelectorExpressions[1] == "+");
            Assert.IsTrue(selectorExpressionNode.SelectorExpressions[2] == "test2");

            // negation
            combinatorSimpleSelectorSequenceNode = combinatorSimpleSelectorSequenceNodes[3];
            Assert.IsNotNull(combinatorSimpleSelectorSequenceNode);
            simpleSelectorSequenceNode = combinatorSimpleSelectorSequenceNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);
            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);
            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            var negationNode = hashClassAttribPseudoNegationNode.NegationNode;
            Assert.IsNotNull(negationNode);
            var negationArgNode = negationNode.NegationArgNode;
            Assert.IsNotNull(negationArgNode);
            var typeSelectorNode = negationArgNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelectorNode);
            Assert.IsTrue(typeSelectorNode.ElementName == "bar");

            ////
            //// html, a , div, span
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[7] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 4);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelector);
            Assert.IsTrue(typeSelector.ElementName == "html");

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[1];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelector);
            Assert.IsTrue(typeSelector.ElementName == "a");

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[2];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelector);
            Assert.IsTrue(typeSelector.ElementName == "div");

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[3];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelector);
            Assert.IsTrue(typeSelector.ElementName == "span");

            ////
            //// a html + div > li
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[8] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelector);
            Assert.IsTrue(typeSelector.ElementName == "a");

            var combinatorSimpleSelectors = selectorNode.CombinatorSimpleSelectorSequenceNodes;
            Assert.IsNotNull(combinatorSimpleSelectors);
            Assert.IsTrue(combinatorSimpleSelectors.Count == 3);

            var combinatorSimpleSelector = combinatorSimpleSelectors[0];
            Assert.IsNotNull(combinatorSimpleSelector);
            Assert.IsTrue(combinatorSimpleSelector.Combinator == Combinator.SingleSpace);
            simpleSelectorSequenceNode = combinatorSimpleSelector.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);
            typeSelectorNode = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelectorNode);
            Assert.IsTrue(typeSelectorNode.ElementName == "html");

            combinatorSimpleSelector = combinatorSimpleSelectors[1];
            Assert.IsNotNull(combinatorSimpleSelector);
            Assert.IsTrue(combinatorSimpleSelector.Combinator == Combinator.PlusSign);
            simpleSelectorSequenceNode = combinatorSimpleSelector.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);
            typeSelectorNode = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelectorNode);
            Assert.IsTrue(typeSelectorNode.ElementName == "div");

            combinatorSimpleSelector = combinatorSimpleSelectors[2];
            Assert.IsNotNull(combinatorSimpleSelector);
            Assert.IsTrue(combinatorSimpleSelector.Combinator == Combinator.GreaterThanSign);
            simpleSelectorSequenceNode = combinatorSimpleSelector.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);
            typeSelectorNode = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelectorNode);
            Assert.IsTrue(typeSelectorNode.ElementName == "li");

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for simple selector with hash</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void SelectorElementNameAndHashTest()
        {
            const string FileName = @"selectorelementnameandhash.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.IsTrue(styleSheetNode.StyleSheetRules.Count == 6);

            ////
            //// html #foo #bar (With Type Selector)
            ////
            var rulesetNode = styleSheetNode.StyleSheetRules[0] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            var selectorsGroupNode = rulesetNode.SelectorsGroupNode;
            Assert.IsNotNull(selectorsGroupNode);

            var selectors = selectorsGroupNode.SelectorNodes;
            Assert.IsNotNull(selectors);
            Assert.IsTrue(selectors.Count == 1);

            var simpleSelectorSequenceNode = selectors[0].SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            var typeSelectorNode = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelectorNode);
            Assert.IsTrue(typeSelectorNode.ElementName == "html");

            var hashClassAttribPseudoNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNodes);
            Assert.IsTrue(hashClassAttribPseudoNodes.Count == 1);

            var hashClassAttribPseudoNegationNode = hashClassAttribPseudoNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            Assert.IsTrue(hashClassAttribPseudoNegationNode.Hash == "#foo");

            var combinatorSimpleSelectorSequenceNodes = selectors[0].CombinatorSimpleSelectorSequenceNodes;
            Assert.IsNotNull(combinatorSimpleSelectorSequenceNodes);
            Assert.IsTrue(combinatorSimpleSelectorSequenceNodes.Count == 1);
            simpleSelectorSequenceNode = combinatorSimpleSelectorSequenceNodes[0].SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);
            hashClassAttribPseudoNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNodes);
            Assert.IsTrue(hashClassAttribPseudoNodes.Count == 1);
            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            Assert.IsTrue(hashClassAttribPseudoNegationNode.Hash == "#bar");

            ////
            //// ns|html #foo1 #bar1 (With Type Selector)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[1] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            selectorsGroupNode = rulesetNode.SelectorsGroupNode;
            Assert.IsNotNull(selectorsGroupNode);

            selectors = selectorsGroupNode.SelectorNodes;
            Assert.IsNotNull(selectors);
            Assert.IsTrue(selectors.Count == 1);

            simpleSelectorSequenceNode = selectors[0].SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelectorNode = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelectorNode);
            Assert.IsTrue(typeSelectorNode.ElementName == "html");

            var namespacePrefixNode = typeSelectorNode.SelectorNamespacePrefixNode;
            Assert.IsNotNull(namespacePrefixNode);
            Assert.IsTrue(namespacePrefixNode.Prefix == "ns");

            hashClassAttribPseudoNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNodes);
            Assert.IsTrue(hashClassAttribPseudoNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            Assert.IsTrue(hashClassAttribPseudoNegationNode.Hash == "#foo1");

            combinatorSimpleSelectorSequenceNodes = selectors[0].CombinatorSimpleSelectorSequenceNodes;
            Assert.IsNotNull(combinatorSimpleSelectorSequenceNodes);
            Assert.IsTrue(combinatorSimpleSelectorSequenceNodes.Count == 1);
            simpleSelectorSequenceNode = combinatorSimpleSelectorSequenceNodes[0].SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);
            hashClassAttribPseudoNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNodes);
            Assert.IsTrue(hashClassAttribPseudoNodes.Count == 1);
            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            Assert.IsTrue(hashClassAttribPseudoNegationNode.Hash == "#bar1");

            ////
            //// * #foo1 (With Universal Selector)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[2] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            selectorsGroupNode = rulesetNode.SelectorsGroupNode;
            Assert.IsNotNull(selectorsGroupNode);

            selectors = selectorsGroupNode.SelectorNodes;
            Assert.IsNotNull(selectors);
            Assert.IsTrue(selectors.Count == 1);

            simpleSelectorSequenceNode = selectors[0].SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            var universalSelectorNode = simpleSelectorSequenceNode.UniversalSelectorNode;
            Assert.IsNotNull(universalSelectorNode);

            namespacePrefixNode = universalSelectorNode.SelectorNamespacePrefixNode;
            Assert.IsNull(namespacePrefixNode);

            hashClassAttribPseudoNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNodes);
            Assert.IsTrue(hashClassAttribPseudoNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            Assert.IsTrue(hashClassAttribPseudoNegationNode.Hash == "#foo1");

            ////
            //// ns|* #foo2 (With Universal Selector)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[3] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            selectorsGroupNode = rulesetNode.SelectorsGroupNode;
            Assert.IsNotNull(selectorsGroupNode);

            selectors = selectorsGroupNode.SelectorNodes;
            Assert.IsNotNull(selectors);
            Assert.IsTrue(selectors.Count == 1);

            simpleSelectorSequenceNode = selectors[0].SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            universalSelectorNode = simpleSelectorSequenceNode.UniversalSelectorNode;
            Assert.IsNotNull(universalSelectorNode);

            namespacePrefixNode = universalSelectorNode.SelectorNamespacePrefixNode;
            Assert.IsNotNull(namespacePrefixNode);
            Assert.IsTrue(namespacePrefixNode.Prefix == "ns");

            hashClassAttribPseudoNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNodes);
            Assert.IsTrue(hashClassAttribPseudoNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            Assert.IsTrue(hashClassAttribPseudoNegationNode.Hash == "#foo2");

            ////
            //// |* #foo3 (With Universal Selector)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[4] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            selectorsGroupNode = rulesetNode.SelectorsGroupNode;
            Assert.IsNotNull(selectorsGroupNode);

            selectors = selectorsGroupNode.SelectorNodes;
            Assert.IsNotNull(selectors);
            Assert.IsTrue(selectors.Count == 1);

            simpleSelectorSequenceNode = selectors[0].SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            universalSelectorNode = simpleSelectorSequenceNode.UniversalSelectorNode;
            Assert.IsNotNull(universalSelectorNode);

            namespacePrefixNode = universalSelectorNode.SelectorNamespacePrefixNode;
            Assert.IsNotNull(namespacePrefixNode);
            Assert.IsTrue(namespacePrefixNode.Prefix == string.Empty);

            hashClassAttribPseudoNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNodes);
            Assert.IsTrue(hashClassAttribPseudoNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            Assert.IsTrue(hashClassAttribPseudoNegationNode.Hash == "#foo3");

            ////
            //// #bar #moo
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[5] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            selectorsGroupNode = rulesetNode.SelectorsGroupNode;
            Assert.IsNotNull(selectorsGroupNode);

            selectors = selectorsGroupNode.SelectorNodes;
            Assert.IsNotNull(selectors);
            Assert.IsTrue(selectors.Count == 1);

            simpleSelectorSequenceNode = selectors[0].SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelectorNode = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelectorNode);

            hashClassAttribPseudoNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNodes);
            Assert.IsTrue(hashClassAttribPseudoNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            Assert.IsTrue(hashClassAttribPseudoNegationNode.Hash == "#bar");

            combinatorSimpleSelectorSequenceNodes = selectors[0].CombinatorSimpleSelectorSequenceNodes;
            Assert.IsNotNull(combinatorSimpleSelectorSequenceNodes);
            Assert.IsTrue(combinatorSimpleSelectorSequenceNodes.Count == 1);
            simpleSelectorSequenceNode = combinatorSimpleSelectorSequenceNodes[0].SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);
            hashClassAttribPseudoNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNodes);
            Assert.IsTrue(hashClassAttribPseudoNodes.Count == 1);
            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            Assert.IsTrue(hashClassAttribPseudoNegationNode.Hash == "#moo");

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for simple selector with class</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void SelectorElementNameAndClassTest()
        {
            const string FileName = @"selectorelementnameandclass.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.IsTrue(styleSheetNode.StyleSheetRules.Count == 2);

            ////
            //// html .foo
            ////
            var rulesetNode = styleSheetNode.StyleSheetRules[0] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            var selectorsGroupNode = rulesetNode.SelectorsGroupNode;
            Assert.IsNotNull(selectorsGroupNode);

            var selectors = selectorsGroupNode.SelectorNodes;
            Assert.IsNotNull(selectors);
            Assert.IsTrue(selectors.Count == 1);

            var simpleSelectorSequenceNode = selectors[0].SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            var typeSelectorNode = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelectorNode);
            Assert.IsTrue(typeSelectorNode.ElementName == "html");

            var hashClassAttribPseudoNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNodes);
            Assert.IsTrue(hashClassAttribPseudoNodes.Count == 1);

            var hashClassAttribPseudoNegationNode = hashClassAttribPseudoNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            Assert.IsTrue(hashClassAttribPseudoNegationNode.CssClass == ".foo");

            ////
            //// .bar
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[1] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            selectorsGroupNode = rulesetNode.SelectorsGroupNode;
            Assert.IsNotNull(selectorsGroupNode);

            selectors = selectorsGroupNode.SelectorNodes;
            Assert.IsNotNull(selectors);
            Assert.IsTrue(selectors.Count == 1);

            simpleSelectorSequenceNode = selectors[0].SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelectorNode = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelectorNode);

            hashClassAttribPseudoNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNodes);
            Assert.IsTrue(hashClassAttribPseudoNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);
            Assert.IsTrue(hashClassAttribPseudoNegationNode.CssClass == ".bar");

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for simple selector with attribute</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void SelectorElementNameAndAttribTest()
        {
            const string FileName = @"selectorelementnameandattrib.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.IsTrue(styleSheetNode.StyleSheetRules.Count == 11);

            ////
            //// [att1]
            ////
            var rulesetNode = styleSheetNode.StyleSheetRules[0] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            var selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            var simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            var typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            var hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            var hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            var attribNode = hashClassAttribPseudoNegationNode.AttribNode;
            Assert.IsNotNull(attribNode);
            Assert.IsTrue(attribNode.Ident == "att1");

            var ns = attribNode.SelectorNamespacePrefixNode;
            Assert.IsNull(ns);

            var operatorValue = attribNode.OperatorAndValueNode;
            Assert.IsTrue(operatorValue == null || (operatorValue.AttribOperatorKind == AttribOperatorKind.None && operatorValue.IdentOrString == string.Empty));

            ////
            //// [ns|att1]
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[1] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            attribNode = hashClassAttribPseudoNegationNode.AttribNode;
            Assert.IsNotNull(attribNode);
            Assert.IsTrue(attribNode.Ident == "att2");

            ns = attribNode.SelectorNamespacePrefixNode;
            Assert.IsNotNull(ns);
            Assert.IsTrue(ns.Prefix == "ns");

            operatorValue = attribNode.OperatorAndValueNode;
            Assert.IsTrue(operatorValue == null || (operatorValue.AttribOperatorKind == AttribOperatorKind.None && operatorValue.IdentOrString == string.Empty));

            ////
            //// [att3 ^= "asdsdfsd4"]
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[2] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            attribNode = hashClassAttribPseudoNegationNode.AttribNode;
            Assert.IsNotNull(attribNode);
            Assert.IsTrue(attribNode.Ident == "att3");

            ns = attribNode.SelectorNamespacePrefixNode;
            Assert.IsNull(ns);

            operatorValue = attribNode.OperatorAndValueNode;
            Assert.IsNotNull(operatorValue);
            Assert.IsTrue(operatorValue.AttribOperatorKind == AttribOperatorKind.Prefix);
            Assert.IsTrue(operatorValue.IdentOrString == "\"asdsdfsd4\"");

            ////
            //// [att4 $= "assfdsd4"]
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[3] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            attribNode = hashClassAttribPseudoNegationNode.AttribNode;
            Assert.IsNotNull(attribNode);
            Assert.IsTrue(attribNode.Ident == "att4");

            ns = attribNode.SelectorNamespacePrefixNode;
            Assert.IsNull(ns);

            operatorValue = attribNode.OperatorAndValueNode;
            Assert.IsNotNull(operatorValue);
            Assert.IsTrue(operatorValue.AttribOperatorKind == AttribOperatorKind.Suffix);
            Assert.IsTrue(operatorValue.IdentOrString == "\"assfdsd4\"");

            ////
            //// [att5 *= "asdfsdfsd4"]
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[4] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            attribNode = hashClassAttribPseudoNegationNode.AttribNode;
            Assert.IsNotNull(attribNode);
            Assert.IsTrue(attribNode.Ident == "att5");

            ns = attribNode.SelectorNamespacePrefixNode;
            Assert.IsNull(ns);

            operatorValue = attribNode.OperatorAndValueNode;
            Assert.IsNotNull(operatorValue);
            Assert.IsTrue(operatorValue.AttribOperatorKind == AttribOperatorKind.Substring);
            Assert.IsTrue(operatorValue.IdentOrString == "\"asdfsdfsd4\"");

            ////
            //// [att6 = "skjdhgfkjdh"]
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[5] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            attribNode = hashClassAttribPseudoNegationNode.AttribNode;
            Assert.IsNotNull(attribNode);
            Assert.IsTrue(attribNode.Ident == "att6");

            ns = attribNode.SelectorNamespacePrefixNode;
            Assert.IsNull(ns);

            operatorValue = attribNode.OperatorAndValueNode;
            Assert.IsNotNull(operatorValue);
            Assert.IsTrue(operatorValue.AttribOperatorKind == AttribOperatorKind.Equal);
            Assert.IsTrue(operatorValue.IdentOrString == "\"skjdhgfkjdh\"");

            ////
            //// [att7 ~= "skjdhgfkjdh"]
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[6] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            attribNode = hashClassAttribPseudoNegationNode.AttribNode;
            Assert.IsNotNull(attribNode);
            Assert.IsTrue(attribNode.Ident == "att7");

            ns = attribNode.SelectorNamespacePrefixNode;
            Assert.IsNull(ns);

            operatorValue = attribNode.OperatorAndValueNode;
            Assert.IsNotNull(operatorValue);
            Assert.IsTrue(operatorValue.AttribOperatorKind == AttribOperatorKind.Includes);
            Assert.IsTrue(operatorValue.IdentOrString == "\"skjdhgfkjdh\"");

            ////
            //// [att8 |= "skjdhgfkjdh"]
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[7] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            attribNode = hashClassAttribPseudoNegationNode.AttribNode;
            Assert.IsNotNull(attribNode);
            Assert.IsTrue(attribNode.Ident == "att8");

            ns = attribNode.SelectorNamespacePrefixNode;
            Assert.IsNull(ns);

            operatorValue = attribNode.OperatorAndValueNode;
            Assert.IsNotNull(operatorValue);
            Assert.IsTrue(operatorValue.AttribOperatorKind == AttribOperatorKind.DashMatch);
            Assert.IsTrue(operatorValue.IdentOrString == "\"skjdhgfkjdh\"");

            ////
            //// [att9 = "abc\
            //// d"]
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[8] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            attribNode = hashClassAttribPseudoNegationNode.AttribNode;
            Assert.IsNotNull(attribNode);
            Assert.IsTrue(attribNode.Ident == "att9");

            ns = attribNode.SelectorNamespacePrefixNode;
            Assert.IsNull(ns);

            operatorValue = attribNode.OperatorAndValueNode;
            Assert.IsNotNull(operatorValue);
            Assert.IsTrue(operatorValue.AttribOperatorKind == AttribOperatorKind.Equal);
            Assert.IsTrue(operatorValue.IdentOrString == "\"abcd\"");

            ////
            //// [att10 = 'abc\
            //// d']
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[9] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            attribNode = hashClassAttribPseudoNegationNode.AttribNode;
            Assert.IsNotNull(attribNode);
            Assert.IsTrue(attribNode.Ident == "att10");

            ns = attribNode.SelectorNamespacePrefixNode;
            Assert.IsNull(ns);

            operatorValue = attribNode.OperatorAndValueNode;
            Assert.IsNotNull(operatorValue);
            Assert.IsTrue(operatorValue.AttribOperatorKind == AttribOperatorKind.Equal);
            Assert.IsTrue(operatorValue.IdentOrString == "'abcd'");

            ////
            //// [att11 = foo]
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[10] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            attribNode = hashClassAttribPseudoNegationNode.AttribNode;
            Assert.IsNotNull(attribNode);
            Assert.IsTrue(attribNode.Ident == "att11");

            ns = attribNode.SelectorNamespacePrefixNode;
            Assert.IsNull(ns);

            operatorValue = attribNode.OperatorAndValueNode;
            Assert.IsNotNull(operatorValue);
            Assert.IsTrue(operatorValue.AttribOperatorKind == AttribOperatorKind.Equal);
            Assert.IsTrue(operatorValue.IdentOrString == "foo");

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for simple selector with negation</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void SelectorElementNameAndNegationTest()
        {
            const string FileName = @"selectorelementnameandnegation.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.IsTrue(styleSheetNode.StyleSheetRules.Count == 6);

            ////
            //// :not(*)
            ////
            var rulesetNode = styleSheetNode.StyleSheetRules[0] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            var selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            var simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            var typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            var hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            var hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            var negationNode = hashClassAttribPseudoNegationNode.NegationNode;
            Assert.IsNotNull(negationNode);

            var negationArgNode = negationNode.NegationArgNode;
            Assert.IsNotNull(negationArgNode);

            var universalSelectorNode = negationArgNode.UniversalSelectorNode;
            Assert.IsNotNull(universalSelectorNode);

            ////
            //// :not(foo)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[1] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            negationNode = hashClassAttribPseudoNegationNode.NegationNode;
            Assert.IsNotNull(negationNode);

            negationArgNode = negationNode.NegationArgNode;
            Assert.IsNotNull(negationArgNode);

            var typeSelectorNode = negationArgNode.TypeSelectorNode;
            Assert.IsNotNull(typeSelectorNode);
            Assert.IsTrue(typeSelectorNode.ElementName == "foo");

            ////
            //// :not(#bar)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[2] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            negationNode = hashClassAttribPseudoNegationNode.NegationNode;
            Assert.IsNotNull(negationNode);

            negationArgNode = negationNode.NegationArgNode;
            Assert.IsNotNull(negationArgNode);
            Assert.IsTrue(negationArgNode.Hash == "#bar");

            ////
            //// :not(.foo1)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[3] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            negationNode = hashClassAttribPseudoNegationNode.NegationNode;
            Assert.IsNotNull(negationNode);

            negationArgNode = negationNode.NegationArgNode;
            Assert.IsNotNull(negationArgNode);
            Assert.IsTrue(negationArgNode.CssClass == ".foo1");

            ////
            //// :not([a])
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[4] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            negationNode = hashClassAttribPseudoNegationNode.NegationNode;
            Assert.IsNotNull(negationNode);

            negationArgNode = negationNode.NegationArgNode;
            Assert.IsNotNull(negationArgNode);

            var attribNode = negationArgNode.AttribNode;
            Assert.IsNotNull(attribNode);
            Assert.IsTrue(attribNode.Ident == "a");

            ////
            //// :not([a])
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[5] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            negationNode = hashClassAttribPseudoNegationNode.NegationNode;
            Assert.IsNotNull(negationNode);

            negationArgNode = negationNode.NegationArgNode;
            Assert.IsNotNull(negationArgNode);

            var pseudoNode = negationArgNode.PseudoNode;
            Assert.IsNotNull(pseudoNode);

            var functionalPseudoNode = pseudoNode.FunctionalPseudoNode;
            Assert.IsNotNull(functionalPseudoNode);
            Assert.IsTrue(functionalPseudoNode.FunctionName == "ps");

            var selectorExpressionNode = functionalPseudoNode.SelectorExpressionNode;
            Assert.IsNotNull(selectorExpressionNode);

            var selectorExpressions = selectorExpressionNode.SelectorExpressions;
            Assert.IsNotNull(selectorExpressions);
            Assert.IsTrue(selectorExpressions.Count == 1);
            Assert.IsTrue(selectorExpressions[0] == "ps1");

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>A test for simple selector with pseudo</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void SelectorElementNameAndPseudoTest()
        {
            const string FileName = @"selectorelementnameandpseudo.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.IsTrue(styleSheetNode.StyleSheetRules.Count == 11);

            ////
            //// :e1
            ////
            var rulesetNode = styleSheetNode.StyleSheetRules[0] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            var selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            var simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            var typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            var hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            var hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            var pseudoNode = hashClassAttribPseudoNegationNode.PseudoNode;
            Assert.IsNotNull(pseudoNode);
            Assert.IsTrue(pseudoNode.NumberOfColons == 1);
            Assert.IsTrue(pseudoNode.Ident == "e1");

            var functionalPseudoNode = pseudoNode.FunctionalPseudoNode;
            Assert.IsNull(functionalPseudoNode);

            ////
            //// ::e2
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[1] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            pseudoNode = hashClassAttribPseudoNegationNode.PseudoNode;
            Assert.IsNotNull(pseudoNode);
            Assert.IsTrue(pseudoNode.NumberOfColons == 2);
            Assert.IsTrue(pseudoNode.Ident == "e2");

            functionalPseudoNode = pseudoNode.FunctionalPseudoNode;
            Assert.IsNull(functionalPseudoNode);

            ////
            //// :e3(a)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[2] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            pseudoNode = hashClassAttribPseudoNegationNode.PseudoNode;
            Assert.IsNotNull(pseudoNode);
            Assert.IsTrue(pseudoNode.NumberOfColons == 1);
            Assert.IsNull(pseudoNode.Ident);

            functionalPseudoNode = pseudoNode.FunctionalPseudoNode;
            Assert.IsNotNull(functionalPseudoNode);
            Assert.IsTrue(functionalPseudoNode.FunctionName == "e3");

            ////
            //// :e4(a+b)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[3] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            pseudoNode = hashClassAttribPseudoNegationNode.PseudoNode;
            Assert.IsNotNull(pseudoNode);
            Assert.IsTrue(pseudoNode.NumberOfColons == 1);
            Assert.IsNull(pseudoNode.Ident);

            functionalPseudoNode = pseudoNode.FunctionalPseudoNode;
            Assert.IsNotNull(functionalPseudoNode);
            Assert.IsTrue(functionalPseudoNode.FunctionName == "e4");

            var selectorExpressionNode = functionalPseudoNode.SelectorExpressionNode;
            Assert.IsNotNull(selectorExpressionNode);

            var selectorExpressions = selectorExpressionNode.SelectorExpressions;
            Assert.IsNotNull(selectorExpressions);
            Assert.IsTrue(selectorExpressions.Count == 3);
            Assert.IsTrue(selectorExpressions[0] == "a");
            Assert.IsTrue(selectorExpressions[1] == "+");
            Assert.IsTrue(selectorExpressions[2] == "b");

            ////
            //// ::e5(a)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[4] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            pseudoNode = hashClassAttribPseudoNegationNode.PseudoNode;
            Assert.IsNotNull(pseudoNode);
            Assert.IsTrue(pseudoNode.NumberOfColons == 2);
            Assert.IsNull(pseudoNode.Ident);

            functionalPseudoNode = pseudoNode.FunctionalPseudoNode;
            Assert.IsNotNull(functionalPseudoNode);
            Assert.IsTrue(functionalPseudoNode.FunctionName == "e5");

            selectorExpressionNode = functionalPseudoNode.SelectorExpressionNode;
            Assert.IsNotNull(selectorExpressionNode);

            selectorExpressions = selectorExpressionNode.SelectorExpressions;
            Assert.IsNotNull(selectorExpressions);
            Assert.IsTrue(selectorExpressions.Count == 1);
            Assert.IsTrue(selectorExpressions[0] == "a");

            ////
            //// ::e6(a+b)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[5] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            pseudoNode = hashClassAttribPseudoNegationNode.PseudoNode;
            Assert.IsNotNull(pseudoNode);
            Assert.IsTrue(pseudoNode.NumberOfColons == 2);
            Assert.IsNull(pseudoNode.Ident);

            functionalPseudoNode = pseudoNode.FunctionalPseudoNode;
            Assert.IsNotNull(functionalPseudoNode);
            Assert.IsTrue(functionalPseudoNode.FunctionName == "e6");

            selectorExpressionNode = functionalPseudoNode.SelectorExpressionNode;
            Assert.IsNotNull(selectorExpressionNode);

            selectorExpressions = selectorExpressionNode.SelectorExpressions;
            Assert.IsNotNull(selectorExpressions);
            Assert.IsTrue(selectorExpressions.Count == 3);
            Assert.IsTrue(selectorExpressions[0] == "a");
            Assert.IsTrue(selectorExpressions[1] == "+");
            Assert.IsTrue(selectorExpressions[2] == "b");

            ////
            //// ::e7(a-b)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[6] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            pseudoNode = hashClassAttribPseudoNegationNode.PseudoNode;
            Assert.IsNotNull(pseudoNode);
            Assert.IsTrue(pseudoNode.NumberOfColons == 2);
            Assert.IsNull(pseudoNode.Ident);

            functionalPseudoNode = pseudoNode.FunctionalPseudoNode;
            Assert.IsNotNull(functionalPseudoNode);
            Assert.IsTrue(functionalPseudoNode.FunctionName == "e7");

            selectorExpressionNode = functionalPseudoNode.SelectorExpressionNode;
            Assert.IsNotNull(selectorExpressionNode);

            selectorExpressions = selectorExpressionNode.SelectorExpressions;
            Assert.IsNotNull(selectorExpressions);
            Assert.IsTrue(selectorExpressions.Count == 1);
            Assert.IsTrue(selectorExpressions[0] == "a-b"); // This is treated as IDENT

            ////
            //// ::e8(1a)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[7] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            pseudoNode = hashClassAttribPseudoNegationNode.PseudoNode;
            Assert.IsNotNull(pseudoNode);
            Assert.IsTrue(pseudoNode.NumberOfColons == 2);
            Assert.IsNull(pseudoNode.Ident);

            functionalPseudoNode = pseudoNode.FunctionalPseudoNode;
            Assert.IsNotNull(functionalPseudoNode);
            Assert.IsTrue(functionalPseudoNode.FunctionName == "e8");

            selectorExpressionNode = functionalPseudoNode.SelectorExpressionNode;
            Assert.IsNotNull(selectorExpressionNode);

            selectorExpressions = selectorExpressionNode.SelectorExpressions;
            Assert.IsNotNull(selectorExpressions);
            Assert.IsTrue(selectorExpressions.Count == 1);
            Assert.IsTrue(selectorExpressions[0] == "1a");

            ////
            //// ::e9(1)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[8] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            pseudoNode = hashClassAttribPseudoNegationNode.PseudoNode;
            Assert.IsNotNull(pseudoNode);
            Assert.IsTrue(pseudoNode.NumberOfColons == 2);
            Assert.IsNull(pseudoNode.Ident);

            functionalPseudoNode = pseudoNode.FunctionalPseudoNode;
            Assert.IsNotNull(functionalPseudoNode);
            Assert.IsTrue(functionalPseudoNode.FunctionName == "e9");

            selectorExpressionNode = functionalPseudoNode.SelectorExpressionNode;
            Assert.IsNotNull(selectorExpressionNode);

            selectorExpressions = selectorExpressionNode.SelectorExpressions;
            Assert.IsNotNull(selectorExpressions);
            Assert.IsTrue(selectorExpressions.Count == 1);
            Assert.IsTrue(selectorExpressions[0] == "1");

            ////
            //// ::e10("a")
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[9] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            pseudoNode = hashClassAttribPseudoNegationNode.PseudoNode;
            Assert.IsNotNull(pseudoNode);
            Assert.IsTrue(pseudoNode.NumberOfColons == 2);
            Assert.IsNull(pseudoNode.Ident);

            functionalPseudoNode = pseudoNode.FunctionalPseudoNode;
            Assert.IsNotNull(functionalPseudoNode);
            Assert.IsTrue(functionalPseudoNode.FunctionName == "e10");

            selectorExpressionNode = functionalPseudoNode.SelectorExpressionNode;
            Assert.IsNotNull(selectorExpressionNode);

            selectorExpressions = selectorExpressionNode.SelectorExpressions;
            Assert.IsNotNull(selectorExpressions);
            Assert.IsTrue(selectorExpressions.Count == 1);
            Assert.IsTrue(selectorExpressions[0] == "\"a\"");

            ////
            //// ::e11(ident)
            ////
            rulesetNode = styleSheetNode.StyleSheetRules[10] as RulesetNode;
            Assert.IsNotNull(rulesetNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode);
            Assert.IsNotNull(rulesetNode.SelectorsGroupNode.SelectorNodes);
            Assert.IsTrue(rulesetNode.SelectorsGroupNode.SelectorNodes.Count == 1);

            selectorNode = rulesetNode.SelectorsGroupNode.SelectorNodes[0];
            Assert.IsNotNull(selectorNode);

            simpleSelectorSequenceNode = selectorNode.SimpleSelectorSequenceNode;
            Assert.IsNotNull(simpleSelectorSequenceNode);

            typeSelector = simpleSelectorSequenceNode.TypeSelectorNode;
            Assert.IsNull(typeSelector);

            hashClassAttribPseudoNegationNodes = simpleSelectorSequenceNode.HashClassAttribPseudoNegationNodes;
            Assert.IsNotNull(hashClassAttribPseudoNegationNodes);
            Assert.IsTrue(hashClassAttribPseudoNegationNodes.Count == 1);

            hashClassAttribPseudoNegationNode = hashClassAttribPseudoNegationNodes[0];
            Assert.IsNotNull(hashClassAttribPseudoNegationNode);

            pseudoNode = hashClassAttribPseudoNegationNode.PseudoNode;
            Assert.IsNotNull(pseudoNode);
            Assert.IsTrue(pseudoNode.NumberOfColons == 2);
            Assert.IsNull(pseudoNode.Ident);

            functionalPseudoNode = pseudoNode.FunctionalPseudoNode;
            Assert.IsNotNull(functionalPseudoNode);
            Assert.IsTrue(functionalPseudoNode.FunctionName == "e11");

            selectorExpressionNode = functionalPseudoNode.SelectorExpressionNode;
            Assert.IsNotNull(selectorExpressionNode);

            selectorExpressions = selectorExpressionNode.SelectorExpressions;
            Assert.IsNotNull(selectorExpressions);
            Assert.IsTrue(selectorExpressions.Count == 1);
            Assert.IsTrue(selectorExpressions[0] == "ident");

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>Declaration with number term test.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void DeclarationWithNumberTermTest()
        {
            const string FileName = @"declarationwithnumberterm.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules[0]);
            Assert.IsTrue(styleSheetNode.StyleSheetRules.Count == 1);

            var rulesetNode = styleSheetNode.StyleSheetRules[0] as RulesetNode;
            Assert.IsNotNull(rulesetNode);

            var declarations = rulesetNode.Declarations;
            Assert.IsNotNull(declarations);
            Assert.IsTrue(declarations.Count == 30);

            // border: 0;
            var declaration = declarations[0];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == string.Empty);
            var expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            var term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "0");

            // border: 1px;
            declaration = declarations[1];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "1px");

            // border: 2%;
            declaration = declarations[2];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "2%");

            // margin: 3cm;;
            declaration = declarations[3];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "margin");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "3cm");

            // margin: 4mm;
            declaration = declarations[4];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "margin");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "4mm");

            // height: 5in;
            declaration = declarations[5];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "height");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "5in");

            // border: 6pt;
            declaration = declarations[6];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "6pt");

            // border: 7pc;
            declaration = declarations[7];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "7pc");

            // margin: 8em;
            declaration = declarations[8];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "margin");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "8em");


            // border: 9ex;
            declaration = declarations[9];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "9ex");

            // border: 10deg;
            declaration = declarations[10];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "10deg");

            // padding: 11rad;
            declaration = declarations[11];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "padding");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "11rad");

            // border: 12grad;
            declaration = declarations[12];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "12grad");

            // border: 13s;
            declaration = declarations[13];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "13s");

            // border: 14ms;
            declaration = declarations[14];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "14ms");

            // border: 15hz;
            declaration = declarations[15];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "15hz");

            // border: 16khz;
            declaration = declarations[16];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "16khz");

            // margin: +17px;
            declaration = declarations[17];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "margin");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "17px");
            Assert.IsTrue(term.UnaryOperator == "+");

            // border: -18px;
            declaration = declarations[18];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "18px");
            Assert.IsTrue(term.UnaryOperator == "-");

            // border: 1.9%;
            declaration = declarations[19];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "1.9%");

            // border: .20%;
            declaration = declarations[20];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == ".20%");

            // border: 0 !important;
            declaration = declarations[21];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == "!important");
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "0");

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>Declaration with multiple number term test.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void DeclarationWithMultipleNumberTermTest()
        {
            const string FileName = @"declarationwithmultiplenumberterm.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules[0]);
            Assert.IsTrue(styleSheetNode.StyleSheetRules.Count == 1);

            var rulesetNode = styleSheetNode.StyleSheetRules[0] as RulesetNode;
            Assert.IsNotNull(rulesetNode);

            var declarations = rulesetNode.Declarations;
            Assert.IsNotNull(declarations);
            Assert.IsTrue(declarations.Count == 2);

            // border: 0 / 0 0 0;
            var declaration = declarations[0];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "border");
            Assert.IsTrue(declaration.Prio == "!important");
            var expr = declaration.ExprNode;
            Assert.IsNotNull(expr);

            var term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "0");

            var termWithOperators = expr.TermsWithOperators;
            Assert.IsNotNull(termWithOperators);
            Assert.IsTrue(termWithOperators.Count == 3);

            var termWithOperator = termWithOperators[0];
            Assert.IsNotNull(termWithOperator);
            Assert.IsTrue(termWithOperator.Operator == "/");
            term = termWithOperator.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "0");

            termWithOperator = termWithOperators[1];
            Assert.IsNotNull(termWithOperator);
            term = termWithOperator.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "0");

            termWithOperator = termWithOperators[2];
            Assert.IsNotNull(termWithOperator);
            term = termWithOperator.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "0");

            // margin: +17px -1px 0 0.4%;
            declaration = declarations[1];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "margin");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);

            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "17px");
            Assert.IsTrue(term.UnaryOperator == "+");

            termWithOperators = expr.TermsWithOperators;
            Assert.IsNotNull(termWithOperators);
            Assert.IsTrue(termWithOperators.Count == 3);

            termWithOperator = termWithOperators[0];
            Assert.IsNotNull(termWithOperator);
            term = termWithOperator.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "1px");
            Assert.IsTrue(term.UnaryOperator == "-");

            termWithOperator = termWithOperators[1];
            Assert.IsNotNull(termWithOperator);
            term = termWithOperator.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "0");

            termWithOperator = termWithOperators[2];
            Assert.IsNotNull(termWithOperator);
            term = termWithOperator.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "0.4%");

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>Declaration with string term test.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void DeclarationWithStringTermTest()
        {
            const string FileName = @"declarationwithstringterm.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules[0]);
            Assert.IsTrue(styleSheetNode.StyleSheetRules.Count == 1);

            var rulesetNode = styleSheetNode.StyleSheetRules[0] as RulesetNode;
            Assert.IsNotNull(rulesetNode);

            var declarations = rulesetNode.Declarations;
            Assert.IsNotNull(declarations);
            Assert.IsTrue(declarations.Count == 5);

            // background-image:url(http://foo.com/bar.gif);
            var declaration = declarations[0];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "background-image");
            Assert.IsTrue(declaration.Prio == string.Empty);
            var expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            var term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.StringBasedValue == "url(http://foo.com/bar.gif)");

            // background:red url("http://foo.com/bar.gif") no-repeat left top;
            declaration = declarations[1];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "background");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.StringBasedValue == "red");

            var termWithOperators = expr.TermsWithOperators;
            Assert.IsNotNull(termWithOperators);
            Assert.IsTrue(termWithOperators.Count == 4);

            var termWithOperator = termWithOperators[0];
            Assert.IsNotNull(termWithOperator);
            term = termWithOperator.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.StringBasedValue == "url(\"http://foo.com/bar.gif\")");

            termWithOperator = termWithOperators[1];
            Assert.IsNotNull(termWithOperator);
            term = termWithOperator.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.StringBasedValue == "no-repeat");

            termWithOperator = termWithOperators[2];
            Assert.IsNotNull(termWithOperator);
            term = termWithOperator.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.StringBasedValue == "left");

            termWithOperator = termWithOperators[3];
            Assert.IsNotNull(termWithOperator);
            term = termWithOperator.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.StringBasedValue == "top");

            // background-image:url('http://foo.com/bar.gif');
            declaration = declarations[2];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "background-image");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.StringBasedValue == "url('http://foo.com/bar.gif')");

            declaration = declarations[3];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "-webkit-appearance");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.StringBasedValue == "none");

            declaration = declarations[4];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "font-family");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.StringBasedValue == "'宋体'");

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>Declaration with hex term test.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void DeclarationWithHexTermTest()
        {
            const string FileName = @"declarationwithhexterm.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules[0]);
            Assert.IsTrue(styleSheetNode.StyleSheetRules.Count == 1);

            var rulesetNode = styleSheetNode.StyleSheetRules[0] as RulesetNode;
            Assert.IsNotNull(rulesetNode);

            var declarations = rulesetNode.Declarations;
            Assert.IsNotNull(declarations);
            Assert.IsTrue(declarations.Count == 2);

            // color: #000;
            var declaration = declarations[0];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "color");
            Assert.IsTrue(declaration.Prio == string.Empty);
            var expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            var term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.Hexcolor == "#000");

            // background-color: #232343
            declaration = declarations[1];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "background-color");
            Assert.IsTrue(declaration.Prio == string.Empty);
            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);
            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.Hexcolor == "#232343");

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>Declaration with func term test.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void DeclarationWithFuncTermTest()
        {
            const string FileName = @"declarationwithfuncterm.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules[0]);
            Assert.IsTrue(styleSheetNode.StyleSheetRules.Count == 2);

            // content: counters(test, ".", lower - roman);
            var rulesetNode = styleSheetNode.StyleSheetRules[0] as RulesetNode;
            Assert.IsNotNull(rulesetNode);

            var declarations = rulesetNode.Declarations;
            Assert.IsNotNull(declarations);
            Assert.IsTrue(declarations.Count == 3);

            var declaration = declarations[0];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "content");
            Assert.IsTrue(declaration.Prio == string.Empty);

            var expr = declaration.ExprNode;
            Assert.IsNotNull(expr);

            var term = expr.TermNode;
            Assert.IsNotNull(term);

            var func = term.FunctionNode;
            Assert.IsNotNull(func);
            Assert.IsTrue(func.FunctionName == "counters");

            expr = func.ExprNode;
            Assert.IsNotNull(expr);

            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.StringBasedValue == "test");

            var termWithOperators = expr.TermsWithOperators;
            Assert.IsNotNull(termWithOperators);
            Assert.IsTrue(termWithOperators.Count == 2);

            var termWithOperator = termWithOperators[0];
            Assert.IsNotNull(termWithOperator);
            Assert.IsTrue(termWithOperator.Operator == ",");

            term = termWithOperator.TermNode;
            Assert.IsTrue(term.StringBasedValue == "\".\"");

            termWithOperator = termWithOperators[1];
            Assert.IsNotNull(termWithOperator);
            Assert.IsTrue(termWithOperator.Operator == ",");

            term = termWithOperator.TermNode;
            Assert.IsTrue(term.StringBasedValue == "lower-roman");

            // color: rgb(255, 0, 0);
            rulesetNode = styleSheetNode.StyleSheetRules[1] as RulesetNode;
            Assert.IsNotNull(rulesetNode);

            declarations = rulesetNode.Declarations;
            Assert.IsNotNull(declarations);
            Assert.IsTrue(declarations.Count == 1);

            declaration = declarations[0];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "color");
            Assert.IsTrue(declaration.Prio == string.Empty);

            expr = declaration.ExprNode;
            Assert.IsNotNull(expr);

            term = expr.TermNode;
            Assert.IsNotNull(term);

            func = term.FunctionNode;
            Assert.IsNotNull(func);
            Assert.IsTrue(func.FunctionName == "rgb");

            expr = func.ExprNode;
            Assert.IsNotNull(expr);

            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "255");

            termWithOperators = expr.TermsWithOperators;
            Assert.IsNotNull(termWithOperators);
            Assert.IsTrue(termWithOperators.Count == 2);

            termWithOperator = termWithOperators[0];
            Assert.IsNotNull(termWithOperator);
            Assert.IsTrue(termWithOperator.Operator == ",");

            term = termWithOperator.TermNode;
            Assert.IsTrue(term.NumberBasedValue == "0");

            termWithOperator = termWithOperators[1];
            Assert.IsNotNull(termWithOperator);
            Assert.IsTrue(termWithOperator.Operator == ",");

            term = termWithOperator.TermNode;
            Assert.IsTrue(term.NumberBasedValue == "0");

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>The ruleset, media, page unordered test.</summary>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void RulesetMediaPageUnorderedTest()
        {
            const string FileName = @"rulesetmediapageunordered.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules[0]);
            Assert.IsTrue(styleSheetNode.StyleSheetRules.Count == 7);
            Assert.IsTrue(styleSheetNode.StyleSheetRules[0] is RulesetNode);
            Assert.IsTrue(styleSheetNode.StyleSheetRules[1] is MediaNode);
            Assert.IsTrue(styleSheetNode.StyleSheetRules[2] is PageNode);
            Assert.IsTrue(styleSheetNode.StyleSheetRules[3] is MediaNode);
            Assert.IsTrue(styleSheetNode.StyleSheetRules[4] is RulesetNode);
            Assert.IsTrue(styleSheetNode.StyleSheetRules[5] is PageNode);
            Assert.IsTrue(styleSheetNode.StyleSheetRules[6] is RulesetNode);

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }

        /// <summary>Test for various new functions.</summary>
        /// <remarks>Note that these are not currently parsed as specific lexical functions, just as generic function nodes</remarks>
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        public void FunctionsTest()
        {
            const string FileName = @"functions.css";
            var styleSheetNode = CssParser.Parse(new FileInfo(Path.Combine(ActualDirectory, FileName)));
            Assert.IsNotNull(styleSheetNode);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules);
            Assert.IsNotNull(styleSheetNode.StyleSheetRules[0]);
            Assert.IsTrue(styleSheetNode.StyleSheetRules.Count == 7);

            var rulesetNode = styleSheetNode.StyleSheetRules[0] as RulesetNode;
            Assert.IsNotNull(rulesetNode);

            var declarations = rulesetNode.Declarations;
            Assert.IsNotNull(declarations);
            Assert.IsTrue(declarations.Count == 4);

            var declaration = declarations[3];
            Assert.IsNotNull(declaration);
            Assert.IsTrue(declaration.Property == "width");

            var expr = declaration.ExprNode;
            Assert.IsNotNull(expr);

            var term = expr.TermNode;
            Assert.IsNotNull(term);

            var func = term.FunctionNode;
            Assert.IsNotNull(func);
            Assert.IsTrue(func.FunctionName == "calc");

            expr = func.ExprNode;
            Assert.IsNotNull(expr);

            term = expr.TermNode;
            Assert.IsNotNull(term);
            Assert.IsTrue(term.NumberBasedValue == "100%");

            var termWithOperators = expr.TermsWithOperators;
            Assert.IsNotNull(termWithOperators);
            Assert.IsTrue(termWithOperators.Count == 5);

            var termWithOperator = termWithOperators[0];
            Assert.IsNotNull(termWithOperator);
            Assert.IsTrue(termWithOperator.Operator == "/");
            term = termWithOperator.TermNode;
            Assert.IsTrue(term.NumberBasedValue == "3");

            termWithOperator = termWithOperators[1];
            Assert.IsNotNull(termWithOperator);
            term = termWithOperator.TermNode;
            Assert.AreEqual(term.UnaryOperator, "-");
            Assert.AreEqual(term.NumberBasedValue, "2");

            termWithOperator = termWithOperators[2];
            Assert.IsTrue(termWithOperator.Operator == "*");
            Assert.AreEqual(termWithOperator.TermNode.NumberBasedValue, "1em");

            termWithOperator = termWithOperators[3];
            Assert.IsTrue(termWithOperator.TermNode.UnaryOperator == "-");
            Assert.AreEqual(termWithOperator.TermNode.NumberBasedValue, "2");

            termWithOperator = termWithOperators[4];
            Assert.IsTrue(termWithOperator.Operator == "*");
            Assert.AreEqual(termWithOperator.TermNode.NumberBasedValue, "1px");

            MinificationVerifier.VerifyMinification(BaseDirectory, FileName);
            PrettyPrintVerifier.VerifyPrettyPrint(BaseDirectory, FileName);
        }
    }
}