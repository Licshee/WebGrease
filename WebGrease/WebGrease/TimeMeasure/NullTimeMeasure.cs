// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullTimeMeasure.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease
{
    using System.Collections.Generic;

    /// <summary>
    /// The null time measure class, used when measure is is disabled through configuration.
    /// </summary>
    internal class NullTimeMeasure : ITimeMeasure
    {
        /// <summary>
        /// The empty result
        /// </summary>
        private readonly IEnumerable<TimeMeasureResult> emptyResult = new TimeMeasureResult[] { };

        /// <summary>Gets the results.</summary>
        public IEnumerable<TimeMeasureResult> Results
        {
            get
            {
                return this.emptyResult;
            }
        }

        /// <summary>The end.</summary>
        /// <param name="idParts">The names.</param>
        public void End(params string[] idParts)
        {
        }

        /// <summary>The start.</summary>
        /// <param name="idParts">The names.</param>
        public void Start(params string[] idParts)
        {
        }
    }
}