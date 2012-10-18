// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImageAssemblyAnalysis.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   Represents the css analysis node generated from css which is analyzed
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Css.ImageAssemblyAnalysis.LogModel
{
    using Ast;

    /// <summary>The failure reason enumeration</summary>
    public enum FailureReason
    {
        /// <summary>
        /// The position criteria is not matched
        /// </summary>
        IncorrectPosition, 

        /// <summary>
        /// The no-repeat criteria is not found
        /// </summary>
        NoRepeat, 

        /// <summary>
        /// The url is not found
        /// </summary>
        NoUrl, 

        /// <summary>
        /// The url is configured to ignore
        /// </summary>
        IgnoreUrl
    }

    /// <summary>Represents the image log generated from css which is analyzed</summary>
    public class ImageAssemblyAnalysis
    {
        /// <summary>
        /// Gets or sets the failure reason for node not being considered for image analysis
        /// </summary>
        /// <value>The reason for failure</value>
        public FailureReason? FailureReason { get; set; }

        /// <summary>
        /// Gets or sets the Ast node to print for the context
        /// </summary>
        /// <value>The ast node for log print</value>
        public AstNode AstNode { get; set; }
    }
}
