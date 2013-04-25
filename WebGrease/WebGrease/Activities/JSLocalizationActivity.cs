// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JSLocalizationActivity.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The class responsible for expanding the JS resource keys.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Common;

    /// <summary>The class responsible for expanding the JS resource keys.</summary>
    internal sealed class JSLocalizationActivity
    {
        /// <summary>The context.</summary>
        private readonly IWebGreaseContext context;

        /// <summary>Initializes a new instance of the <see cref="JSLocalizationActivity"/> class.</summary>
        /// <param name="context">The context.</param>
        internal JSLocalizationActivity(IWebGreaseContext context)
        {
            this.context = context;
            this.JsLocalizationInputs = new List<JSLocalizationInput>();
        }

        /// <summary>Gets or sets DestinationDirectory.</summary>
        internal string DestinationDirectory { get; set; }

        /// <summary>Gets or sets ResourcesDirectory.</summary>
        internal string ResourcesDirectory { get; set; }

        /// <summary>Gets the JS Localization Inputs.</summary>
        internal IList<JSLocalizationInput> JsLocalizationInputs { get; private set; }

        /// <summary>When overridden in a derived class, executes the task.</summary>
        internal void Execute()
        {
            if (string.IsNullOrWhiteSpace(this.DestinationDirectory))
            {
                throw new ArgumentException("JSLocalizationActivity - The destination directory cannot be null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(this.ResourcesDirectory))
            {
                throw new ArgumentException("JSLocalizationActivity - The resources directory cannot be null or whitespace.");
            }

            if (this.JsLocalizationInputs.Count == 0)
            {
                // Nothing to localize
                return;
            }

            try
            {
                this.context.Measure.Start(TimeMeasureNames.JSLocalizationActivity);
                Directory.CreateDirectory(this.DestinationDirectory);
                foreach (var jsLocalizationInput in this.JsLocalizationInputs.Where(_ => (_ != null && !string.IsNullOrWhiteSpace(_.DestinationFile))))
                {
                    var locales = jsLocalizationInput.Locales.Count == 0 ? new List<string> { Strings.DefaultLocale } : jsLocalizationInput.Locales;
                    foreach (var localeName in locales.Where(_ => !string.IsNullOrWhiteSpace(_)))
                    {
                        var destinationFile = jsLocalizationInput.DestinationFile.EndsWith(Strings.JS, StringComparison.OrdinalIgnoreCase) ? jsLocalizationInput.DestinationFile : Path.Combine(this.DestinationDirectory, localeName, string.Format(CultureInfo.InvariantCulture, "{0}.{1}", jsLocalizationInput.DestinationFile, Strings.JS));
                        this.ExpandLocaleResources(jsLocalizationInput, localeName, destinationFile);
                    }
                }
            }
            catch (ResourceOverrideException resourceOverrideException)
            {
                var errorMessage = string.Format(CultureInfo.CurrentUICulture, "JSLocalizationActivity - {0} has more than one value assigned. Only one value per key name is allowed in libraries and features. Resource key overrides are allowed at the product level only.", resourceOverrideException.TokenKey);
                throw new WorkflowException(errorMessage, resourceOverrideException);
            }
            catch (Exception exception)
            {
                throw new WorkflowException("JSLocalizationActivity - Error happened while executing the expand js resources activity.", exception);
            }
            finally
            {
                this.context.Measure.End(TimeMeasureNames.JSLocalizationActivity);
            }
        }

        /// <summary>Process the files for locales</summary>
        /// <param name="jsLocalizationInput">The JS localization input.</param>
        /// <param name="localeName">Name of the locale.</param>
        /// <param name="outputPath">The output directory to generate the JS file</param>
        private void ExpandLocaleResources(JSLocalizationInput jsLocalizationInput, string localeName, string outputPath)
        {
            // Js locale resources
            Dictionary<string, string> localeResources;
            ResourcesManager.TryGetResources(this.ResourcesDirectory, localeName, out localeResources);

            // Read the file from hard drive
            var fileContent = File.ReadAllText(jsLocalizationInput.SourceFile);

            // Apply the locale resources
            fileContent = ResourcesResolver.ExpandResourceKeys(fileContent, localeResources);

            // Write the localized file to disk
            FileHelper.WriteFile(outputPath, fileContent, Encoding.UTF8);
        }
    }
}
