// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MinificationVerifier.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The minification test.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Css.Tests.TestSuite
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using WebGrease.Css;
    using WebGrease.Css.Ast;
    using WebGrease.Css.Extensions;
    using WebGrease.Css.Visitor;

    /// <summary>The minification test.</summary>
    internal static class MinificationVerifier
    {
        /// <summary>The verify minification.</summary>
        /// <param name="baseDirectory">The base directory.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="visitors">The list of optional visitors.</param>
        public static void VerifyMinification(string baseDirectory, string fileName, IList<NodeVisitor> visitors = null)
        {
            Contract.Requires(Directory.Exists(baseDirectory));
            Contract.Requires(File.Exists(fileName));

            // Actual
            var actualCssNode = CssParser.Parse(new FileInfo(Path.Combine(baseDirectory, "Actual", fileName)), false);
            var actualMinifiedCss = ApplyVisitors(actualCssNode, visitors).MinifyPrint();

            // Now minify the minified file again and then compare (covers the reparsing validity)
            
            // Actual again
            actualCssNode = CssParser.Parse(actualMinifiedCss);
            var actualMinifiedCssAgain = ApplyVisitors(actualCssNode, visitors).MinifyPrint();

            if (string.Compare(actualMinifiedCss, actualMinifiedCssAgain, StringComparison.Ordinal) != 0)
            {
                Trace.WriteLine("Actual Css:");
                Trace.WriteLine(actualMinifiedCss);
                Trace.WriteLine("Actual Css (again):");
                Trace.WriteLine(actualMinifiedCssAgain);

                throw new Exception("Comparison failed.");
            }

            // Expect
            var expectMinifiedCss = File.ReadAllText(Path.Combine(baseDirectory, "MinifiedExpect", fileName));

            if (string.Compare(actualMinifiedCss, expectMinifiedCss, StringComparison.Ordinal) != 0)
            {
                Trace.WriteLine("Actual Css:");
                Trace.WriteLine(actualMinifiedCss);
                Trace.WriteLine("Expect Css:");
                Trace.WriteLine(expectMinifiedCss);

                throw new Exception("Comparison failed.");
            }
        }

        /// <summary>Applies the list of optional visitors</summary>
        /// <param name="ast">The ast to visit.</param>
        /// <param name="visitors">The list of visitors.</param>
        /// <returns>The updated ast after applying visitors.</returns>
        internal static AstNode ApplyVisitors(AstNode ast, IList<NodeVisitor> visitors)
        {
            if (ast == null || visitors == null)
            {
                return ast;
            }

            return visitors.Aggregate(ast, (current, visitor) => current.Accept(visitor));
        }
    }
}
