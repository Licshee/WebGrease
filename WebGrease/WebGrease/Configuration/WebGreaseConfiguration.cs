// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebGreaseConfiguration.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//   The web grease configuration root.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using Activities;
    using Extensions;

    /// <summary>The web grease configuration root.</summary>
    public class WebGreaseConfiguration
    {
        /// <summary>The environment variables match pattern.</summary>
        private static readonly Regex EnvironmentVariablesMatchPattern = new Regex("%(?<name>[a-zA-Z]*?)%", RegexOptions.Compiled);

        /// <summary>The minimum cache timeout.</summary>
        private static readonly TimeSpan MinimumCacheTimeout = TimeSpan.FromHours(1);

        /// <summary>Initializes a new instance of the <see cref="WebGreaseConfiguration"/> class.</summary>
        internal WebGreaseConfiguration()
        {
            this.ImageExtensions = new List<string>();
            this.ImageDirectories = new List<string>();
            this.CssFileSets = new List<CssFileSet>();
            this.JSFileSets = new List<JSFileSet>();
            this.DefaultLocales = new List<string>();
            this.DefaultThemes = new List<string>();
        }

        /// <summary>Initializes a new instance of the <see cref="WebGreaseConfiguration"/> class.</summary>
        /// <param name="configType">Configuration type (debug/release)</param>
        /// <param name="preprocessingPluginPath">The path to the pre processing plugin assemblies.</param>
        internal WebGreaseConfiguration(string configType, string preprocessingPluginPath = null)
            : this()
        {
            this.ConfigType = configType;
            this.PreprocessingPluginPath = preprocessingPluginPath;
        }

        /// <summary>Initializes a new instance of the <see cref="WebGreaseConfiguration"/> class.</summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="configurationFile">The configuration file.</param>
        internal WebGreaseConfiguration(WebGreaseConfiguration configuration, FileInfo configurationFile)
            : this(configurationFile, configuration.ConfigType, configuration.SourceDirectory, configuration.DestinationDirectory, configuration.LogsDirectory, configuration.ToolsTempDirectory, configuration.ApplicationRootDirectory, configuration.PreprocessingPluginPath)
        {
            this.CacheEnabled = configuration.CacheEnabled;
            this.CacheRootPath = configuration.CacheRootPath;
            this.CacheTimeout = configuration.CacheTimeout;
            this.CacheUniqueKey = configuration.CacheUniqueKey;
            this.Measure = configuration.Measure;
            this.Overrides = configuration.Overrides;
            this.MeasureReportPath = configuration.MeasureReportPath;
        }

        /// <summary>Initializes a new instance of the <see cref="WebGreaseConfiguration"/> class.</summary>
        /// <param name="configurationFile">The configuration File.</param>
        /// <param name="configType">Configuration type (debug/release)</param>
        /// <param name="sourceDirectory">The source directory, if not supplied, all relative paths are assumed
        /// to be relative to location of configuration file.</param>
        /// <param name="destinationDirectory">The destination directory where the statics should be generated.</param>
        /// <param name="logsDirectory">The directory where the logs will be generated.</param>
        /// <param name="toolsTempDirectory">The tools Temp Directory.</param>
        /// <param name="appRootDirectory">root directory of the application. Used for generating relative urls from the root. If not provided, the current directory is used.</param>
        /// <param name="preprocessingPluginPath">The path to the pre processing plugin assemblies.</param>
        internal WebGreaseConfiguration(FileInfo configurationFile, string configType, string sourceDirectory, string destinationDirectory, string logsDirectory, string toolsTempDirectory = null, string appRootDirectory = null, string preprocessingPluginPath = null)
            : this(configType, sourceDirectory, destinationDirectory, logsDirectory, toolsTempDirectory, appRootDirectory, preprocessingPluginPath)
        {
            Contract.Requires(configurationFile != null);
            Contract.Requires(configurationFile.Exists);

            if (configurationFile == null)
            {
                throw new ArgumentNullException("configType");
            }

            this.Parse(configurationFile.FullName);
        }

        /// <summary>Initializes a new instance of the <see cref="WebGreaseConfiguration"/> class.</summary>
        /// <param name="configType">The config type.</param>
        /// <param name="sourceDirectory">The source directory.</param>
        /// <param name="destinationDirectory">The destination directory.</param>
        /// <param name="logsDirectory">The logs directory.</param>
        /// <param name="toolsTempDirectory">The tools temp directory.</param>
        /// <param name="appRootDirectory">The app root directory.</param>
        /// <param name="preprocessingPluginPath">The preprocessing plugin path.</param>
        internal WebGreaseConfiguration(string configType, string sourceDirectory, string destinationDirectory, string logsDirectory, string toolsTempDirectory, string appRootDirectory = null, string preprocessingPluginPath = null)
            : this(configType, preprocessingPluginPath)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(destinationDirectory));
            Contract.Requires(!string.IsNullOrWhiteSpace(logsDirectory));

            this.SourceDirectory = sourceDirectory;
            this.DestinationDirectory = destinationDirectory;
            this.LogsDirectory = logsDirectory;
            this.ToolsTempDirectory = toolsTempDirectory;
            this.ApplicationRootDirectory = appRootDirectory ?? Environment.CurrentDirectory;

            Directory.CreateDirectory(destinationDirectory);
            Directory.CreateDirectory(logsDirectory);
        }

        /// <summary>
        /// The configuration type
        /// </summary>
        internal string ConfigType { get; private set; }

        /// <summary>
        /// The source directory for all paths in configuration.
        /// </summary>
        internal string SourceDirectory { get; set; }

        /// <summary>
        /// The destination directory where the static files should be generated.
        /// </summary>
        internal string DestinationDirectory { get; set; }

        /// <summary>
        /// The directory within which the <see cref="ResourcesResolutionActivity"/> can find resource tokens.
        /// </summary>
        internal string TokensDirectory { get; set; }

        /// <summary>
        /// The directory within which the <see cref="ResourcesResolutionActivity"/> can find resource tokens meant to override any other tokens with.
        /// </summary>
        internal string OverrideTokensDirectory { get; set; }

        /// <summary>
        /// Gets or sets the root application directory.
        /// </summary>
        internal string ApplicationRootDirectory { get; set; }

        /// <summary>
        /// The logs directory.
        /// </summary>
        internal string LogsDirectory { get; set; }

        /// <summary>
        /// The report directory.
        /// </summary>
        internal string MeasureReportPath { get; set; }

        /// <summary>
        /// The tools temp directory.
        /// </summary>
        internal string ToolsTempDirectory { get; set; }

        /// <summary>
        /// The path to the pre processing plugin assemblies
        /// </summary>
        internal string PreprocessingPluginPath { get; private set; }

        /// <summary>Gets or sets the image directories to be used.</summary>
        internal IList<string> ImageDirectories { get; set; }

        /// <summary>Gets or sets the image extensions to be used.</summary>
        internal IList<string> ImageExtensions { get; set; }

        /// <summary>Gets or sets the css file sets to be used.</summary>
        internal IList<CssFileSet> CssFileSets { get; set; }

        /// <summary>Gets or sets the javascript file sets to be used.</summary>
        internal IList<JSFileSet> JSFileSets { get; set; }

        /// <summary>Gets or sets the value that determines if webgrease measures it tasks.</summary>
        internal bool Measure { get; set; }

        /// <summary>
        /// Gets or sets the value that determines to use cache.
        /// </summary>
        internal bool CacheEnabled { get; set; }

        /// <summary>
        /// Gets or sets the root path used for caching, this defaults to the ToolsTempPath.
        /// Use the system temp folder (%temp%) if you want to enable this on the build server.
        /// </summary>
        internal string CacheRootPath { get; set; }

        /// <summary>
        /// Gets or sets the unique key for the unique key, is required when enabling cache.
        /// You should use the project Guid to make a distinction between cache for different projects when using a shared cache folder.
        /// </summary>
        internal string CacheUniqueKey { get; set; }

        /// <summary>
        /// gets or sets the value that determines how long to keep cache items that have not been touched. (both read and right will touch a file)
        /// </summary>
        internal TimeSpan CacheTimeout { get; set; }

        /// <summary>Gets or sets the overrides.</summary>
        internal TemporaryOverrides Overrides { get; set; }

        /// <summary>Gets or sets the default list of locales</summary>
        private IList<string> DefaultLocales { get; set; }

        /// <summary>Gets or sets the default list of themes</summary>
        private IList<string> DefaultThemes { get; set; }

        /// <summary>Gets or sets the default JavaScript minification configuration</summary>
        private IDictionary<string, JsMinificationConfig> DefaultJSMinification { get; set; }

        /// <summary>Gets or sets the default CSS minification configuration</summary>
        private IDictionary<string, CssMinificationConfig> DefaultCssMinification { get; set; }

        /// <summary>Gets or sets the default spriting configuration</summary>
        private IDictionary<string, CssSpritingConfig> DefaultSpriting { get; set; }

        /// <summary>Gets or sets the default preprocessing configuration.</summary>
        private PreprocessingConfig DefaultPreprocessing { get; set; }

        /// <summary>
        /// Gets the named configuration from the dictionary, or the first config if no name is passed or returns a default config if not found.
        /// </summary>
        /// <typeparam name="T">ConfigurationType to retrieve</typeparam>
        /// <param name="configDictionary">Dictionary of config objects</param>
        /// <param name="configName">Named configuration to find</param>
        /// <returns>the configuration object.</returns>
        internal static T GetNamedConfig<T>(IDictionary<string, T> configDictionary, string configName)
            where T : new()
        {
            T config;
            bool nullConfig = configName.IsNullOrWhitespace();

            // if the config name is blank, return the first config
            if (configDictionary.Keys.Any() && nullConfig)
            {
                config = configDictionary[configDictionary.Keys.First()];
            }
            else if (nullConfig || !configDictionary.TryGetValue(configName, out config))
            {
                // if the config is not found, use a default instance
                config = new T();
            }

            return config;
        }

        /// <summary>Validates the configuration.</summary>
        internal void Validate()
        {
            this.ApplicationRootDirectory = EnsureAndExpandDirectory(this.ApplicationRootDirectory, false);
            this.DestinationDirectory = EnsureAndExpandDirectory(this.DestinationDirectory, false);
            this.SourceDirectory = EnsureAndExpandDirectory(this.SourceDirectory, false);
            this.PreprocessingPluginPath = EnsureAndExpandDirectory(this.PreprocessingPluginPath, false);

            this.LogsDirectory = EnsureAndExpandDirectory(this.LogsDirectory, true);
            this.CacheRootPath = EnsureAndExpandDirectory(this.CacheRootPath, true);
            this.ToolsTempDirectory = EnsureAndExpandDirectory(this.ToolsTempDirectory, true);

            this.MeasureReportPath = EnsureAndExpandDirectory(this.MeasureReportPath ?? this.LogsDirectory, true);

            if (this.CacheTimeout > TimeSpan.Zero && this.CacheTimeout < MinimumCacheTimeout)
            {
                // Only timeout of an hour makes sense, otherwise don't use cache.
                this.CacheTimeout = MinimumCacheTimeout;
            }
            
        }

        /// <summary>Expands and ensures a directory exists and creates it if enabled.</summary>
        /// <param name="directory">The directory.</param>
        /// <param name="allowCreate">If it is allowed to create the directory.</param>
        /// <returns>The expanded directory.</returns>
        private static string EnsureAndExpandDirectory(string directory, bool allowCreate)
        {
            if (!string.IsNullOrWhiteSpace(directory))
            {
                directory = EnvironmentVariablesMatchPattern.Replace(
                    directory,
                    match => Environment.GetEnvironmentVariable(match.Groups["name"].Value));

                var di = new DirectoryInfo(directory);
                if (!di.Exists)
                {
                    if (allowCreate)
                    {
                        di.Create();
                    }
                    else
                    {
                        throw new DirectoryNotFoundException(directory);
                    }
                }

                return di.FullName;
            }

            return null;
        }
        
        /// <summary>Parses the configurations segments.</summary>
        /// <param name="configurationFile">The configuration file.</param>
        private void Parse(string configurationFile)
        {
            var element = XElement.Load(configurationFile);

            var settingsElement = element.Descendants("Settings");
            this.ParseSettings(settingsElement);

            foreach (var cssFileSetElement in element.Descendants("CssFileSet"))
            {
                var cssSet = new CssFileSet(cssFileSetElement, this.SourceDirectory, this.DefaultLocales, this.DefaultCssMinification, this.DefaultSpriting, this.DefaultThemes, this.DefaultPreprocessing);
                this.CssFileSets.Add(cssSet);
            }

            foreach (var jsFileSetElement in element.Descendants("JsFileSet"))
            {
                var jsFileSet = new JSFileSet(jsFileSetElement, this.SourceDirectory, this.DefaultLocales, this.DefaultJSMinification, this.DefaultPreprocessing);
                this.JSFileSets.Add(jsFileSet);
            }
        }

        /// <summary>Parses the settings xml elements.</summary>
        /// <param name="settingsElement">The settings xml element.</param>
        private void ParseSettings(IEnumerable<XElement> settingsElement)
        {
            foreach (var settingElement in settingsElement.Descendants())
            {
                var settingName = settingElement.Name.ToString();
                var settingValue = settingElement.Value;
                switch (settingName)
                {
                    case "ImageDirectories":
                        if (!string.IsNullOrWhiteSpace(settingValue))
                        {
                            foreach (
                                var imageDirectory in
                                    settingValue.Split(Strings.SemicolonSeparator, StringSplitOptions.RemoveEmptyEntries))
                            {
                                // Path.GetFullPath would make the path uniform taking alt directory separators into account
                                this.ImageDirectories.Add(Path.GetFullPath(Path.Combine(this.SourceDirectory, imageDirectory)));
                            }
                        }

                        break;
                    case "ImageExtensions":
                        if (!string.IsNullOrWhiteSpace(settingValue))
                        {
                            foreach (
                                var imageExtension in
                                    settingValue.Split(Strings.SemicolonSeparator, StringSplitOptions.RemoveEmptyEntries))
                            {
                                this.ImageExtensions.Add(imageExtension);
                            }
                        }

                        break;

                    case "TokensDirectory":
                        this.TokensDirectory = settingValue;
                        break;

                    case "OverrideTokensDirectory":
                        this.OverrideTokensDirectory = settingValue;
                        break;

                    case "Locales":
                        // get the default set of locales
                        this.LoadDefaultLocales(settingValue);
                        break;

                    case "Themes":
                        // get the default set of locales
                        this.LoadDefaultThemes(settingValue);
                        break;

                    case "CssMinification":
                        // get the default CSS minification configuration
                        this.LoadDefaultCssMinification(settingElement);
                        break;

                    case "Spriting":
                        // get the default CSS minification configuration
                        this.LoadDefaultSpriting(settingElement);
                        break;

                    case "JsMinification":
                        // get the default JavaScript minification configuration
                        this.LoadDefaultJSMinification(settingElement);
                        break;

                    case "Preprocessing":
                        // get the default JavaScript minification configuration
                        this.DefaultPreprocessing = new PreprocessingConfig(settingElement);
                        break;
                }
            }
        }

        /// <summary>
        /// Get the default theme list
        /// </summary>
        /// <param name="settingValue">settings string containing a semicolon-separate list of themes</param>
        private void LoadDefaultThemes(string settingValue)
        {
            // if it's null or whitespace, ignore it
            if (!string.IsNullOrWhiteSpace(settingValue))
            {
                // split it by semicolons, ignore empty entries, and add them to the list
                foreach (var theme in settingValue.Split(Strings.SemicolonSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    this.DefaultThemes.Add(theme);
                }
            }
        }

        /// <summary>
        /// Get the default locales list
        /// </summary>
        /// <param name="settingValue">settings string containing a semicolon-separate list of locales</param>
        private void LoadDefaultLocales(string settingValue)
        {
            // if it's null or whitespace, ignore it
            if (!string.IsNullOrWhiteSpace(settingValue))
            {
                // split it by semicolons, ignore empty entries, and add them to the list
                foreach (var locale in settingValue.Split(Strings.SemicolonSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    this.DefaultLocales.Add(locale);
                }
            }
        }

        /// <summary>
        /// Get the default JavaScript Minification configuration collection
        /// </summary>
        /// <param name="element">XML element from the Settings section</param>
        private void LoadDefaultJSMinification(XElement element)
        {
            // create a configuration object from the markup and set it in the dictionary.
            // create the dictionary if it hasn't been created yet.
            var configuration = new JsMinificationConfig(element);
            if (this.DefaultJSMinification == null)
            {
                this.DefaultJSMinification = new Dictionary<string, JsMinificationConfig>(StringComparer.OrdinalIgnoreCase);
            }

            this.DefaultJSMinification[configuration.Name] = configuration;
        }

        /// <summary>
        /// Get the default CSS Minification configuration collection
        /// </summary>
        /// <param name="element">XML element from the Settings section</param>
        private void LoadDefaultCssMinification(XElement element)
        {
            // create a configuration object from the markup and set it in the dictionary.
            // create the dictionary if it hasn't been created yet.
            var miniConfig = new CssMinificationConfig(element);
            if (this.DefaultCssMinification == null)
            {
                this.DefaultCssMinification = new Dictionary<string, CssMinificationConfig>(StringComparer.OrdinalIgnoreCase);
            }

            this.DefaultCssMinification[miniConfig.Name] = miniConfig;
        }

        /// <summary>
        /// Get the default spriting configuration collection
        /// </summary>
        /// <param name="element">XML element from the Settings section</param>
        private void LoadDefaultSpriting(XElement element)
        {
            // create a configuration object from the markup and set it in the dictionary.
            // create the dictionary if it hasn't been created yet.
            var miniConfig = new CssSpritingConfig(element);
            if (this.DefaultSpriting == null)
            {
                this.DefaultSpriting = new Dictionary<string, CssSpritingConfig>(StringComparer.OrdinalIgnoreCase);
            }

            this.DefaultSpriting[miniConfig.Name] = miniConfig;
        }
    }
}
