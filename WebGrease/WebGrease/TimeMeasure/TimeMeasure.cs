// ----------------------------------------------------------------------------------------------------
// <copyright file="TimeMeasure.cs" company="Microsoft Corporation">
//   Copyright Microsoft Corporation, all rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------------
namespace WebGrease
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using WebGrease.Activities;
    using WebGrease.Extensions;

    /// <summary>The web grease measure.</summary>
    public class TimeMeasure : ITimeMeasure
    {
        #region Fields

        /// <summary>The measurement counts.</summary>
        private readonly List<IDictionary<string, int>> measurementCounts = new List<IDictionary<string, int>> { new Dictionary<string, int>() };

        /// <summary>The measurements.</summary>
        private readonly List<IDictionary<string, double>> measurements = new List<IDictionary<string, double>> { new Dictionary<string, double>() };

        /// <summary>The timers.</summary>
        private readonly IList<TimeMeasureItem> timers = new List<TimeMeasureItem>();

        /// <summary>Gets the results.</summary>
        /// <returns>The measure results.</returns>
        public TimeMeasureResult[] GetResults()
        {
            return
                this.measurements.Last().OrderByDescending(m => m.Value)
                    .Select(
                        m =>
                        new TimeMeasureResult
                            {
                                IdParts = WebGreaseContext.ToIdParts(m.Key),
                                Duration = m.Value,
                                Count = this.measurementCounts.Last()[m.Key]
                            })
                    .ToArray();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>The start.</summary>
        /// <param name="isGroup">The is Group.</param>
        /// <param name="idParts">The id parts.</param>
        public void Start(bool isGroup, params string[] idParts)
        {
            var id = WebGreaseContext.ToStringId(idParts);
            if (this.timers.Any(t => t.Id.Equals(id)))
            {
                throw new BuildWorkflowException("An error occurred while measuring, probably a wrong start/end for key: " + id);
            }

            this.PauseLastTimer();
            this.timers.Add(new TimeMeasureItem(id, DateTime.Now));

            if (isGroup)
            {
                this.BeginGroup();
            }
        }

        /// <summary>The end.</summary>
        /// <param name="isGroup">The is Group.</param>
        /// <param name="idParts">The id parts.</param>
        public void End(bool isGroup, params string[] idParts)
        {
            if (isGroup)
            {
                this.EndGroup();
            }

            var id = WebGreaseContext.ToStringId(idParts);
            var lastTimer = this.timers.Last();
            if (lastTimer.Id != id)
            {
                throw new BuildWorkflowException("Trying to end a timer that was not started.");
            }

            this.StopTimer(lastTimer);
            this.ResumeLastTimer();
        }

        /// <summary>The begin group.</summary>
        public void BeginGroup()
        {
            this.measurementCounts.Add(new Dictionary<string, int>());
            this.measurements.Add(new Dictionary<string, double>());
        }

        /// <summary>The end group.</summary>
        public void EndGroup()
        {
            if (this.measurementCounts.Count() == 1)
            {
                throw new BuildWorkflowException("No measure sections available to end.");
            }

            var sectionMeasurementCounts = this.measurementCounts.Last();
            var sectionMeasurements = this.measurements.Last();

            this.measurementCounts.RemoveAt(this.measurementCounts.Count() - 1);
            this.measurements.RemoveAt(this.measurements.Count() - 1);

            this.measurementCounts.Last().Add(sectionMeasurementCounts);
            this.measurements.Last().Add(sectionMeasurements);
        }

        /// <summary>The write results.</summary>
        /// <param name="filePathWithoutExtension">The file path without extension.</param>
        /// <param name="title">The title.</param>
        /// <param name="utcStart">The utc start.</param>
        public void WriteResults(string filePathWithoutExtension, string title, DateTimeOffset utcStart)
        {
            var timeMeasureResults = this.GetResults();

            File.WriteAllText(
                filePathWithoutExtension + ".measure.txt",
                GetMeasureTable(title, timeMeasureResults) + "\r\nTotal seconds: {0}".InvariantFormat((DateTimeOffset.Now - utcStart).TotalSeconds));

            File.WriteAllText(
                filePathWithoutExtension + ".measure.csv",
                timeMeasureResults.GetCsv());
        }

        /// <summary>The write results.</summary>
        /// <param name="filePathWithoutExtension">The file path without extension.</param>
        /// <param name="results">The results.</param>
        /// <param name="title">The title.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="activityName">The activity name.</param>
        internal static void WriteResults(string filePathWithoutExtension, IDictionary<FileTypes, TimeMeasureResult[]> results, string title, DateTimeOffset startTime, string activityName)
        {
            var endTime = DateTimeOffset.Now;

            var textReportBuilder = new StringBuilder();
            textReportBuilder.AppendFormat("Configuration file: {0}", title);
            textReportBuilder.AppendLine();
            textReportBuilder.AppendFormat("Activity: {0}", activityName);
            textReportBuilder.AppendLine();
            textReportBuilder.AppendFormat("Started at: {0:yy-MM-dd HH:mm:ss.fff}", startTime);
            textReportBuilder.AppendLine();
            textReportBuilder.AppendFormat("Ended at: {0:yy-MM-dd HH:mm:ss.fff}", endTime);
            textReportBuilder.AppendLine();
            textReportBuilder.AppendFormat("Total Seconds: {0}", (endTime - startTime).TotalSeconds);
            textReportBuilder.AppendLine();
            textReportBuilder.AppendLine();

            foreach (var result in results.OrderByDescending(r => r.Value.Sum(v => v.Duration)))
            {
                var fileType = result.Key;
                var timeMeasureResults = result.Value;

                textReportBuilder.AppendLine(timeMeasureResults.GetTextTable(fileType + " - " + "Details"));
            }

            textReportBuilder.AppendLine();
            textReportBuilder.AppendLine();
            textReportBuilder.AppendLine();
            textReportBuilder.AppendLine();
            textReportBuilder.AppendLine();

            foreach (var result in results.OrderByDescending(r => r.Value.Sum(v => v.Duration)))
            {
                var fileType = result.Key;
                var timeMeasureResults = result.Value;

                textReportBuilder.AppendLine(timeMeasureResults.Group(tm => tm.IdParts.FirstOrDefault()).GetTextTable(fileType + " - " + "Summary"));
            }

            File.WriteAllText("{0}.{1}.measure.txt".InvariantFormat(filePathWithoutExtension, activityName), textReportBuilder.ToString());

            foreach (var result in results)
            {
                File.WriteAllText(
                    "{0}.{1}.{2}.measure.csv".InvariantFormat(filePathWithoutExtension, activityName, result.Key),
                    result.Value.GetCsv());
            }
        }

        /// <summary>The get measure table.</summary>
        /// <param name="title">The title.</param>
        /// <param name="measureTotal">The measure total.</param>
        /// <returns>The <see cref="string"/>.</returns>
        private static string GetMeasureTable(string title, IEnumerable<TimeMeasureResult> measureTotal)
        {
            return "{0}\r\n\r\n{1}\r\n\r\nStarted at: {2:yy-MM-dd HH:mm:ss.fff}".InvariantFormat(
                measureTotal.GetTextTable(title),
                measureTotal.Group(tm => tm.IdParts.FirstOrDefault()).GetTextTable(title),
                DateTime.Now);
        }

        #endregion

        #region Methods

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