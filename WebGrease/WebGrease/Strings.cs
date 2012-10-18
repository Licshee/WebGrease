// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Strings.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// <summary>
//   Represents the various strings
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>Represents the various strings </summary>
    internal static class Strings
    {
        /// <summary>Gets the semicolon separator field.</summary>
        private static readonly char[] SemicolonSeparatorField = new[] { ';' };

        /// <summary>Gets the semicolon separator.</summary>
        internal static char[] SemicolonSeparator
        {
            get { return SemicolonSeparatorField; }
        }

        /// <summary>
        /// Gets the all files filter
        /// </summary>
        /// <value>The *.* string.</value>
        internal const string AllFilesFilter = "*.*";

        /// <summary>
        /// Gets the xml files filter
        /// </summary>
        internal const string XmlFilesFilter = "*.xml";

        /// <summary>
        /// Gets the error string
        /// </summary>
        internal const string Error = "Error";

        /// <summary>
        /// Gets the warning string
        /// </summary>
        internal const string Warning = "Warning";

        /// <summary>
        /// Gets the default workflow string
        /// </summary>
        internal const string WorkFlow = "Statics Workflow";

        /// <summary>
        /// Gets the default VS error format
        /// </summary>
        internal const string VSErrorFormat = "{0}: {1}{2} {3}: {4}";

        /// <summary>
        /// Back slash string constant
        /// </summary>
        internal const string BackwardSlash = @"\";

        /// <summary>
        /// Comma String
        /// </summary>
        internal const string Comma = ",";

        /// <summary>
        /// Gets the CSS.
        /// </summary>
        /// <value>The CSS string.</value>
        internal const string Css = "css";

        /// <summary>
        /// min css extension
        /// </summary>
        internal const string MinCssExtension = ".min.css";

        /// <summary>
        /// min js extension
        /// </summary>
        internal const string MinJsExtension = ".min.js";

        /// <summary>
        /// Gets the CSS filter.
        /// </summary>
        /// <value>The CSS filter.</value>
        internal const string CssFilter = "*.css";

        /// <summary>
        /// Gets the Min CSS filter.
        /// </summary>
        /// <value>The Min CSS filter.</value>
        internal const string MinCssFilter = "*.min.css";

        /// <summary>
        /// Gets the JS filter.
        /// </summary>
        /// <value>The JS filter.</value>
        internal const string JsFilter = "*.js";

        /// <summary>
        /// Gets the Min JS filter.
        /// </summary>
        /// <value>The Min JS filter.</value>
        internal const string MinJsFilter = "*.min.js";

        /// <summary>
        /// This is default Css Version string. Version 2.1
        /// </summary>
        internal const string DefaultCssVersion = "css21";

        /// <summary>
        /// Gets the Double Dot string constant
        /// </summary>
        /// <value>Double Dot string constant</value>
        internal const string DoubleDot = "..";

        /// <summary>
        /// File Filter Separator.
        /// </summary>
        internal static readonly char[] FileFilterSeparator = ",".ToCharArray();

        /// <summary>
        /// Gets the Forward slash string constant
        /// </summary>
        /// <value>Forward slash string constant</value>
        internal const string ForwardSlash = "/";

        /// <summary>
        /// The internet explorer IE shortcut
        /// </summary>
        internal const string Ie = "ie";

        /// <summary>
        /// Js String constant
        /// </summary>
        internal const string JS = "js";

        /// <summary>
        /// Localization delimiter. Resource key can be wrapped with one of the special charaters in the s_LocalizationResourceKeyDelimiter. 
        /// Currently there is only one delimiter, this can be extended by adding more characters with a comma seperated. Ex: %,$ 
        /// </summary>
        internal const string LocalizationResourceKeyDelimiter = "%";

        /// <summary>
        /// Localization resource key format
        /// </summary>
        internal const string LocalizationResourceKeyRegex = @"^[a-zA-Z][\w\.]*$";

        /// <summary>
        /// Measurement units
        /// </summary>
        internal const string Px = "px";

        /// <summary>
        /// The scan log extension
        /// </summary>
        internal const string ScanLogExtension = ".scan.xml";

        /// <summary>
        /// Resource filter string constant
        /// </summary>
        internal const string ResxExtension = ".resx";

        /// <summary>
        /// Gets the semicolon.
        /// </summary>
        /// <value>The semicolon.</value>
        internal const string Semicolon = ";";

        /// <summary>
        /// Gets the semicolon.
        /// </summary>
        /// <value>The semicolon.</value>
        internal const char SemicolonChar = ';';

        /// <summary>
        /// Gets the comma.
        /// </summary>
        /// <value>The comma.</value>
        internal const char CommaChar = ',';

        /// <summary>
        /// Gets the pipe.
        /// </summary>
        /// <value>The pipe.</value>
        internal const char PipeChar = '|';

        /// <summary>
        /// Gets the underscore.
        /// </summary>
        /// <value>The underscore.</value>
        internal const string Underscore = "_";

        /// <summary>
        /// Gets the default locale.
        /// </summary>
        /// <value>The default locale.</value>
        internal const string DefaultLocale = "generic-generic";

        /// <summary>
        /// Gets the default locale resx.
        /// </summary>
        /// <value>The default locale resx.</value>
        internal const string DefaultResx = "generic-generic.resx";

        /// <summary>
        /// Gets the Xml File extension
        /// </summary>
        internal const string XmlExtension = ".xml";

        /// <summary>
        /// Lazy load Xml File extension
        /// </summary>
        internal const string LazyLoadXmlFileExtension = ".lazyload.xml";

        /// <summary>
        /// argument name of the globals to ingore for js minification
        /// </summary>
        internal const string GlobalsToIgnoreArg = "/global:";

        /// <summary>
        /// default globals ignored.
        /// </summary>
        internal const string DefaultGlobalsToIgnore = "jQuery";

        /// <summary>
        /// Default set of minify args.
        /// </summary>
        internal const string DefaultMinifyArgs = "";

        /// <summary>
        /// Set of args passed to jscruch for analyze/minify.
        /// </summary>
        internal const string DefaultAnalyzeArgs = "-analyze -WARN:4";

        // default list of image extensions.
        internal static readonly List<string> DefaultImageExtensions = new[] { "png", "jpg", "jpeg", "gif" }.ToList();

        /// <summary>
        /// css selectors determined to be hacks.
        /// </summary>
        internal static readonly string[] HackSelectors = "html>body;* html;*:first-child+html p;head:first-child+body;head+body;body>;*>html;*html>body".Split(';');

        internal const string CssLocalizedOutput = "CssLocalizedOutput";
        internal const string JsLocalizedOutput = "JsLocalizedOutput";
        internal const string CssPreHashedOutput = "CssPreHashedOutput";
        internal const string JsPreHashedOutput = "JsPreHasedOutput";

        // file name for image hash logs
        internal const string ImagesLogFile = "images_log.xml";

        // file name for css logs
        internal const string CssLogFile = "css_log.xml";

        // file name for js logs
        internal const string JsLogFile = "js_log.xml";
    }
}
