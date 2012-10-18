// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResourcesResolutionActivity.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The class which is responsible for resolving the resources hierarchy.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    /// <summary>The class which is responsible for resolving the resources hierarchy.</summary>
    internal sealed class ResourcesResolutionActivity
    {
        /// <summary>Initializes a new instance of the <see cref="ResourcesResolutionActivity"/> class.</summary>
        public ResourcesResolutionActivity()
        {
            this.ResourceKeys = new List<string>();
        }

        /// <summary>
        /// Gets or sets the paths to the base directory say "Content" which is the root path of application and features resources.
        /// </summary>
        /// <value>The root path of application and features resources.</value>
        internal string SourceDirectory { get; set; }

        /// <summary>
        /// Gets or sets the directory such as "Locales", "Themes"
        /// </summary>
        /// <value>The type of resources to process. Used for directory names.</value>
        internal ResourceType ResourceTypeFilter { get; set; }

        /// <summary>
        /// Gets or sets the directory App directory which would contain the list of sites.
        /// </summary>
        /// <value>The feature aggregated resources folder path.</value>
        internal string ApplicationDirectoryName { get; set; }

        /// <summary>
        /// Gets or sets the site name which would contain the site overriden resources
        /// </summary>
        /// <value>The site name.</value>
        internal string SiteDirectoryName { get; set; }

        /// <summary>
        /// Gets or sets the destination path where the compressed resources will be
        /// written to the hard drive
        /// </summary>
        /// <value>The output Resources folder path.</value>
        internal string DestinationDirectory { get; set; }

        /// <summary>
        /// Gets the list of locales or themes to generate the resources for.
        /// </summary>
        /// <value>The list of locales or themes.</value>
        internal IList<string> ResourceKeys { get; private set; }

        /// <summary>When overridden in a derived class, executes the task.</summary>
        internal void Execute()
        {
            if (this.ResourceKeys == null || 
                this.ResourceKeys.Count() == 0 ||
                string.IsNullOrWhiteSpace(this.SourceDirectory) ||
                !Directory.Exists(this.SourceDirectory))
            {
                // Nothing to resolve
                return;
            }

            try
            {
                ResourcesResolver.Factory(this.SourceDirectory, this.ResourceTypeFilter, this.ApplicationDirectoryName, this.SiteDirectoryName, this.ResourceKeys, this.DestinationDirectory).ResolveHierarchy();
            }
            catch (ResourceOverrideException resourceOverrideException)
            {
                // There was a token override in folder path that does not
                // allow token overriding. For this case, we need to
                // show a build error.
                var errorMessage = string.Format(CultureInfo.InvariantCulture, "ResourcesResolutionActivity - {0} has more than one value assigned. Only one value per key name is allowed in libraries and features. Resource key overrides are allowed at the product level only.", resourceOverrideException.TokenKey);
                throw new WorkflowException(errorMessage, resourceOverrideException);
            }
            catch (Exception exception)
            {
                throw new WorkflowException("ResourcesResolutionActivity - Error happened while executing the resolve resources activity", exception);
            }
        }
    }
}
