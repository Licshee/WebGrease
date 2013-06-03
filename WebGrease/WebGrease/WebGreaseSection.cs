// ---------------------------------------------------------------------
// <copyright file="WebGreaseSection.cs" company="Microsoft">
//    Copyright Microsoft Corporation, all rights reserved
// </copyright>
// ---------------------------------------------------------------------
namespace WebGrease
{
    using System;

    using WebGrease.Configuration;

    /// <summary>The section.</summary>
    public class WebGreaseSection : IWebGreaseSection, ICachableWebGreaseSection
    {
        /// <summary>The is group.</summary>
        private readonly bool isGroup;

        /// <summary>The context.</summary>
        private readonly IWebGreaseContext context;

        /// <summary>The id parts.</summary>
        private readonly string[] idParts;

        /// <summary>The cache var by setting.</summary>
        private object cacheVarBySetting;

        /// <summary>The cache is skipable.</summary>
        private bool cacheIsSkipable;

        /// <summary>The cache var by content item.</summary>
        private ContentItem cacheVarByContentItem;

        /// <summary>The cachevar by file set.</summary>
        private IFileSet cacheVarByFileSet;

        /// <summary>The restore from cache action.</summary>
        private Func<ICacheSection, bool> restoreFromCacheAction;

        /// <summary>The when skipped action.</summary>
        private Action<ICacheSection> whenSkippedAction;

        /// <summary>Initializes a new instance of the <see cref="WebGreaseSection"/> class.</summary>
        /// <param name="context">The context.</param>
        /// <param name="idParts">The id parts.</param>
        /// <param name="isGroup">The is group.</param>
        private WebGreaseSection(IWebGreaseContext context, string[] idParts, bool isGroup)
        {
            this.context = context;
            this.idParts = idParts;
            this.isGroup = isGroup;
        }

        /// <summary>The create.</summary>
        /// <param name="context">The context.</param>
        /// <param name="idParts">The id parts.</param>
        /// <param name="isGroup">The is group.</param>
        /// <returns>The <see cref="IWebGreaseSection"/>.</returns>
        public static IWebGreaseSection Create(IWebGreaseContext context, string[] idParts, bool isGroup)
        {
            return new WebGreaseSection(context, idParts, isGroup);
        }

        /// <summary>Executes the action for the section.</summary>
        /// <param name="action">The action to execute.</param>
        public void Execute(Action action)
        {
            this.context.Measure.Start(this.isGroup, this.idParts);
            try
            {
                action();
            }
            finally
            {
                this.context.Measure.End(this.isGroup, this.idParts);
            }
        }

        /// <summary>Executes the action for the section and returns the result, the parameter of the action is the id of the section.</summary>
        /// <param name="action">The action to execute.</param>
        /// <typeparam name="T">The type fo the result of the action.</typeparam>
        /// <returns>The result of type T.</returns>
        public T Execute<T>(Func<T> action)
        {
            this.context.Measure.Start(this.isGroup, this.idParts);
            try
            {
                return action();
            }
            finally
            {
                this.context.Measure.End(this.isGroup, this.idParts);
            }
        }

        /// <summary>Makes the section cachable.</summary>
        /// <param name="varBySettings">The settings to var by.</param>
        /// <param name="isSkipable">Determines if the cache is skipable.</param>
        /// <returns>The <see cref="ICachableWebGreaseSection"/>.</returns>
        public ICachableWebGreaseSection CanBeCached(object varBySettings, bool isSkipable = false)
        {
            this.CanBeCached(null as IFileSet, varBySettings, isSkipable);
            return this;
        }

        /// <summary>Makes the section cachable.</summary>
        /// <param name="varByContentItem">The content item to vary by.</param>
        /// <param name="varBySettings">The settings to var by.</param>
        /// <param name="isSkipable">Determines if the cache is skipable.</param>
        /// <returns>The <see cref="ICachableWebGreaseSection"/>.</returns>
        public ICachableWebGreaseSection CanBeCached(ContentItem varByContentItem, object varBySettings = null, bool isSkipable = false)
        {
            this.cacheVarByContentItem = varByContentItem;
            this.cacheVarBySetting = varBySettings;
            this.cacheIsSkipable = isSkipable;
            return this;
        }

        /// <summary>Makes the section cachable.</summary>
        /// <param name="varByFileSet">The var By File Set.</param>
        /// <param name="varBySettings">The settings to var by.</param>
        /// <param name="isSkipable">Determines if the cache is skipable.</param>
        /// <returns>The <see cref="ICachableWebGreaseSection"/>.</returns>
        public ICachableWebGreaseSection CanBeCached(IFileSet varByFileSet, object varBySettings = null, bool isSkipable = false)
        {
            this.cacheVarByFileSet = varByFileSet;
            this.cacheVarBySetting = varBySettings;
            this.cacheIsSkipable = isSkipable;
            return this;
        }

        /// <summary>Sets the restore action</summary>
        /// <param name="action">The restore section action.</param>
        /// <returns>The <see cref="ICachableWebGreaseSection"/>.</returns>
        public ICachableWebGreaseSection RestoreFromCacheAction(Func<ICacheSection, bool> action)
        {
            this.restoreFromCacheAction = action;
            return this;
        }

        /// <summary>The when skipped.</summary>
        /// <param name="action">The action.</param>
        /// <returns>The <see cref="ICachableWebGreaseSection"/>.</returns>
        public ICachableWebGreaseSection WhenSkipped(Action<ICacheSection> action)
        {
            this.whenSkippedAction = action;
            return this;
        }

        /// <summary>
        /// Executes the action for the cached section
        /// the boolean/success result of the action will determine if the action will be stored in cache.
        /// </summary>
        /// <param name="cachableSectionAction">The section action.</param>
        /// <returns>If all was successfull (being passed from within the actions).</returns>
        public bool Execute(Func<ICacheSection, bool> cachableSectionAction)
        {
            var id = WebGreaseContext.ToStringId(this.idParts);

            var errorHasOccurred = false;
            EventHandler logOnErrorOccurred = delegate { errorHasOccurred = true; };

            var cacheSection = this.context.Cache.BeginSection(id, this.cacheVarByContentItem, this.cacheVarBySetting, this.cacheVarByFileSet);
            this.context.Log.ErrorOccurred += logOnErrorOccurred;
            try
            {
                if (this.context.TemporaryIgnore(this.cacheVarByFileSet, this.cacheVarByContentItem) && !errorHasOccurred)
                {
                    cacheSection.Save();
                    return true;
                }

                if (this.cacheIsSkipable && cacheSection.CanBeSkipped())
                {
                    if (this.whenSkippedAction != null)
                    {
                        this.whenSkippedAction(cacheSection);
                    }

                    if (!errorHasOccurred)
                    {
                        return true;
                    }
                }

                if (this.restoreFromCacheAction != null && cacheSection.CanBeRestoredFromCache())
                {
                    if (this.restoreFromCacheAction(cacheSection) && !errorHasOccurred)
                    {
                        return true;
                    }
                }

                this.context.Measure.Start(this.isGroup, this.idParts);
                try
                {
                    if (!cachableSectionAction(cacheSection) || errorHasOccurred)
                    {
                        return false;
                    }

                    cacheSection.Save();
                    return true;
                }
                finally
                {
                    this.context.Measure.End(this.isGroup, this.idParts);
                }
            }
            finally
            {
                this.context.Log.ErrorOccurred -= logOnErrorOccurred;
                cacheSection.EndSection();
            }
        }
    }
}