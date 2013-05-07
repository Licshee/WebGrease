// ----------------------------------------------------------------------------------------------------
// <copyright file="ResultFile.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System.Text;

    using WebGrease.Activities;
    using WebGrease.Extensions;

    /// <summary>The result file.</summary>
    public class ResultFile
    {
        #region Constructors and Destructors

        /// <summary>Prevents a default instance of the <see cref="ResultFile"/> class from being created.</summary>
        private ResultFile()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>Gets the content.</summary>
        public string Content { get; private set; }

        /// <summary>Gets the original path.</summary>
        public string OriginalPath { get; private set; }

        /// <summary>Gets the original relative path.</summary>
        public string OriginalRelativePath { get; private set; }

        /// <summary>Gets the path.</summary>
        public string Path { get; private set; }

        /// <summary>Gets the encoding.</summary>
        public Encoding Encoding { get; private set; }

        /// <summary>Gets the content type.</summary>
        public ResultContentType ResultContentType { get; private set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>The from content.</summary>
        /// <param name="content">The content.</param>
        /// <param name="fileType">The file type.</param>
        /// <param name="originalPath">The original path.</param>
        /// <param name="originalRootPath">The original root path.</param>
        /// <param name="encoding">The encoding</param>
        /// <returns>The <see cref="ResultFile"/>.</returns>
        public static ResultFile FromContent(string content, FileTypes fileType, string originalPath, string originalRootPath, Encoding encoding = null)
        {
            var rf = new ResultFile
                         {
                             Content = content, 
                             ResultContentType = ResultContentType.Memory,
                             OriginalPath = originalPath, 
                             Encoding = encoding ?? Encoding.UTF8,
                             OriginalRelativePath = originalPath.MakeRelativeToDirectory(originalRootPath), 
                         };
            return rf;
        }

        /// <summary>The from file.</summary>
        /// <param name="path">The path.</param>
        /// <param name="fileType">The file type.</param>
        /// <param name="originalPath">The original path.</param>
        /// <param name="originalRootPath">The original root path.</param>
        /// <returns>The <see cref="ResultFile"/>.</returns>
        public static ResultFile FromFile(string path, FileTypes fileType, string originalPath, string originalRootPath)
        {
            var rf = new ResultFile
                         {
                             Path = path, 
                             ResultContentType = ResultContentType.Disk,
                             OriginalPath = originalPath, 
                             OriginalRelativePath = originalPath.MakeRelativeToDirectory(originalRootPath), 
                         };
            return rf;
        }

        #endregion
    }
}