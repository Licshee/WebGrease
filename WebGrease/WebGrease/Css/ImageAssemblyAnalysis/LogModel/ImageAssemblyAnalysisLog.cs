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
    using System.Xml.Linq;
    using Common;
    using Extensions;

    /// <summary>Represents the image log generated from css which is analyzed</summary>
    public class ImageAssemblyAnalysisLog
    {
        /// <summary>
        /// The scan results element
        /// </summary>
        private const string ScanResultsElement = "ScanResults";

        /// <summary>
        /// The scan result element
        /// </summary>
        private const string ScanResultElement = "ScanResult";

        /// <summary>
        /// The assemble element
        /// </summary>
        private const string AssembleElement = "Assemble";

        /// <summary>
        /// The ruleset element
        /// </summary>
        private const string RulesetElement = "Ruleset";

        /// <summary>
        /// The comments element
        /// </summary>
        private const string CommentsElement = "Comments";

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

        /// <summary>Clears the log nodes</summary>
        public void Clear()
        {
            this.logNodes.Clear();
        }

        /// <summary>Saves the log to hard drive</summary>
        /// <param name="path">The log path</param>
        public void Save(string path)
        {
            if (this.logNodes.Count <= 0)
            {
                return;
            }

            FileHelper.WriteFile(path, this.ToString());
        }

        /// <summary>Override the tostring implementation.</summary>
        /// <returns>The string representation of the log file.</returns>
        public override string ToString()
        {
            if (this.logNodes.Count <= 0)
            {
                return base.ToString();
            }

            var rootElement = new XElement(ScanResultsElement);
            this.logNodes.ForEach(logNode => rootElement.Add(new XElement(ScanResultElement, new XElement(AssembleElement, logNode.FailureReason == null ? bool.TrueString : bool.FalseString), Comments(logNode), new XElement(RulesetElement, new XCData(logNode.AstNode.PrettyPrint())))));
            return rootElement.ToString();
        }

        /// <summary>Provide the comments for analysis failure</summary>
        /// <param name="analysis">The analysis node</param>
        /// <returns>The comments element (optional)</returns>
        private static XElement Comments(ImageAssemblyAnalysis analysis)
        {
            if (analysis.FailureReason == null)
            {
                return null;
            }

            var root = new XElement(CommentsElement);

            switch (analysis.FailureReason)
            {
                case FailureReason.IncorrectPosition:
                    root.Value = PxMessage;
                    break;
                case FailureReason.NoUrl:
                    root.Value = NoUrlMessage;
                    break;
                case FailureReason.NoRepeat:
                    root.Value = NoRepeatMessage;
                    break;
                case FailureReason.IgnoreUrl:
                    root.Value = IgnoreUrlMessage;
                    break;
                default:
                    return null;
            }

            return root;
        }
    }
}