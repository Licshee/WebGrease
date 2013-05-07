// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebGreaseContext.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
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
        #region Static Fields

        /// <summary>
        /// The cached content hashes
        /// </summary>
        private static readonly IDictionary<string, string> CachedContentHashes = new Dictionary<string, string>();

        /// <summary>
        /// The cached file hashes
        /// </summary>
        private static readonly IDictionary<string, Tuple<DateTime, string>> CachedFileHashes = new Dictionary<string, Tuple<DateTime, string>>();

        /// <summary>
        /// The md5 hasher
        /// </summary>
        private static readonly MD5 Hasher = MD5.Create();

        #endregion

        #region Fields

        /// <summary>Per session in memory cache of available files.</summary>
        private readonly IDictionary<string, IEnumerable<ResultFile>> availableFiles = new Dictionary<string, IEnumerable<ResultFile>>();

        #endregion

        #region Constructors and Destructors

        /// <summary>Initializes a new instance of the <see cref="WebGreaseContext"/> class.</summary>
        /// <param name="parentContext">The parent context.</param>
        /// <param name="configFile">The config file.</param>
        public WebGreaseContext(IWebGreaseContext parentContext, FileInfo configFile)
        {
            var configuration = new WebGreaseConfiguration(parentContext.Configuration, configFile);
            configuration.Validate();
            this.Initialize(
                configuration, parentContext.Log, parentContext.Cache, parentContext.Preprocessing, parentContext.SessionStartTime, parentContext.Measure);

            // TODO: Add measure of parent context as parent measure.
        }

        /// <summary>Initializes a new instance of the <see cref="WebGreaseContext"/> class. The web grease context.</summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="logInformation">The log information.</param>
        /// <param name="logWarning">The log warning.</param>
        /// <param name="logError">The log error.</param>
        /// <param name="logExtendedError">The log extended error.</param>
        public WebGreaseContext(
            WebGreaseConfiguration configuration, 
            Action<string> logInformation = null, 
            LogExtendedError logWarning = null, 
            LogError logError = null, 
            LogExtendedError logExtendedError = null)
        {
            var runStartTime = DateTime.UtcNow;
            configuration.Validate();
            var timeMeasure = configuration.Measure ? new TimeMeasure() as ITimeMeasure : new NullTimeMeasure();
            var logManager = new LogManager(logInformation, logWarning, logError, logExtendedError);
            var cacheManager = configuration.CacheEnabled ? new CacheManager(configuration, logManager) as ICacheManager : new NullCacheManager();
            var preprocessingManager = new PreprocessingManager(configuration, logManager, timeMeasure);
            this.Initialize(configuration, logManager, cacheManager, preprocessingManager, runStartTime, timeMeasure);
        }

        #endregion

        #region Public Properties

        /// <summary>Gets the cache manager.</summary>
        public ICacheManager Cache { get; private set; }

        /// <summary>Gets the configuration.</summary>
        public WebGreaseConfiguration Configuration { get; private set; }

        /// <summary>Gets the log.</summary>
        public LogManager Log { get; private set; }

        /// <summary>Gets the measure object.</summary>
        public ITimeMeasure Measure { get; private set; }

        /// <summary>Gets the preprocessing manager.</summary>
        public PreprocessingManager Preprocessing { get; private set; }

        /// <summary>Gets the session start time.</summary>
        public DateTime SessionStartTime { get; private set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>The clean directory.</summary>
        /// <param name="directory">The directory.</param>
        public static void CleanDirectory(string directory)
        {
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }

            Directory.CreateDirectory(directory);
        }

        /// <summary>The compute content hash.</summary>
        /// <param name="content">The content.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public static string ComputeContentHash(string content)
        {
            return BitConverter.ToString(Hasher.ComputeHash(Encoding.ASCII.GetBytes(content))).Replace("-", string.Empty);
        }

        /// <summary>The compute file hash.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The <see cref="string"/>.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "MD5 Lower case")]
        public static string ComputeFileHash(string filePath)
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

        /// <summary>The clean cache.</summary>
        public void CleanCache()
        {
            CleanDirectory(this.Configuration.CacheRootPath);
        }

        /// <summary>The clean destination.</summary>
        public void CleanDestination()
        {
            CleanDirectory(this.Configuration.DestinationDirectory);
            CleanDirectory(this.Configuration.LogsDirectory);
        }

        /// <summary>The clean tools temp.</summary>
        public void CleanToolsTemp()
        {
            CleanDirectory(this.Configuration.CacheRootPath);
        }

        /// <summary>Gets the available files, only gets them once per session/context.</summary>
        /// <param name="rootDirectory">The root directory.</param>
        /// <param name="directories">The directories.</param>
        /// <param name="extensions">The extensions.</param>
        /// <param name="fileType">The file type.</param>
        /// <returns>The available files.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Need lowercase")]
        public IEnumerable<ResultFile> GetAvailableFiles(string rootDirectory, IList<string> directories, IList<string> extensions, FileTypes fileType)
        {
            var key = new { rootDirectory, directories, extensions, fileType }.ToJson();
            if (!this.availableFiles.ContainsKey(key))
            {
                var results = new List<ResultFile>();
                if (directories == null)
                {
                    return results;
                }

                foreach (string directory in directories)
                {
                    foreach (string extension in extensions)
                    {
                        results.AddRange(
                            Directory.GetFiles(directory, extension, SearchOption.AllDirectories)
                                     .Select(f => f.ToLowerInvariant())
                                     .Select(f => ResultFile.FromFile(f.MakeRelativeToDirectory(rootDirectory), fileType, f, rootDirectory)));
                    }
                }

                this.availableFiles.Add(key, results);
            }

            return this.availableFiles[key];
        }

        /// <summary>The get content hash.</summary>
        /// <param name="content">The content.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public string GetContentHash(string content)
        {
            this.Measure.Start(TimeMeasureNames.FileHash);
            try
            {
                if (!CachedContentHashes.ContainsKey(content))
                {
                    CachedContentHashes.Add(content, ComputeContentHash(content));
                }

                return CachedContentHashes[content];
            }
            finally
            {
                this.Measure.End(TimeMeasureNames.FileHash);
            }
        }

        /// <summary>Gets the hash for the content of the file provided in the file path.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The MD5 hash.</returns>
        public string GetFileHash(string filePath)
        {
            this.Measure.Start(TimeMeasureNames.FileHash);
            try
            {
                var fi = new FileInfo(filePath);
                if (!fi.Exists)
                {
                    throw new FileNotFoundException("Could not find the file to create a hash for", filePath);
                }

                var uniqueId = fi.FullName;
                if (!CachedFileHashes.ContainsKey(uniqueId) || CachedFileHashes[uniqueId].Item1 != fi.LastWriteTimeUtc)
                {
                    CachedFileHashes[uniqueId] = new Tuple<DateTime, string>(fi.LastWriteTimeUtc, ComputeFileHash(fi.FullName));
                }

                return CachedFileHashes[uniqueId].Item2;
            }
            finally
            {
                this.Measure.End(TimeMeasureNames.FileHash);
            }
        }

        /// <summary>The make relative.</summary>
        /// <param name="absolutePath">The absolute path.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public string MakeRelative(string absolutePath, string relativePath = null)
        {
            return string.IsNullOrWhiteSpace(relativePath)
                ? absolutePath
                : absolutePath.MakeRelativeTo(this.Configuration.ApplicationRootDirectory);
        }

        /// <summary>The touch.</summary>
        /// <param name="filePath">The file path.</param>
        public void Touch(string filePath)
        {
            File.SetLastWriteTimeUtc(filePath, this.SessionStartTime);
        }

        #endregion

        #region Methods

        /// <summary>The initialize.</summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="cacheManager">The cache manager.</param>
        /// <param name="preprocessingManager">The preprocessing manager.</param>
        /// <param name="runStartTime">The run start time.</param>
        /// <param name="timeMeasure">The time measure.</param>
        private void Initialize(
            WebGreaseConfiguration configuration, 
            LogManager logManager, 
            ICacheManager cacheManager, 
            PreprocessingManager preprocessingManager, 
            DateTime runStartTime, 
            ITimeMeasure timeMeasure)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            // Note: Configuration needs to be set before the other ones.
            this.Configuration = configuration;
            this.Configuration.Validate();

            this.Measure = timeMeasure;

            this.Log = logManager;

            this.Cache = cacheManager;

            this.Preprocessing = preprocessingManager;

            this.SessionStartTime = runStartTime;

            this.Cache.SetContext(this);
            this.Preprocessing.SetContext(this);
        }

        #endregion
    }
}