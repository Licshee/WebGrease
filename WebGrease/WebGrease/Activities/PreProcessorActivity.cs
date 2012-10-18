// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PreprocessorActivity.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The pre processor activity
// </summary>
// --------------------------------------------------------------------------------------------------------------------


namespace WebGrease.Activities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using WebGrease.Configuration;
    using WebGrease.Preprocessing;

    /// <summary>The pre processor activity.</summary>
    internal sealed class PreprocessorActivity
    {
        internal Action<string> LogInformation;

        internal LogError LogError;

        internal LogExtendedError LogExtendedError;

        #region Constructors and Destructors

        /// <summary>Initializes a new instance of the <see cref="PreprocessorActivity"/> class.</summary>
        public PreprocessorActivity()
        {
            this.Inputs = new List<string>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The default extension of the type of files to process (js / css)
        /// </summary>
        internal string DefaultExtension { get; set; }

        /// <summary>Gets the list of inputs which need to be assembled.</summary>
        internal IList<string> Inputs { get; private set; }

        /// <summary>
        /// The folder to output the results to
        /// </summary>
        internal string OutputFolder { get; set; }

        /// <summary>
        /// If the activity needs to use hashed filenames or not.
        /// </summary>
        internal bool UseHashedFileNames { get; set; }

        /// <summary>
        /// The preprocessing configuration
        /// </summary>
        internal PreprocessingConfig PreprocessingConfig { get; set; }

        #endregion

        #region Methods

        internal IEnumerable<string> Execute()
        {
            var preprocessedFiles = new List<string>();
            foreach (var file in this.Inputs)
            {
                var fi = new FileInfo(file);
                if (!fi.Exists)
                {
                    throw new FileNotFoundException("Could not find the file {0} to preprocess on.");
                }

                if (!Directory.Exists(this.OutputFolder))
                {
                    Directory.CreateDirectory(this.OutputFolder);
                }

                var content = File.ReadAllText(file);
                content = PreprocessingManager.Instance.Process(content, file, this.PreprocessingConfig, this.LogInformation, this.LogError, this.LogExtendedError);
                if (content == null)
                {
                    throw new WorkflowException("An error occurred while processing the file: " + file);
                }

                var targetFile = this.GetTargetFile(fi);
                File.WriteAllText(targetFile, content, Encoding.UTF8);
                preprocessedFiles.Add(targetFile);
            }

            return preprocessedFiles;
        }

        /// <summary>
        /// Gets the target file.
        /// </summary>
        /// <param name="fi"></param>
        /// <returns></returns>
        private string GetTargetFile(FileSystemInfo fi)
        {
            if (UseHashedFileNames)
            {
                return Path.Combine(
                    this.OutputFolder, Guid.NewGuid().ToString().Replace("-", "") + "." + DefaultExtension.Trim('.'));
            }

            return
                fi.Extension.Trim('.').Equals(this.DefaultExtension.Trim('.'), StringComparison.OrdinalIgnoreCase)
                    ? Path.Combine(this.OutputFolder, fi.Name + ".processed." + this.DefaultExtension.Trim('.'))
                    : Path.Combine(this.OutputFolder, fi.Name + "." + this.DefaultExtension.Trim('.'));
        }

        #endregion
    }
}