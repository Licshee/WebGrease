// -----------------------------------------------------------------------
// <copyright file="Minifier.cs" company="Microsoft">
// Copyright Microsoft Corporation, all rights reserved
// </copyright>
// -----------------------------------------------------------------------

namespace WebGrease
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Css;
    using WebGrease.Activities;

    /// <summary>
    /// Minifier for css.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Minifier", Justification = "Spelt as desired")]
    public class CssMinifier
    {
        /// <summary>
        /// Gets or sets the activity to use.
        /// </summary>
        private MinifyCssActivity CssActivity { get; set; }

        /// <summary>
        /// List of errors causing the minification to fail, if any.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification="This is not performance critical, and needed for the AddRange method.")]
        public List<string> Errors { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the css content should be minified.
        /// True by default, but set to false to create a "pretty print" version instead.
        /// </summary>
        public bool ShouldMinify { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CssMinifier"/> class with default settings.
        /// </summary>
        public CssMinifier()
        {
            this.CssActivity = new MinifyCssActivity() {
                ShouldMinify = true, 
                ShouldOptimize = true,
                ShouldValidateForLowerCase = false,
                ShouldExcludeProperties = false,
                ShouldAssembleBackgroundImages = false
            };
            this.ShouldMinify = true;
            this.Errors = new List<string>();
        }

        /// <summary>
        /// Minifies the given css content.
        /// </summary>
        /// <param name="cssContent">Css to be minified.</param>
        /// <returns>The minifed css. If there are errors, this will be empty.</returns>
        public string Minify(string cssContent)
        {
            this.CssActivity.ShouldMinify = this.ShouldMinify;
            var cssOutput = this.CssActivity.Execute(cssContent, false);

            // we only expect this if the CssParser bombed on the css, and it should always throw an AggregateEx with it's errors inside
            var ex = this.CssActivity.ParserException;
            if (ex != null)
            {
                // try to take the friendliest bits of the errors
                var aggEx = ex as AggregateException;
                if (aggEx != null)
                {
                   this.Errors.AddRange(ErrorHelper.DedupeCSSErrors(aggEx));
                }
            }

            return cssOutput;
        }
    }
}
