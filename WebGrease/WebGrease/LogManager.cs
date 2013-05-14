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
        /// <summary>Gets the information.</summary>
        private readonly Action<string, MessageImportance> information;

        /// <summary>Gets the warning.</summary>
        private readonly LogExtendedError extendedWarning;

        /// <summary>Gets the warning.</summary>
        private readonly Action<string> warning;

        /// <summary>Gets the error.</summary>
        private readonly LogError error;

        /// <summary>Gets the error.</summary>
        private readonly Action<string> errorMessage;

        /// <summary>Gets the extended error.</summary>
        private readonly LogExtendedError extendedError;

        /// <summary>Initializes a new instance of the <see cref="LogManager"/> class.</summary>
        /// <param name="logInformation">The log information.</param>
        /// <param name="logWarning">The log warning.</param>
        /// <param name="logExtendedWarning">The log extended warning.</param>
        /// <param name="logErrorMessage">The log Error Message.</param>
        /// <param name="logError">The log error.</param>
        /// <param name="logExtendedError">The log extended error.</param>
        public LogManager(Action<string, MessageImportance> logInformation, Action<string> logWarning, LogExtendedError logExtendedWarning, Action<string> logErrorMessage, LogError logError, LogExtendedError logExtendedError)
        {
            // settings default values to prevent legacy code from throwing null exceptions.
            this.information = logInformation;

            this.warning = logWarning;
            this.extendedWarning = logExtendedWarning;

            this.error = logError;
            this.errorMessage = logErrorMessage;
            this.extendedError = logExtendedError;

            // setting the Has* properties so that some consumers can check if null values were passed.
            this.HasExtendedErrorHandler = logExtendedError != null;
        }

        /// <summary>Gets a value indicating whether has extended error.</summary>
        public bool HasExtendedErrorHandler { get; set; }

        /// <summary>The information.</summary>
        /// <param name="message">The message.</param>
        /// <param name="messageImportance">The message importance.</param>
        public void Information(string message, MessageImportance messageImportance = MessageImportance.Normal)
        {
            if (this.information != null)
            {
                this.information(message, messageImportance);
            }
        }

        /// <summary>The warning.</summary>
        /// <param name="message">The message.</param>
        public void Warning(string message)
        {
            if (this.warning != null)
            {
                this.warning(message);
            }
        }

        /// <summary>The warning.</summary>
        /// <param name="subcategory">The subcategory.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="helpKeyword">The help keyword.</param>
        /// <param name="file">The file.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <param name="columnNumber">The column number.</param>
        /// <param name="endLineNumber">The end line number.</param>
        /// <param name="endColumnNumber">The end column number.</param>
        /// <param name="message">The message.</param>
        public void Warning(string subcategory, string errorCode, string helpKeyword, string file, int? lineNumber, int? columnNumber, int? endLineNumber, int? endColumnNumber, string message)
        {
            if (this.extendedWarning != null)
            {
                this.extendedWarning(subcategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber, message);
            }
        }

        /// <summary>The error.</summary>
        /// <param name="message">The message.</param>
        public void Error(string message)
        {
            if (this.errorMessage != null)
            {
                this.errorMessage(message);
            }
        }

        /// <summary>The error.</summary>
        /// <param name="exception">The exception.</param>
        /// <param name="customMessage">The custom message.</param>
        /// <param name="file">The file.</param>
        public void Error(Exception exception, string customMessage = null, string file = null)
        {
            if (this.error != null)
            {
                this.error(exception, customMessage, file);
            }
        }

        /// <summary>The error.</summary>
        /// <param name="subcategory">The subcategory.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="helpKeyword">The help keyword.</param>
        /// <param name="file">The file.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <param name="columnNumber">The column number.</param>
        /// <param name="endLineNumber">The end line number.</param>
        /// <param name="endColumnNumber">The end column number.</param>
        /// <param name="message">The message.</param>
        public void Error(string subcategory, string errorCode, string helpKeyword, string file, int? lineNumber, int? columnNumber, int? endLineNumber, int? endColumnNumber, string message)
        {
            if (this.extendedError != null)
            {
                this.extendedError(subcategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber, message);
            }
        }


    }
}