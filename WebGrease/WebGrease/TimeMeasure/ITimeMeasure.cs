// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITimeMeasure.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease
{
    using System.Collections.Generic;

    public interface ITimeMeasure
    {
        /// <summary>Gets the results.</summary>
        IEnumerable<TimeMeasureResult> Results { get; }

        /// <summary>The end.</summary>
        /// <param name="idParts">The names.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "End", Justification = "Works well")]
        void End(params string[] idParts);

        /// <summary>The start.</summary>
        /// <param name="idParts">The names.</param>
        void Start(params string[] idParts);
    }
}