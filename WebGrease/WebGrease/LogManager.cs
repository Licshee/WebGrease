// ----------------------------------------------------------------------------------------------------
// <copyright file="LogManager.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// <summary>
//   The log manager
// </summary>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease
{
    using System;

    using WebGrease.Activities;

    /// <summary>The web grease log.</summary>
    public class LogManager
    {
        /// <summary>The default information.</summary>
        private static readonly Action<string> DefaultInformation = delegate { };

        /// <summary>The default warning.</summary>
        private static readonly LogExtendedError DefaultWarning = delegate { };

        /// <summary>The default log error.</summary>
        private static readonly LogError DefaultLogError = delegate { };

        /// <summary>The default extended error.</summary>
        private static readonly LogExtendedError DefaultExtendedError = delegate { };

        /// <summary>Initializes a new instance of the <see cref="LogManager"/> class.</summary>
        /// <param name="logInformation">The log information.</param>
        /// <param name="logWarning">The log warning.</param>
        /// <param name="logError">The log error.</param>
        /// <param name="logExtendedError">The log extended error.</param>
        public LogManager(Action<string> logInformation, LogExtendedError logWarning, LogError logError, LogExtendedError logExtendedError)
        {
            // settings default values to prevent legacy code from throwing null exceptions.
            this.Information = logInformation ?? DefaultInformation;
            this.Warning = logWarning ?? DefaultWarning;
            this.Error = logError ?? DefaultLogError;
            this.ExtendedError = logExtendedError ?? DefaultExtendedError;

            // setting the Has* properties so that some consumers can check if null values were passed.
            this.HasInformationHandler = logInformation != DefaultInformation;
            this.HasWarningHandler = logWarning != DefaultWarning;
            this.HasErrorHandler = logError != DefaultLogError;
            this.HasExtendedErrorHandler = logExtendedError != DefaultExtendedError;
        }

        /// <summary>Gets a value indicating whether has information.</summary>
        public bool HasInformationHandler { get; private set; }

        /// <summary>Gets a value indicating whether has warning.</summary>
        public bool HasWarningHandler { get; private set; }

        /// <summary>Gets a value indicating whether has error.</summary>
        public bool HasErrorHandler { get; private set; }

        /// <summary>Gets a value indicating whether has extended error.</summary>
        public bool HasExtendedErrorHandler { get; private set; }

        /// <summary>Gets the information.</summary>
        public Action<string> Information { get; private set; }

        /// <summary>Gets the warning.</summary>
        public LogExtendedError Warning { get; private set; }

        /// <summary>Gets the error.</summary>
        public LogError Error { get; private set; }

        /// <summary>Gets the extended error.</summary>
        public LogExtendedError ExtendedError { get; private set; }
    }
}