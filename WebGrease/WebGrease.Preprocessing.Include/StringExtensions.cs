// ----------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease.Preprocessing.Include
{
    using System;
    using System.Globalization;

    /// <summary>The string extensions.</summary>
    internal static class StringExtensions
    {
        #region Public Methods and Operators

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
    }
}