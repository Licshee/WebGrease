// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InputSpec.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   A specification for a file or files.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Configuration
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Xml.Linq;

    /// <summary>A specification for a file or files.</summary>
    public class InputSpec
    {
        /// <summary>Initializes a new instance of the <see cref="InputSpec"/> class.</summary>
        internal InputSpec()
        {
            // default expectation of existing code when parsing the property prior when it was a string.
            this.SearchOption = System.IO.SearchOption.AllDirectories;
        }

        /// <summary>Initializes a new instance of the <see cref="InputSpec"/> class.</summary>
        /// <param name="element">config element specifiying a directory or file.</param>
        /// <param name="sourceDirectory">The base directory.</param>
        internal InputSpec(XElement element, string sourceDirectory)
        {
            Contract.Requires(element != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(sourceDirectory));

            // see if there is an optional "optional" attribute.
            // if it doesn't exist, the default value of IsOptional remains (FALSE)
            // meaning the input is mandatory and will fail the process if it
            // doesn't exist.
            var optionalAttribute = element.Attribute("optional");
            if (optionalAttribute != null)
            {
                // it exists -- see if we can parse the value. If not, the default
                // value of IsOptional property remains (FALSE)
                bool flag;
                if (bool.TryParse(optionalAttribute.Value, out flag))
                {
                    // parse succeeded -- use the boolean value of the attribute as
                    // the IsOptional property for the file spec
                    this.IsOptional = flag;
                }
            }

            var searchPatternAttribute = element.Attribute("searchPattern");
            this.SearchPattern = searchPatternAttribute != null ? searchPatternAttribute.Value : string.Empty;
            var searchOptionAttribute = element.Attribute("searchOption");
            if (searchOptionAttribute != null)
            {
                System.IO.SearchOption temp;
                this.SearchOption = Enum.TryParse<System.IO.SearchOption>(searchOptionAttribute.Value, out temp) ? temp : System.IO.SearchOption.AllDirectories;
            }
            else
            {
                this.SearchOption = System.IO.SearchOption.AllDirectories;
            }


            if (!string.IsNullOrWhiteSpace(element.Value))
            {
                // Path.GetFullPath would make the path uniform taking alt directory separators into account
                this.Path = System.IO.Path.GetFullPath(System.IO.Path.Combine(sourceDirectory, element.Value));
            }
        }

        /// <summary>
        /// Path for a file or directory.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Optional, can be used if the Path is a directory.
        /// If present, wildcards like http://msdn.microsoft.com/en-us/library/dd383462.aspx can be used.
        /// If not present and Path is a directory, all files (*.js or *.css) will be matched.
        /// </summary>
        public string SearchPattern { get; set; }

        /// <summary>
        /// Optional, can be used if the Path is a directory.
        /// If present, must be one of <see cref="System.IO.SearchOption"/>.
        /// If not present and Path is a directory, AllDirectories will be used.
        /// </summary>
        public SearchOption SearchOption { get; set; }

        /// <summary>
        /// Gets or sets a flag inidcating whether it's not an error if the input file does not exist
        /// </summary>
        public bool IsOptional { get; set; }
    }
}
