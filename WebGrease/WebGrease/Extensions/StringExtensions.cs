// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The string extensions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    using WebGrease.Configuration;
    using WebGrease.Css.Extensions;

    /// <summary>The string extensions.</summary>
    internal static class StringExtensions
    {
        private static readonly JsonSerializerSettings DefaultJsonSerializerSettings = new JsonSerializerSettings();

        private static readonly Lazy<JsonSerializerSettings> JsonSerializerSettings = new Lazy<JsonSerializerSettings>(
            () =>
            {
                var contractResolver = new DefaultContractResolver();
                contractResolver.DefaultMembersSearchFlags |= BindingFlags.NonPublic;
                return new JsonSerializerSettings { ContractResolver = contractResolver };
            });

        /// <summary>The try parse for string to boolean.</summary>
        /// <param name="textToParse">The text to parse.</param>
        /// <returns>The try parse.</returns>
        internal static bool TryParseBool(this string textToParse)
        {
            bool minify;
            return !Boolean.TryParse(textToParse, out minify) || minify;
        }

        /// <summary>
        /// parses text into a number (if valid)
        /// </summary>
        /// <param name="textToParse">text to parse</param>
        /// <returns>the number</returns>
        internal static int TryParseInt32(this string textToParse)
        {
            int temp;
            return Int32.TryParse(textToParse, out temp) ? temp : default(int);
        }

        /// <summary>
        /// Checks if the string is null or empty space.
        /// </summary>
        /// <param name="text">string to test</param>
        /// <returns>true or false</returns>
        internal static bool IsNullOrWhitespace(this string text)
        {
            return String.IsNullOrWhiteSpace(text);
        }

        /// <summary>
        /// Return null if the string is empty or whitespace or null otherwise returns the string.
        /// </summary>
        /// <param name="value">The string</param>
        /// <returns>Null or the string of not empty or whitespace</returns>
        public static string AsNullIfWhiteSpace(this string value)
        {
            return String.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>
        /// Formats the string with the InvariantCulture.
        /// </summary>
        /// <param name="format">The format</param>
        /// <param name="args">The format parameters.</param>
        /// <returns>The formatting string.</returns>
        public static string InvariantFormat(this string format, params object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            return String.Format(CultureInfo.InvariantCulture, format, args);
        }

        internal static string EnsureEndSeperatorChar(this string absolutePath)
        {
            if (!absolutePath.EndsWith(new string(Path.DirectorySeparatorChar, 1), StringComparison.OrdinalIgnoreCase))
            {
                absolutePath = absolutePath + Path.DirectorySeparatorChar;
            }
            return absolutePath;
        }

        internal static string MakeRelativeTo(this string absolutePath, string relativeTo)
        {
            if (String.IsNullOrWhiteSpace(relativeTo))
            {
                return absolutePath;
            }

            return new Uri(relativeTo).MakeRelativeUri(new Uri(absolutePath)).ToString().Replace("/", @"\");
        }

        internal static string MakeRelativeToDirectory(this string absolutePath, string relativeTo)
        {
            relativeTo = relativeTo.EnsureEndSeperatorChar();
            if (String.IsNullOrWhiteSpace(relativeTo))
            {
                return absolutePath;
            }

            return new Uri(relativeTo).MakeRelativeUri(new Uri(absolutePath)).ToString().Replace("/", @"\");
        }

        internal static IEnumerable<string> GetFiles(this IEnumerable<InputSpec> inputs, string rootPath, LogManager log = null)
        {
            return inputs
                .Where(_ => _ != null && !string.IsNullOrWhiteSpace(_.Path))
                .SelectMany(i => i.GetFiles(rootPath, log));
        }

        internal static IEnumerable<string> GetFiles(this InputSpec input, string rootPath, LogManager log = null)
        {
            var files = new List<string>();
            var path = Path.Combine(rootPath, input.Path);
            if (File.Exists(path))
            {
                if (log != null)
                {
                    log.Information("- {0}".InvariantFormat(path));
                }
                files.Add(path);
            }
            else if (Directory.Exists(path))
            {
                if (log != null)
                {
                    log.Information(
                        "Folder: {0}, Pattern: {1}, Options: {2}".InvariantFormat(
                            path, input.SearchPattern, input.SearchOption));
                }

                files.AddRange(
                    Directory.EnumerateFiles(
                        path,
                        string.IsNullOrWhiteSpace(input.SearchPattern)
                            ? "*.*"
                            : input.SearchPattern,
                        input.SearchOption)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase));

                if (log != null)
                {
                    foreach (var file in files)
                    {
                        log.Information("- {0}".InvariantFormat(file));
                    }
                }
            }
            else if (!input.IsOptional)
            {
                throw new FileNotFoundException("Could not find the file for non option input spec: Path:{0}, SearchPattern:{1}, Options:{2}".InvariantFormat(path, input.SearchPattern, input.SearchOption), path);
            }

            return files;
        }

        internal static T FromJson<T>(this string json, bool nonPublic = false)
        {
            return JsonConvert.DeserializeObject<T>(json, GetJsonSerializationSettings(nonPublic));
        }

        internal static string ToJson(this object value, bool nonPublic = false)
        {
            return JsonConvert.SerializeObject(value, Formatting.None, GetJsonSerializationSettings(nonPublic));
        }

        internal static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> range)
        {
            range.ForEach(dictionary.Add);
        }

        private static JsonSerializerSettings GetJsonSerializationSettings(bool nonPublic)
        {
            return nonPublic 
                ? JsonSerializerSettings.Value 
                : DefaultJsonSerializerSettings;
        }
    }
}
