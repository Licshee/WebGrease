// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CacheFileLockObject.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System;
    using System.IO;

    /// <summary>The file lock object.</summary>
    internal class CacheFileLockObject : IDisposable
    {
        /// <summary>The thread lock object.</summary>
        private static readonly object ThreadLockObject = new object();

        /// <summary>The lock file.</summary>
        private readonly string lockFile;

        /// <summary>Initializes a new instance of the <see cref="CacheFileLockObject"/> class.</summary>
        /// <param name="lockFile">The lock file.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Dispose is called in outer scope.")]
        private CacheFileLockObject(string lockFile)
        {
            if (File.Exists(lockFile))
            {
                throw new CacheFileLockException(lockFile);
            }

            this.lockFile = lockFile;
            try
            {
                File.Create(lockFile);
            }
            catch (Exception ex)
            {
                throw new CacheFileLockException(lockFile, ex);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            File.Delete(this.lockFile);
        }

        /// <summary>
        /// Tries to create a cache lock object, checks if the file exists, fails if it does, returns object if not. 
        /// Will remove lock file on dispose.
        /// </summary>
        /// <param name="lockFile">The lock file.</param>
        /// <returns>The <see cref="IDisposable"/>.</returns>
        internal static IDisposable TryCreate(string lockFile)
        {
            lock (ThreadLockObject)
            {
                return new CacheFileLockObject(lockFile);
            }
        }
    }
}