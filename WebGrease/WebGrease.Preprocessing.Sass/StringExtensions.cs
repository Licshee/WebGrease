// ----------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease.Preprocessing.Sass
{
    using System;
    using System.Globalization;

    /// <summary>The string extensions.</summary>
    internal static class StringExtensions
    {
        #region Public Methods and Operators

        /// <summary>
        /// Return null if the string is empty or whitespace or null otherwise returns the string.
        /// </summary>
        /// <param name="value">The string</param>
        /// <returns>Null or the string of not empty or whitespace</returns>
        internal static string AsNullIfWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>
        /// Formats the string with the InvariantCulture.
        /// </summary>
        /// <param name="format">The format</param>
        /// <param name="args">The format parameters.</param>
        /// <returns>The formatting string.</returns>
        internal static string InvariantFormat(this string format, params object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

        #endregion

        #region Methods

        /// <summary>
        /// parses text into a number (if valid)
        /// </summary>
        /// <param name="textToParse">text to parse</param>
        /// <returns>the number</returns>
        internal static int TryParseInt32(this string textToParse)
        {
            int temp;
            return int.TryParse(textToParse, out temp) ? temp : default(int);
        }

        #endregion
    }
}