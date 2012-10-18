// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringPathExtensions.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   StringPathExtensions Class - Provides the extension on string types which deal with paths
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>StringPathExtensions Class - Provides the extension on string types which deal with paths</summary>
    public static class StringPathExtensions
    {
        /// <summary>Generates the relative path in form "..\".</summary>
        /// <param name="pathToConvert">The path to convert</param>
        /// <param name="pathToConvertFrom">The base path</param>
        /// <param name="separators">The directory separators</param>
        /// <returns>Returns a path for the second file that is relative to the first in lower case</returns>
        public static string MakeRelativeTo(this string pathToConvert, string pathToConvertFrom, params char[] separators)
        {
            if (string.IsNullOrWhiteSpace(pathToConvert))
            {
                throw new ArgumentNullException("pathToConvert");
            }

            if (string.IsNullOrWhiteSpace(pathToConvertFrom))
            {
                throw new ArgumentNullException("pathToConvertFrom");
            }

            var inputDirectorySeparator = Path.DirectorySeparatorChar;
            var outputDirectorySeparator = Path.AltDirectorySeparatorChar;

            if (separators != null && separators.Length == 2)
            {
                inputDirectorySeparator = separators[0];
                outputDirectorySeparator = separators[1];
            }

            var pathToConvertTokens = pathToConvert.Split(new[] { inputDirectorySeparator });
            var pathToConvertFromTokens = pathToConvertFrom.Split(new[] { inputDirectorySeparator });

            if (((pathToConvertFromTokens.Length == 0) || (pathToConvertTokens.Length == 0))
                || (pathToConvertFromTokens[0] != pathToConvertTokens[0]))
            {
                return pathToConvert;
            }

            // Index 0 is already verified above
            var index = 1;
            while (index < pathToConvertFromTokens.Length && index < pathToConvertTokens.Length)
            {
                if (pathToConvertFromTokens[index] != pathToConvertTokens[index])
                {
                    break;
                }

                index++;
            }

            var builder = new StringBuilder();
            for (var count = index; count < pathToConvertFromTokens.Length - 1; count++)
            {
                builder.Append(CssConstants.DoubleDot);
                builder.Append(outputDirectorySeparator);
            }

            for (var count = index; count < pathToConvertTokens.Length; count++)
            {
                builder.Append(pathToConvertTokens[count]);
                if (count < (pathToConvertTokens.Length - 1))
                {
                    builder.Append(outputDirectorySeparator);
                }
            }

            return builder.ToString().ToLower(CultureInfo.CurrentUICulture);
        }

        /// <summary>Returns the full path in lower case</summary>
        /// <param name="pathToConvert">The path to convert</param>
        /// <param name="pathToConvertFrom">The base path</param>
        /// <returns>The full path in lower case</returns>
        public static string MakeAbsoluteTo(this string pathToConvert, string pathToConvertFrom)
        {
            if (string.IsNullOrWhiteSpace(pathToConvert) ||
                string.IsNullOrWhiteSpace(pathToConvertFrom))
            {
                return pathToConvert;
            }

            return Path.Combine(Path.GetDirectoryName(pathToConvertFrom), pathToConvert).GetFullPathWithLowercase();
        }

        /// <summary>Returns the full path in lower case</summary>
        /// <param name="originalPath">The original path</param>
        /// <returns>The full path in lower case</returns>
        public static string GetFullPathWithLowercase(this string originalPath)
        {
            return string.IsNullOrWhiteSpace(originalPath) ? originalPath : Path.GetFullPath(originalPath).ToLower(CultureInfo.CurrentUICulture);
        }

        /// <summary>Combines path1 and path2</summary>
        /// <param name="path1">The path1 to combine</param>
        /// <param name="path2">The path2 to combine</param>
        /// <returns>The combined path</returns>
        public static string CombinePath(this string path1, string path2)
        {
            if (string.IsNullOrWhiteSpace(path1))
            {
                path1 = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(path2))
            {
                path2 = string.Empty;
            }

            return Path.Combine(path1, path2);
        }
    }
}