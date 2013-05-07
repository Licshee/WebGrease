// ----------------------------------------------------------------------------------------------------
// <copyright file="WebGreaseMeasure.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using WebGrease.Extensions;

    /// <summary>The web grease measure.</summary>
    public class TimeMeasure : ITimeMeasure
    {
        /// <summary>The id parts delimiter.</summary>
        public const string IdPartsDelimiter = ".";

        /// <summary>The char delimiter.</summary>
        private const char CharDelimiter = (char)2;

        #region Fields

        /// <summary>The measurement counts.</summary>
        private readonly List<IDictionary<string, int>> measurementCounts = new List<IDictionary<string, int>> { new Dictionary<string, int>() };

        /// <summary>The measurements.</summary>
        private readonly List<IDictionary<string, double>> measurements = new List<IDictionary<string, double>> { new Dictionary<string, double>() };

        /// <summary>The timers.</summary>
        private readonly IList<TimeMeasureItem> timers = new List<TimeMeasureItem>();

        /// <summary>Gets the results.</summary>
        public TimeMeasureResult[] GetResults()
        {
            return
                this.measurements.Last().OrderByDescending(m => m.Value)
                    .Select(
                        m =>
                        new TimeMeasureResult
                            {
                                IdParts = GetIdParts(m.Key),
                                Duration = m.Value,
                                Count = this.measurementCounts.Last()[m.Key]
                            })
                    .ToArray();
        }

        #endregion

        #region Public Methods and Operators

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
        }

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


        public void BeginSection()
        {
            measurementCounts.Add(new Dictionary<string, int>());
            measurements.Add(new Dictionary<string, double>());
        }

        public void EndSection()
        {
            if (measurementCounts.Count() == 1)
            {
                throw new BuildWorkflowException("No measure sections available to end.");
            }

            var sectionMeasurementCounts = measurementCounts.Last();
            var sectionMeasurements = measurements.Last();

            measurementCounts.RemoveAt(measurementCounts.Count() - 1);
            measurements.RemoveAt(measurements.Count() - 1);

            measurementCounts.Last().Add(sectionMeasurementCounts);
            measurements.Last().Add(sectionMeasurements);
        }

        public void WriteResults(string filePathWithoutExtension, string title, DateTime utcStart)
        {
            var timeMeasureResults = GetResults();

            File.WriteAllText(
                filePathWithoutExtension + ".measure.txt",
                GetMeasureTable(title, timeMeasureResults)
                    + "\r\nTotal seconds: {0}".InvariantFormat((DateTime.UtcNow - utcStart).TotalSeconds));

            File.WriteAllText(
                filePathWithoutExtension + ".measure.csv",
                timeMeasureResults.GetCsv());
        }

        private static string GetMeasureTable(string title, IEnumerable<TimeMeasureResult> measureTotal)
        {
            return "{0}\r\n\r\n{1}\r\n\r\nStarted at: {2:yy-MM-dd HH:mm:ss.fff}".InvariantFormat(
                measureTotal.GetTextTable(title),
                measureTotal.Group(tm => tm.IdParts.FirstOrDefault()).GetTextTable(title),
                DateTime.Now);
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
            var id = timer.Id;
            if (!this.measurementCounts.Last().ContainsKey(id))
            {
                this.measurementCounts.Last().Add(id, 0);
            }
            this.measurementCounts.Last()[id]++;

            if (!this.measurements.Last().ContainsKey(id))
            {
                this.measurements.Last().Add(id, 0);
            }
            this.measurements.Last()[id] += (DateTime.Now - timer.Value).TotalMilliseconds;
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