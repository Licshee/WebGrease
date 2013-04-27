// ----------------------------------------------------------------------------------------------------
// <copyright file="WebGreaseContext.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    using WebGrease.Activities;
    using WebGrease.Configuration;
    using WebGrease.Extensions;
    using WebGrease.Preprocessing;

    /// <summary>
    /// The web grease context.
    /// It contains all the global information necessary for all the tasks to run.
    /// Only very task specific values should be passed separately.
    /// It also contains all global functionality, like measuring, logging and caching.
    /// </summary>
    public class WebGreaseContext : IWebGreaseContext
    {
        /// <summary>
        /// The cached file hashes
        /// </summary>
        private static readonly IDictionary<string, Tuple<DateTime, string>> CachedFileHashes = new Dictionary<string, Tuple<DateTime, string>>();

        /// <summary>
        /// The cached content hashes
        /// </summary>
        private static readonly IDictionary<string, string> CachedContentHashes = new Dictionary<string, string>();

        /// <summary>
        /// The md5 hasher
        /// </summary>
        private static readonly MD5 Hasher = MD5.Create();

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
            this.Cache = configuration.CacheEnabled ? new CacheManager(this) as ICacheManager : new NullCacheManager();
            this.Log = new LogManager(logInformation, logWarning, logError, logExtendedError);
            this.Preprocessing = new PreprocessingManager(this);

            this.Configuration.Validate();
        }

        #endregion

        #region Public Properties

        /// <summary>Gets the configuration.</summary>
        public WebGreaseConfiguration Configuration { get; private set; }

        /// <summary>Gets the log.</summary>
        public LogManager Log { get; private set; }

        /// <summary>Gets the measure object.</summary>
        public ITimeMeasure Measure { get; private set; }

        /// <summary>Gets the preprocessing manager.</summary>
        public PreprocessingManager Preprocessing { get; private set; }

        /// <summary>Gets the cache manager.</summary>
        public ICacheManager Cache { get; private set; }

        public string GetContentHash(string content)
        {
            if (!CachedContentHashes.ContainsKey(content))
            {
                CachedContentHashes.Add(content, ComputeContentHash(content));
            }

            return CachedContentHashes[content];
        }

        public string GetFileHash(string filePath)
        {
            Measure.Start(TimeMeasureNames.FileHash);
            var fi = new FileInfo(filePath);
            if (!fi.Exists)
            {
                throw new FileNotFoundException("Could not find the file to create a hash for", filePath);
            }

            try
            {
                var uniqueId = fi.FullName;
                if (!CachedFileHashes.ContainsKey(uniqueId)
                    || CachedFileHashes[uniqueId].Item1 != fi.LastWriteTimeUtc)
                {
                    CachedFileHashes[uniqueId] = new Tuple<DateTime, string>(fi.LastWriteTimeUtc, ComputeFileHash(fi.FullName));
                }

                return CachedFileHashes[uniqueId].Item2;
            }
            finally
            {
                Measure.End(TimeMeasureNames.FileHash);
            }
        }

        public string MakeRelative(string absolutePath, string relativePath = null)
        {
            return absolutePath.MakeRelativeTo(relativePath ?? this.Configuration.ApplicationRootDirectory);
        }

        public string MakeAbsolute(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(this.Configuration.ApplicationRootDirectory))
            {
                return relativePath;
            }

            return new Uri(new Uri(this.Configuration.ApplicationRootDirectory, UriKind.Absolute), new Uri(relativePath, UriKind.Relative))
                .ToString()
                .Replace("/", @"\");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "MD5 Lower case")]
        internal static string ComputeFileHash(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var hash = Hasher.ComputeHash(fileStream);

                var hexString = new StringBuilder(hash.Length);
                for (var i = 0; i < hash.Length; i++)
                {
                    hexString.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture));
                }

                return hexString.ToString().ToLower(CultureInfo.InvariantCulture);
            }
        }

        private static string ComputeContentHash(string content)
        {
            return BitConverter.ToString(Hasher.ComputeHash(Encoding.ASCII.GetBytes(content))).Replace("-", "");
        }

        #endregion
    }
}