// ----------------------------------------------------------------------------------------------------
// <copyright file="SassConfig.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease.Preprocessing.Sass
{
    using WebGrease.Configuration;

    /// <summary>
    /// The configuration for the sass preprocessing engine.
    /// </summary>
    public class SassConfig
    {
        private const string ScssExtensionName = "ScssExtension";

        private const string SassExtensionName = "SassExtension";

        #region Constructors and Destructors

        public SassConfig(PreprocessingConfig config)
            : this()
        {
            if (config != null)
            {
                var element = config.Element;
                this.SassExtension =
                    ((string)element.Element(SassExtensionName)).AsNullIfWhiteSpace()
                    ?? ((string)element.Attribute(SassExtensionName)).AsNullIfWhiteSpace()
                    ?? this.SassExtension;

                this.ScssExtension =
                    ((string)element.Element(ScssExtensionName)).AsNullIfWhiteSpace()
                    ?? ((string)element.Attribute(ScssExtensionName)).AsNullIfWhiteSpace()
                    ?? this.ScssExtension;
            }
        }

        private SassConfig()
        {
            this.SassExtension = ".sass";
            this.ScssExtension = ".scss";
        }

        #endregion

        #region Properties

        /// <summary>
        /// The file extension to match sass files, used to determine if the engine can process the file.
        /// </summary>
        internal string SassExtension { get; set; }

        /// <summary>
        /// The file extension to match scss files, used to determine if the engine can process the file.
        /// </summary>
        internal string ScssExtension { get; set; }

        #endregion
    }
}