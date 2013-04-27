namespace WebGrease.Configuration
{
    using System.Collections.Generic;

    internal interface IFileSet
    {
        /// <summary>Gets the locales.</summary>
        IList<string> Locales { get; }

        /// <summary>Gets the preprocessing configuration.</summary>
        PreprocessingConfig Preprocessing { get; }

        /// <summary>
        /// Gets the dictionary of auto naming configs
        /// </summary>
        IDictionary<string, AutoNameConfig> Autonaming { get; }

        /// <summary>
        /// Gets the dictionary of auto naming configs
        /// </summary>
        IDictionary<string, BundlingConfig> Bundling { get; }

        /// <summary>Gets the name of the set.</summary>
        string Name { get; }

        /// <summary>Gets the output specified.</summary>
        string Output { get; }

        /// <summary>Gets the list of <see cref="InputSpec"/> items specified.</summary>
        IList<InputSpec> InputSpecs { get; }
    }
}