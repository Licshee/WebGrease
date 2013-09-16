// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TokensTests.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Css.Tests.WebGreaseSpecific
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WebGrease.Tests;

    using WebGrease;
    using WebGrease.Configuration;
    using WebGrease.Css;
    using WebGrease.Css.Ast;
    using WebGrease.Css.Ast.MediaQuery;
    using WebGrease.Css.Extensions;
    using WebGrease.Css.Visitor;

    [TestClass]
    public class TokensTests
    {
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.Tokens)]
        public void ValueTokens1()
        {
            var css = ".body{font-size:%FONTSIZE%}";
            var stylesheet = ParseCss(css);
            var declarationNode = GetFirstDeclaration(stylesheet);
            Assert.AreEqual("%FONTSIZE%", declarationNode.ExprNode.TermNode.ReplacementTokenBasedValue);
            Assert.AreEqual(css, stylesheet.MinifyPrint());

            var replacedCss = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "FONTSIZE", "10px" } } })).MinifyPrint();
            Assert.AreEqual(".body{font-size:10px}", replacedCss);
        }

        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.Tokens)]
        public void ValueTokens2()
        {
            var css = ".body{background-image:url('%someurl%')}";
            var stylesheet = ParseCss(css);

            var declarationNode = GetFirstDeclaration(stylesheet);
            Assert.AreEqual("url('%someurl%')", declarationNode.ExprNode.TermNode.StringBasedValue);
            Assert.AreEqual(css, stylesheet.MinifyPrint());

            var replacedCss = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "someurl", "http://some.url/image.png" } } })).MinifyPrint();
            Assert.AreEqual(".body{background-image:url('http://some.url/image.png')}", replacedCss);

            var replacedCss2 = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "someurl", "hash(http://some.url/image.png)" } } })).MinifyPrint();
            Assert.AreEqual(".body{background-image:url('hash(http://some.url/image.png)')}", replacedCss2);
        }

        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.Tokens)]
        public void ValueTokens3()
        {
            var css = ".body{padding:%PADDINGLEFT% 10px 2px %PADDINGBOTTOM%}";
            var stylesheet = ParseCss(css);
            var declarationNode = GetFirstDeclaration(stylesheet);
            Assert.AreEqual("%PADDINGLEFT%", declarationNode.ExprNode.TermNode.ReplacementTokenBasedValue);
            Assert.AreEqual("%PADDINGBOTTOM%", declarationNode.ExprNode.TermsWithOperators[2].TermNode.ReplacementTokenBasedValue);
            Assert.AreEqual(css, stylesheet.MinifyPrint());

            var replacedCss = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "PADDINGLEFT", "10rem" }, { "PADDINGBOTTOM", "1%" }, } })).MinifyPrint();
            Assert.AreEqual(".body{padding:10rem 10px 2px 1%}", replacedCss);
        }

        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.Tokens)]
        public void ValueTokens4()
        {
            var css = ".body{url:%URL%}";
            var stylesheet = ParseCss(css);
            var declarationNode = GetFirstDeclaration(stylesheet);
            Assert.AreEqual("%URL%", declarationNode.ExprNode.TermNode.ReplacementTokenBasedValue);
            Assert.AreEqual(css, stylesheet.MinifyPrint());

            var replacedCss2 = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "URL", "url('someurl.com')" } } })).MinifyPrint();
            Assert.AreEqual(".body{url:url('someurl.com')}", replacedCss2);
        }

        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.Tokens)]
        public void PropertyTokens1()
        {
            var css = "%SELECTOR%{%PROPERTYTOKEN%:%VALUETOKEN%}";
            var stylesheet = ParseCss(css);
            var declarationNode = GetFirstDeclaration(stylesheet);
            Assert.AreEqual("%PROPERTYTOKEN%", declarationNode.Property);
            Assert.AreEqual(css, stylesheet.MinifyPrint());

            var replacedCss2 = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "PROPERTYTOKEN", "font-size" }, { "VALUETOKEN", "14px" }, { "SELECTOR", "body.none" } } })).MinifyPrint();
            Assert.AreEqual("body.none{font-size:14px}", replacedCss2);
        }

        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.Tokens)]
        public void SelectorTokens1a()
        {
            var css = "body %TOKEN% .class{}";
            var stylesheet = ParseCss(css);
            Assert.AreEqual(css, stylesheet.MinifyPrint());

            var replacedCss = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "TOKEN", "replaced" } } })).MinifyPrint();
            Assert.AreEqual("body replaced .class{}", replacedCss);

            var replacedCss2 = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "TOKEN", ".replaced" } } })).MinifyPrint();
            Assert.AreEqual("body .replaced .class{}", replacedCss2);

            var replacedCss3 = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "TOKEN", "#replaced" } } })).MinifyPrint();
            Assert.AreEqual("body #replaced .class{}", replacedCss3);
        }

        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.Tokens)]
        public void SelectorTokens1b()
        {
            var css = "body%TOKEN% .class{}";
            var stylesheet = ParseCss(css);
            Assert.AreEqual(css, stylesheet.MinifyPrint());

            var replacedCss = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "TOKEN", "replaced" } } })).MinifyPrint();
            Assert.AreEqual("bodyreplaced .class{}", replacedCss);

            var replacedCss2 = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "TOKEN", ".replaced" } } })).MinifyPrint();
            Assert.AreEqual("body.replaced .class{}", replacedCss2);

            var replacedCss3 = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "TOKEN", "#replaced" } } })).MinifyPrint();
            Assert.AreEqual("body#replaced .class{}", replacedCss3);
        }

        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.Tokens)]
        public void SelectorTokens2()
        {
            var css = "%TOKEN%>div{}";
            var stylesheet = ParseCss(css);
            Assert.AreEqual(css, stylesheet.MinifyPrint());

            var replacedCss = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "TOKEN", "replaced" } } })).MinifyPrint();
            Assert.AreEqual("replaced>div{}", replacedCss);

            var replacedCss2 = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "TOKEN", ".replaced" } } })).MinifyPrint();
            Assert.AreEqual(".replaced>div{}", replacedCss2);

            var replacedCss3 = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "TOKEN", "#replaced" } } })).MinifyPrint();
            Assert.AreEqual("#replaced>div{}", replacedCss3);
        }

        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.Tokens)]
        public void SelectorTokens3()
        {
            var css = ".%TOKEN%>div{}";
            var stylesheet = ParseCss(css);

            Assert.AreEqual(css, stylesheet.MinifyPrint());
            var replacedCss = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "TOKEN", "replaced" } } })).MinifyPrint();
            Assert.AreEqual(".replaced>div{}", replacedCss);
        }

        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.Tokens)]
        public void SelectorTokens4()
        {
            var css = "body#%TOKEN%>div{}";
            var stylesheet = ParseCss(css);
            Assert.AreEqual(css, stylesheet.MinifyPrint());

            var replacedCss = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "TOKEN", "replaced" } } })).MinifyPrint();
            Assert.AreEqual("body#replaced>div{}", replacedCss);
        }
        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.Tokens)]
        public void SelectorTokens5()
        {
            var css = "body#%TOKEN% .hello{}";
            var stylesheet = ParseCss(css);
            Assert.AreEqual(css, stylesheet.MinifyPrint());

            var replacedCss = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "TOKEN", "replaced" } } })).MinifyPrint();
            Assert.AreEqual("body#replaced .hello{}", replacedCss);
        }

        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.Tokens)]
        public void MediaQueryTokens1()
        {
            var css = "@media screen and (max-width:%VALUE%){}";
            var stylesheet = ParseCss(css);
            var styleSheetRuleNode = stylesheet.StyleSheetRules.FirstOrDefault() as MediaNode;
            Assert.IsNotNull(styleSheetRuleNode);
            Assert.AreEqual(css, stylesheet.MinifyPrint());

            var replacedCss = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "VALUE", "128px" } } })).MinifyPrint();
            Assert.AreEqual("@media screen and (max-width:128px){}", replacedCss);
        }

        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.Tokens)]
        public void MediaQueryTokens2()
        {
            var css = "@media screen and (%PROPERTY%:%VALUE%){}";
            var stylesheet = ParseCss(css);
            var styleSheetRuleNode = stylesheet.StyleSheetRules.FirstOrDefault() as MediaNode;
            Assert.IsNotNull(styleSheetRuleNode);
            Assert.AreEqual(css, stylesheet.MinifyPrint());

            var replacedCss = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "VALUE", "128px" }, { "PROPERTY", "min-width" } } })).MinifyPrint();
            Assert.AreEqual("@media screen and (min-width:128px){}", replacedCss);
        }

        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.Tokens)]
        public void ValueTokensFallback()
        {
            var css = ".body{font-size:%FONTSIZE:size%}";
            var stylesheet = ParseCss(css);
            var declarationNode = GetFirstDeclaration(stylesheet);
            Assert.AreEqual("%FONTSIZE:size%", declarationNode.ExprNode.TermNode.ReplacementTokenBasedValue);
            Assert.AreEqual(css, stylesheet.MinifyPrint());

            var replacedCss = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "FONTSIZE", "10px" } } })).MinifyPrint();
            Assert.AreEqual(".body{font-size:10px}", replacedCss);
        }

        [TestMethod]
        [TestCategory(TestCategories.CssParser)]
        [TestCategory(TestCategories.Tokens)]
        public void ValueTokensMissingFallback()
        {
            var css = ".body{font-size:%FONTSIZE:size%;color:%foo.bar:color%;width:%notype:%}";
            var stylesheet = ParseCss(css);
            var declarationNode = GetFirstDeclaration(stylesheet);
            Assert.AreEqual("%FONTSIZE:size%", declarationNode.ExprNode.TermNode.ReplacementTokenBasedValue);

            declarationNode = GetNthDeclaration(stylesheet, 1);
            Assert.AreEqual("%foo.bar:color%", declarationNode.ExprNode.TermNode.ReplacementTokenBasedValue);

            declarationNode = GetNthDeclaration(stylesheet, 2);
            Assert.AreEqual("%notype:%", declarationNode.ExprNode.TermNode.ReplacementTokenBasedValue);

            Assert.AreEqual(css, stylesheet.MinifyPrint());

            // NONE of the tokens should match any of the replacement strings, so they should pass through to the output as-is
            var replacedCss = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "ed", "sewer" } } })).MinifyPrint();
            Assert.AreEqual(css, replacedCss);

            // SOME replacement strings should match the token names (less the fallback type) and get replaced
            replacedCss = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "FONTSIZE", "10px" }, { "notype", "80px" } } })).MinifyPrint();
            Assert.AreEqual(".body{font-size:10px;color:%foo.bar:color%;width:80px}", replacedCss);

            // ALL replacement strings should match the token names (less the fallback type) and get replaced
            replacedCss = stylesheet.Accept(new ResourceResolutionVisitor(new[] { new Dictionary<string, string> { { "FONTSIZE", "10px" }, { "foo.bar", "red" }, { "notype", "80px" } } })).MinifyPrint();
            Assert.AreEqual(".body{font-size:10px;color:red;width:80px}", replacedCss);
        }

        private static DeclarationNode GetFirstDeclaration(StyleSheetNode stylesheet)
        {
            var styleSheetRuleNode = GetFirstRuleSet(stylesheet);
            var declarationNode = styleSheetRuleNode.Declarations.FirstOrDefault();
            Assert.IsNotNull(declarationNode);
            return declarationNode;
        }

        private static DeclarationNode GetNthDeclaration(StyleSheetNode stylesheet, int index)
        {
            var styleSheetRuleNode = GetFirstRuleSet(stylesheet);
            var declarationNode = styleSheetRuleNode.Declarations[index];
            Assert.IsNotNull(declarationNode);
            return declarationNode;
        }

        private static RulesetNode GetFirstRuleSet(StyleSheetNode stylesheet)
        {
            var styleSheetRuleNode = stylesheet.StyleSheetRules.FirstOrDefault() as RulesetNode;
            Assert.IsNotNull(styleSheetRuleNode);
            return styleSheetRuleNode;
        }

        private static StyleSheetNode ParseCss(string css)
        {
            return CssParser.Parse(new WebGreaseContext(new WebGreaseConfiguration()), css);
        }
    }
}
