// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActivityName.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace WebGrease.Build
{
    /// <summary>The activity name.</summary>
    internal enum ActivityName
    {
        /// <summary>The bundle.</summary>
        Bundle, 

        /// <summary>The everything.</summary>
        Everything, 

        /// <summary>The clean.</summary>
        CleanDestination,

        /// <summary>The clean.</summary>
        CleanCache
    }
}