// ----------------------------------------------------------------------------------------------------
// <copyright file="WebGreaseContext.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System;

    using WebGrease.Activities;
    using WebGrease.Configuration;
    using WebGrease.Preprocessing;

    /// <summary>
    /// The web grease context.
    /// It contains all the global information necessary for all the tasks to run.
    /// Only very task specific values should be passed separately.
    /// It also contains all global functionality, like measuring, logging and caching.
    /// </summary>
    public class WebGreaseContext : IWebGreaseContext
    {
        #region Constructors and Destructors

        /// <summary>Initializes a new instance of the <see cref="WebGreaseContext"/> class. The web grease context.</summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="logInformation">The log information.</param>
        /// <param name="logWarning">The log warning.</param>
        /// <param name="logError">The log error.</param>
        /// <param name="logExtendedError">The log extended error.</param>
        public WebGreaseContext(WebGreaseConfiguration configuration, Action<string> logInformation = null, LogExtendedError logWarning = null, LogError logError = null, LogExtendedError logExtendedError = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            // Note: Configuration needs to be set first.
            this.Configuration = configuration;
            this.Measure = (configuration.Measure) ? new TimeMeasure() as ITimeMeasure : new NullTimeMeasure();
            this.Log = new LogManager(logInformation, logWarning, logError, logExtendedError);
            this.Preprocessing = new PreprocessingManager(this);
        }

        #endregion

        #region Public Properties

        /// <summary>Gets the configuration.</summary>
        public WebGreaseConfiguration Configuration { get; private set; }

        /// <summary>Gets the log.</summary>
        public LogManager Log { get; private set; }

        /// <summary>Gets the measure.</summary>
        public ITimeMeasure Measure { get; internal set; }

        /// <summary>Gets the preprocessing.</summary>
        public PreprocessingManager Preprocessing { get; private set; }

        #endregion
    }
}