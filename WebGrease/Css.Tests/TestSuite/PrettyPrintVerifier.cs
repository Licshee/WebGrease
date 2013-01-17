// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrettyPrintVerifier.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The pretty print verifier test.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Css.Tests.TestSuite
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.IO;
    using WebGrease.Css;
    using WebGrease.Css.Extensions;
    using WebGrease.Css.Visitor;

    /// <summary>The pretty print verifier test.</summary>
    internal static class PrettyPrintVerifier
    {
        /// <summary>The verify minification.</summary>
        /// <param name="baseDirectory">The base directory.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="visitors">The list of optional visitors.</param>
        public static void VerifyPrettyPrint(string baseDirectory, string fileName, IList<NodeVisitor> visitors = null)
        {
            Contract.Requires(Directory.Exists(baseDirectory));
            Contract.Requires(File.Exists(fileName));

            // Actual
            var actualCssNode = CssParser.Parse(new FileInfo(Path.Combine(baseDirectory, "Actual", fileName)), false);
            var actualMinifiedCss = MinificationVerifier.ApplyVisitors(actualCssNode, visitors).PrettyPrint();

            // Expect
            var expectMinifiedCss = File.ReadAllText(Path.Combine(baseDirectory, "PrettyExpect", fileName));

            if (string.Compare(actualMinifiedCss, expectMinifiedCss, StringComparison.Ordinal) != 0)
            {
                Trace.WriteLine("Actual Css:");
                Trace.WriteLine(actualMinifiedCss);
                Trace.WriteLine("Expect Css:");
                Trace.WriteLine(expectMinifiedCss);

                throw new Exception("Pretty-Print Comparison failed.");
            }
        }
    }
}
