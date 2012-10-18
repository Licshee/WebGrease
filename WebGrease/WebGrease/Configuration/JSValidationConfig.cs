// -----------------------------------------------------------------------
// <copyright file="JSValidationConfig.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WebGrease.Configuration
{
    using System.Diagnostics.Contracts;
    using System.Xml.Linq;
    using Extensions;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class JSValidationConfig
    {
         /// <summary>
        /// Initializes a new instance of the <see cref="JSValidationConfig"/> class.
        /// </summary>
        public JSValidationConfig()
        {
            // defaults
             this.ShouldAnalyze = true;
            this.AnalyzeArguments = Strings.DefaultAnalyzeArgs;
        }

          /// <summary>
        /// Initializes a new instance of the <see cref="JSValidationConfig"/> class.
        /// </summary>
        /// <param name="jsConfigurationSetElement">element containing js config info</param>
        internal JSValidationConfig(XElement jsConfigurationSetElement)
            : this()
        {
            Contract.Requires(jsConfigurationSetElement != null);
            /* expect this format:
             <Analyze name="Debug">
              <ShouldAnalyze>True</ShouldAnalyze>
              <AnalyzerArguments>-analyze -WARN:4</AnalyzerArguments>
            </Analyze>
             */

            var nameAttribute = jsConfigurationSetElement.Attribute("config");
            this.Name = nameAttribute != null ? nameAttribute.Value : string.Empty;

            foreach (var descendant in jsConfigurationSetElement.Descendants())
            {
                var name = descendant.Name.ToString();
                var value = descendant.Value;

                switch (name)
                {
                    case "Analyze":
                        this.ShouldAnalyze = value.TryParseBool();
                        break;
                    case "AnalayzeArguments":
                        this.AnalyzeArguments = !value.IsNullOrWhitespace() ? value : Strings.DefaultAnalyzeArgs;
                        break;
                }
            }
        }


        /// <summary>Gets or sets the name of the configuration.</summary>
        internal string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the set should be validated.
        /// </summary>
        internal bool ShouldAnalyze { get; set; }

        /// <summary>
        /// Gets or sets the analyze arguments.
        /// </summary>
        internal string AnalyzeArguments { get; set; }
    }
}
