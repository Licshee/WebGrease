﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NumberExtensions.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   NumberExtensions Class - Provides the extension for numbers
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.Extensions
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using ImageAssemblyAnalysis;

    /// <summary>NumberExtensions Class - Provides the extension for numbers</summary>
    internal static class NumberExtensions
    {
        /// <summary>
        /// The compiled regular expression for identifying urls
        /// </summary>
        private static readonly Regex NumberWithUnitsRegex = new Regex(@"([+-]?[0-9]*\.?[0-9]+)[a-z]*", RegexOptions.IgnoreCase);

        /// <summary>Returns the unary operator from the int value</summary>
        /// <param name="number">The int number</param>
        /// <returns>The unary value</returns>
        internal static string UnaryOperator(this float? number)
        {
            return number < 0 ? ImageAssembleConstants.Subtract : null;
        }

        /// <summary>Returns the pixels term from the int value</summary>
        /// <param name="number">The int number</param>
        /// <returns>The unary value</returns>
        internal static string Pixels(this float? number)
        {
            if (number == null)
            {
                return null;
            }

            return number.Value == 0 ? ImageAssembleConstants.Zero : string.Format(CultureInfo.CurrentUICulture, ImageAssembleConstants.PxFormat, Math.Ceiling(Math.Abs(number.Value)));
        }

        /// <summary>Parses the float text.</summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The parsed float.</returns>
        internal static float ParseFloat(this string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                float val;
                if (float.TryParse(text, out val))
                {
                    return val;
                }
            }

            return 0;
        }

        /// <summary>Returns the sign from the unary operator</summary>
        /// <param name="unaryOperator">The unary operator</param>
        /// <returns>The signed integer</returns>
        internal static int SignInt(this string unaryOperator)
        {
            if (unaryOperator == ImageAssembleConstants.Subtract)
            {
                return -1;
            }

            return 1;
        }

        /// <summary>Returns true if zero based number is found.</summary>
        /// <param name="numberBasedValue">The number based value.</param>
        /// <returns>True if zero based number is found.</returns>
        internal static bool TryParseZeroBasedNumberValue(this string numberBasedValue)
        {
            if (string.IsNullOrWhiteSpace(numberBasedValue))
            {
                return true;
            }

            var match = NumberWithUnitsRegex.Match(numberBasedValue);
            if (match.Success)
            {
                var numMatch = match.Result("$1");
                float val;
                if (float.TryParse(numMatch, out val))
                {
                    if (Math.Abs(val) == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}