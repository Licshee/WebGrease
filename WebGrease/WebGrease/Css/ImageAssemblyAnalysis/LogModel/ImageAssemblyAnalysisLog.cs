// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImageAssemblyAnalysisLog.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Represents the css analysis log generated from css which is analyzed
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.ImageAssemblyAnalysis.LogModel
{
    using System.Collections.Generic;

    /// <summary>Represents the image log generated from css which is analyzed</summary>
    public class ImageAssemblyAnalysisLog
    {
        /// <summary>
        /// The message for no px
        /// </summary>
        private const string PxMessage = "No declaration with absolute vertical position found.";

        /// <summary>
        /// The message for no url
        /// </summary>
        private const string NoUrlMessage = "No declaration with background url.";

        /// <summary>
        /// The message for no repeat
        /// </summary>
        private const string NoRepeatMessage = "No declaration with background 'no-repeat'.";

        /// <summary>
        /// The message for ignore url
        /// </summary>
        private const string IgnoreUrlMessage = "The image url is configured to ignore in locale resx file.";

        /// <summary>
        /// The log nodes which maintains the list of criteria
        /// </summary>
        private readonly List<ImageAssemblyAnalysis> logNodes = new List<ImageAssemblyAnalysis>();

        /// <summary>Provides the interface to add the log node</summary>
        /// <param name="logNode">The log node</param>
        public void Add(ImageAssemblyAnalysis logNode)
        {
            if (logNode != null)
            {
                this.logNodes.Add(logNode);
            }
        }

        public static string GetFailureMessage(ImageAssemblyAnalysis analysis)
        {
            switch (analysis.FailureReason)
            {
                case FailureReason.IncorrectPosition:
                    return PxMessage;
                case FailureReason.NoUrl:
                    return NoUrlMessage;
                case FailureReason.NoRepeat:
                    return NoRepeatMessage;
                case FailureReason.IgnoreUrl:
                    return IgnoreUrlMessage;
                default:
                    return "Unkniown failure reason";
            }
        }
    }
}