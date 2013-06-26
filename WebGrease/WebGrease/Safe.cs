// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Safe.cs" company="Microsoft">
//   Copyright Microsoft Corporation, all rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace WebGrease
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    using WebGrease.Extensions;

    /// <summary>
    /// The safe class, adds a timeout on lock(). 
    /// The Resulting im code is the same when using lock() but with a exception when the timeout is reached.
    /// </summary>
    internal sealed class Safe : IDisposable
    {
        #region Fields

        /// <summary>
        /// The default lock timeout, set to 5 seconds for production, can be set to much lower or much higher when debugging.
        /// 5 seconds is neccesary for some image spriting scenario's that need to wait on each other on small virtual machines.
        /// </summary>
        internal const int DefaultLockTimeout = 5000;

        /// <summary>
        /// The maximum lock timeout value, used for scenario's that need to match the normal lock() in production.
        /// This can be set to a much lower value when debugging threading/dead-locking issues.
        /// </summary>
        internal const int MaxLockTimeout = int.MaxValue;

        /// <summary>The unique key locks.</summary>
        private static readonly IDictionary<string, object> UniqueKeyLocks = new Dictionary<string, object>();

        /// <summary>The pad lock object.</summary>
        private readonly object padlockObject;

        /// <summary>The secured flag.</summary>
        private readonly bool securedFlag;

        #endregion

        #region Constructors and Destructors

        /// <summary>Initializes a new instance of the <see cref="Safe"/> class.</summary>
        /// <param name="padlockObject">The padlock.</param>
        /// <param name="milliSecondTimeout">The milli second timeout.</param>
        private Safe(object padlockObject, int milliSecondTimeout)
        {
            this.padlockObject = padlockObject;
            this.securedFlag = Monitor.TryEnter(padlockObject, milliSecondTimeout);
        }

        #endregion

        /// <summary>Gets a value indicating whether secured.</summary>
        private bool Secured
        {
            get
            {
                return this.securedFlag;
            }
        }

        #region Public Methods and Operators

        /// <summary>The dispose.</summary>
        public void Dispose()
        {
            if (this.securedFlag)
            {
                Monitor.Exit(this.padlockObject);
            }
        }

        /// <summary>The lock.</summary>
        /// <param name="padlock">The padlock.</param>
        /// <param name="action">The code to run.</param>
        internal static void Lock(object padlock, Action action)
        {
            Lock(padlock, DefaultLockTimeout, action);
        }

        /// <summary>The lock.</summary>
        /// <param name="fileInfo">The file info.</param>
        /// <param name="fileAction">The file action.</param>
        internal static void FileLock(FileSystemInfo fileInfo, Action fileAction)
        {
            var file = fileInfo.FullName.ToUpperInvariant();
            UniqueKeyLock(file, fileAction);
        }

        /// <summary>The unique key lock.</summary>
        /// <param name="uniqueKey">The unique key.</param>
        /// <param name="millisecondTimeout">The millisecond timeout.</param>
        /// <param name="fileAction">The file action.</param>
        internal static void UniqueKeyLock(string uniqueKey, int millisecondTimeout, Action fileAction)
        {
            object uniqueKeyLock = null;
            Lock(
                UniqueKeyLocks, 
                () =>
                {
                    if (!UniqueKeyLocks.TryGetValue(uniqueKey, out uniqueKeyLock))
                    {
                        UniqueKeyLocks.Add(uniqueKey, uniqueKeyLock = new object());
                    }
                });

            Lock(uniqueKeyLock, millisecondTimeout, fileAction);
        }

        /// <summary>The lock.</summary>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="padlock">The padlock.</param>
        /// <param name="action">The code to run.</param>
        /// <returns>The <see cref="TResult"/>.</returns>
        internal static TResult Lock<TResult>(object padlock, Func<TResult> action)
        {
            return Lock(padlock, DefaultLockTimeout, action);
        }

        /// <summary>The lock.</summary>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="padlock">The padlock.</param>
        /// <param name="millisecondTimeout">The millisecond timeout.</param>
        /// <param name="action">The code to run.</param>
        /// <returns>The <see cref="TResult"/>.</returns>
        internal static TResult Lock<TResult>(object padlock, int millisecondTimeout, Func<TResult> action)
        {
            using (var bolt = new Safe(padlock, millisecondTimeout))
            {
                if (bolt.Secured)
                {
                    return action();
                }

                throw new TimeoutException(ResourceStrings.SafeLockFailedMessage.InvariantFormat(millisecondTimeout));
            }
        }

        /// <summary>The lock.</summary>
        /// <param name="padlock">The padlock.</param>
        /// <param name="millisecondTimeout">The millisecond timeout.</param>
        /// <param name="action">The code to run.</param>
        internal static void Lock(object padlock, int millisecondTimeout, Action action)
        {
            using (var bolt = new Safe(padlock, millisecondTimeout))
            {
                if (bolt.Secured)
                {
                    action();
                }
                else
                {
                    throw new TimeoutException(ResourceStrings.SafeLockFailedMessage.InvariantFormat(millisecondTimeout));
                }
            }
        }

        /// <summary>The unique key lock.</summary>
        /// <param name="uniqueKey">The unique key.</param>
        /// <param name="fileAction">The file action.</param>
        private static void UniqueKeyLock(string uniqueKey, Action fileAction)
        {
            UniqueKeyLock(uniqueKey, DefaultLockTimeout, fileAction);
        }

        #endregion
    }
}