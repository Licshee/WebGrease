// ----------------------------------------------------------------------------------------------------
// <copyright file="ContentPivot.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System.Linq;

    using WebGrease.Extensions;

    /// <summary>The content pivot.</summary>
    public class ContentPivot
    {
        /// <summary>Initializes a new instance of the <see cref="ContentPivot"/> class.</summary>
        /// <param name="locale">The locale.</param>
        /// <param name="theme">The theme.</param>
        public ContentPivot(string locale = null, string theme = null)
        {
            this.Locale = locale;
            this.Theme = theme;
        }

        #region Public Properties

        /// <summary>Gets the locale.</summary>
        public string Locale { get; private set; }

        /// <summary>Gets the theme.</summary>
        public string Theme { get; private set; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return "[{0}]".InvariantFormat(string.Join(",", new[] { this.Locale, this.Theme }.Where(i => !i.IsNullOrWhitespace())));
        }

        #endregion
    }
}