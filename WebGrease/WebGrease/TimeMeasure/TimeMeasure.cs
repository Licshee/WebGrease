// ----------------------------------------------------------------------------------------------------
// <copyright file="WebGreaseMeasure.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>The web grease measure.</summary>
    public class TimeMeasure : ITimeMeasure
    {
        /// <summary>The id parts delimiter.</summary>
        public const string IdPartsDelimiter = ".";

        /// <summary>The char delimiter.</summary>
        private const char CharDelimiter = (char)2;

        #region Fields

        /// <summary>The measurement counts.</summary>
        private readonly IDictionary<string, int> measurementCounts = new Dictionary<string, int>();

        /// <summary>The measurements.</summary>
        private readonly IDictionary<string, double> measurements = new Dictionary<string, double>();

        /// <summary>The timers.</summary>
        private readonly IList<TimeMeasureItem> timers = new List<TimeMeasureItem>();

        /// <summary>Gets the results.</summary>
        public IEnumerable<TimeMeasureResult> Results
        {
            get
            {
                return this.measurements
                    .OrderByDescending(m => m.Value)
                    .Select(m =>
                        new TimeMeasureResult
                            {
                                IdParts = GetIdParts(m.Key),
                                Duration = m.Value,
                                Count = this.measurementCounts[m.Key]
                            });
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>The end.</summary>
        /// <param name="idParts">The id parts.</param>
        public void End(params string[] idParts)
        {
            var id = GetId(idParts);
            var lastTimer = this.timers.Last();
            if (lastTimer.Id != id)
            {
                throw new BuildWorkflowException("Trying to end a timer that was not started.");
            }

            this.StopTimer(lastTimer);
            this.ResumeLastTimer();
        }

        /// <summary>The start.</summary>
        /// <param name="idParts">The id parts.</param>
        public void Start(params string[] idParts)
        {
            var id = GetId(idParts);
            if (this.timers.Any(t => t.Id.Equals(id)))
            {
                throw new BuildWorkflowException("An error occurred while measuring, probably a wrong start/end for key: " + id);    
            }

            this.PauseLastTimer();
            this.timers.Add(new TimeMeasureItem(id, DateTime.Now));
            if (!this.measurementCounts.ContainsKey(id))
            {
                this.measurementCounts.Add(id, 0);
            }

            this.measurementCounts[id]++;
        }

        #endregion

        #region Methods

        /// <summary>The get name.</summary>
        /// <param name="idParts">The names.</param>
        /// <returns>The id.</returns>
        public static string GetId(IEnumerable<string> idParts)
        {
            return string.Join(
                IdPartsDelimiter, 
                idParts
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(s => s.UppercaseFirst()));
        }

        /// <summary>The get name.</summary>
        /// <param name="id">The name.</param>
        /// <returns>The id parts</returns>
        internal static IEnumerable<string> GetIdParts(string id)
        {
            return id.Replace(IdPartsDelimiter, CharDelimiter + string.Empty).Split(CharDelimiter);
        }

        /// <summary>The add to result.</summary>
        /// <param name="timer">The timer.</param>
        private void AddToResult(TimeMeasureItem timer)
        {
            if (!this.measurements.ContainsKey(timer.Id))
            {
                this.measurements.Add(timer.Id, 0);
            }

            this.measurements[timer.Id] += (DateTime.Now - timer.Value).TotalMilliseconds;
        }

        /// <summary>The pause last timer.</summary>
        private void PauseLastTimer()
        {
            if (this.timers.Any())
            {
                this.AddToResult(this.timers.Last());
            }
        }

        /// <summary>The resume last timer.</summary>
        private void ResumeLastTimer()
        {
            if (this.timers.Any())
            {
                this.timers.Last().Value = DateTime.Now;
            }
        }

        /// <summary>The stop timer.</summary>
        /// <param name="timer">The timer.</param>
        private void StopTimer(TimeMeasureItem timer)
        {
            this.timers.Remove(timer);
            this.AddToResult(timer);
        }

        #endregion
    }
}