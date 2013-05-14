// ----------------------------------------------------------------------------------------------------
// <copyright file="DictionaryExtensions.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// <summary>
//   
// </summary>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease.Extensions
{
    using System.Collections.Generic;

    using WebGrease.Css.Extensions;

    /// <summary>Enumerable extensions.</summary>
    internal static class DictionaryExtensions
    {
        #region Methods

        /// <summary>Adds a dictionary to another dictionary with the same types.</summary>
        /// <param name="dictionary">The dictionary to add to.</param>
        /// <param name="range">The dictionary to add.</param>
        /// <typeparam name="TKey">The type of Key</typeparam>
        /// <typeparam name="TValue">The type of Value</typeparam>
        internal static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> range)
        {
            range.ForEach(dictionary.Add);
        }

        #endregion

        /// <summary>Adds dictionary values of one to another dictionary with int values.</summary>
        /// <param name="dictionary1">The dictionary 1.</param>
        /// <param name="dictionary2">The dictionary 2.</param>
        /// <typeparam name="TKey">The type of Key</typeparam>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Extension method")]
        internal static void Add<TKey>(this IDictionary<TKey, double> dictionary1, IEnumerable<KeyValuePair<TKey, double>> dictionary2)
        {
            foreach (var kvp2 in dictionary2)
            {
                var key = kvp2.Key;
                if (!dictionary1.ContainsKey(key))
                {
                    dictionary1[key] = 0;
                }

                dictionary1[key] += kvp2.Value;
            } 
        }

        /// <summary>Adds dictionary values of one to another dictionary with double values.</summary>
        /// <param name="dictionary1">The dictionary 1.</param>
        /// <param name="dictionary2">The dictionary 2.</param>
        /// <typeparam name="TKey">The type of Key</typeparam>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Extension method")]
        internal static void Add<TKey>(this IDictionary<TKey, int> dictionary1, IEnumerable<KeyValuePair<TKey, int>> dictionary2)
        {
            foreach (var kvp2 in dictionary2)
            {
                var key = kvp2.Key;
                if (!dictionary1.ContainsKey(key))
                {
                    dictionary1[key] = 0;
                }

                dictionary1[key] += kvp2.Value;
            }
        }
    }
}