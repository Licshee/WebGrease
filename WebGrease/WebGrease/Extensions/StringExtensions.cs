// ----------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------
namespace WebGrease.Extensions
{
    using System;
    using System.Globalization;

    /// <summary>The string extensions.</summary>
    internal static class StringExtensions
    {
        #region Public Methods and Operators

        /// <summary>Return null if the string is empty or whitespace or null otherwise returns the string.</summary>
        /// <param name="value">The string</param>
        /// <returns>Null or the string of not empty or whitespace</returns>
        public static string AsNullIfWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>Formats the string with the InvariantCulture.</summary>
        /// <param name="format">The format</param>
        /// <param name="args">The format parameters.</param>
        /// <returns>The formatting string.</returns>
        public static string InvariantFormat(this string format, params object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

        #endregion

        #region Methods

        /// <summary>Checks if the string is null or empty space.</summary>
        /// <param name="text">string to test</param>
        /// <returns>true or false</returns>
        internal static bool IsNullOrWhitespace(this string text)
        {
            return string.IsNullOrWhiteSpace(text);
        }

        /// <summary>The try parse for string to boolean.</summary>
        /// <param name="textToParse">The text to parse.</param>
        /// <returns>The try parse.</returns>
        internal static bool TryParseBool(this string textToParse)
        {
            bool minify;
            return !bool.TryParse(textToParse, out minify) || minify;
        }

        /// <summary>parses text into a number (if valid)</summary>
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