// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActivityName.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace WebGrease.Build
{
    /// <summary>The webgrease activity names.</summary>
    public enum ActivityName
    {
        /// <summary>The bundle activity.</summary>
        Bundle, 

        /// <summary>The everything activity.</summary>
        Everything, 

        /// <summary>The clean destination activity.</summary>
        CleanDestination,

        /// <summary>The clean cache activity.</summary>
        CleanCache
    }
}