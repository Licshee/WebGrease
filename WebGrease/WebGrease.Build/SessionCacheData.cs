// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SessionCacheData.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace WebGrease.Build
{
    using System;
    using System.Collections.Generic;

    /// <summary>The session cache data. Used to store information about the different configurations used in a project.</summary>
    internal class SessionCacheData
    {
        /// <summary>Initializes a new instance of the <see cref="SessionCacheData"/> class.</summary>
        public SessionCacheData()
        {
            this.ConfigTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Gets the configuration types.</summary>
        public IDictionary<string, string> ConfigTypes { get; private set; }

        /// <summary>Sets the unique key for the configuration type. This is used to keep Debug cache around when building Release and the other way around.</summary>
        /// <param name="configType">The config type.</param>
        /// <param name="uniqueCacheSectionKey">The unique cache section key.</param>
        public void SetConfigTypeUniqueKey(string configType, string uniqueCacheSectionKey)
        {
            this.ConfigTypes[configType] = uniqueCacheSectionKey;
        }

        /// <summary>Get the unique key for a configiguration.</summary>
        /// <param name="configType">The config type.</param>
        /// <returns>The unique key as a string.</returns>
        public string GetConfigTypeUniqueKey(string configType)
        {
            string uniqueKey;
            return (this.ConfigTypes.TryGetValue(configType, out uniqueKey))
                ? uniqueKey
                : null;
        }
    }
}