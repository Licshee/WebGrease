// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SessionCacheData.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace WebGrease.Build
{
    using System.Collections.Generic;

    /// <summary>The session cache data.</summary>
    internal class SessionCacheData
    {
        /// <summary>Initializes a new instance of the <see cref="SessionCacheData"/> class.</summary>
        public SessionCacheData()
        {
            this.ConfigTypes = new Dictionary<string, string>();
        }

        /// <summary>Gets the config types.</summary>
        public IDictionary<string, string> ConfigTypes { get; private set; }

        /// <summary>The add.</summary>
        /// <param name="configType">The config type.</param>
        /// <param name="uniqueCacheSectionKey">The unique cache section key.</param>
        public void AddConfigType(string configType, string uniqueCacheSectionKey)
        {
            this.ConfigTypes[configType] = uniqueCacheSectionKey;
        }

        /// <summary>The get unique key.</summary>
        /// <param name="configType">The config type.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public string GetUniqueKey(string configType)
        {
            return this.ConfigTypes.ContainsKey(configType) 
                ? this.ConfigTypes[configType] 
                : null;
        }
    }
}