using System;
using System.Collections.Generic;

namespace Unity.Cloud.UserReporting
{
    /// <summary>
    /// Represents a user report.
    /// </summary>
    public class UserReport : UserReportPreview
    {
        #region Nested Types

        /// <summary>
        /// Provides sorting for metrics.
        /// </summary>
        private class UserReportMetricSorter : IComparer<UserReportMetric>
        {
            #region Methods

            /// <inheritdoc />
            public int Compare(UserReportMetric x, UserReportMetric y)
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            }

            #endregion
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="UserReport"/> class.
        /// </summary>
        public UserReport()
        {
            this.AggregateMetrics = new List<UserReportMetric>();
            this.Attachments = new List<UserReportAttachment>();
            this.ClientMetrics = new List<UserReportMetric>();
            this.DeviceMetadata = new List<UserReportNamedValue>();
            this.Events = new List<UserReportEvent>();
            this.Fields = new List<UserReportNamedValue>();
            this.Categories = new List<string>();
            this.Measures = new List<UserReportMeasure>();
            this.Screenshots = new List<UserReportScreenshot>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the attachments.
        /// </summary>
        public List<UserReportAttachment> Attachments { get; set; }

        /// <summary>
        /// Gets or sets the client metrics.
        /// </summary>
        public List<UserReportMetric> ClientMetrics { get; set; }

        /// <summary>
        /// Gets or sets the device metadata.
        /// </summary>
        public List<UserReportNamedValue> DeviceMetadata { get; set; }

        /// <summary>
        /// Gets or sets the events.
        /// </summary>
        public List<UserReportEvent> Events { get; set; }

        /// <summary>
        /// Gets or sets the fields.
        /// </summary>
        public List<UserReportNamedValue> Fields { get; set; }

        /// <summary>
        /// Gets or sets the measures.
        /// </summary>
        public List<UserReportMeasure> Measures { get; set; }

        /// <summary>
        /// Gets or sets the screenshots.
        /// </summary>
        public List<UserReportScreenshot> Screenshots { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Completes the user report. This is called by the client and only needs to be called when constructing a user report manually.
        /// </summary>
        public void Complete()
        {
            // Aggregate Metrics
            Dictionary<string, UserReportMetric> aggregateMetrics = new Dictionary<string, UserReportMetric>();
            foreach (UserReportMeasure measure in this.Measures)
            {
                foreach (UserReportMetric metric in measure.Metrics)
                {
                    if (!aggregateMetrics.ContainsKey(metric.Name))
                    {
                        UserReportMetric userReportMetric = new UserReportMetric();
                        userReportMetric.Name = metric.Name;
                        aggregateMetrics.Add(metric.Name, userReportMetric);
                    }
                    UserReportMetric aggregateMetric = aggregateMetrics[metric.Name];
                    aggregateMetric.Sample(metric.Average);
                    aggregateMetrics[metric.Name] = aggregateMetric;
                }
            }
            if (this.AggregateMetrics == null)
            {
                this.AggregateMetrics = new List<UserReportMetric>();
            }
            foreach (KeyValuePair<string, UserReportMetric> kvp in aggregateMetrics)
            {
                this.AggregateMetrics.Add(kvp.Value);
            }
            this.AggregateMetrics.Sort(new UserReportMetricSorter());
        }

        /// <summary>
        /// Fixes the user report by replace null lists with empty lists.
        /// </summary>
        public void Fix()
        {
            this.AggregateMetrics = this.AggregateMetrics ?? new List<UserReportMetric>();
            this.Attachments = this.Attachments ?? new List<UserReportAttachment>();
            this.ClientMetrics = this.ClientMetrics ?? new List<UserReportMetric>();
            this.DeviceMetadata = this.DeviceMetadata ?? new List<UserReportNamedValue>();
            this.Events = this.Events ?? new List<UserReportEvent>();
            this.Categories = this.Categories ?? new List<string>();
            this.Fields = this.Fields ?? new List<UserReportNamedValue>();
            this.Measures = this.Measures ?? new List<UserReportMeasure>();
            this.Screenshots = this.Screenshots ?? new List<UserReportScreenshot>();
        }

        /// <summary>
        /// Removes screenshots above a certain size from the user report.
        /// </summary>
        /// <param name="maximumWidth">The maximum width.</param>
        /// <param name="maximumHeight">The maximum height.</param>
        /// <param name="totalBytes">The total bytes allowed by screenshots.</param>
        /// <param name="ignoreCount">The number of screenshots to ignoreCount.</param>
        public void RemoveScreenshots(int maximumWidth, int maximumHeight, int totalBytes, int ignoreCount)
        {
            int byteCount = 0;
            for (int i = this.Screenshots.Count; i > 0; i--)
            {
                if (i < ignoreCount)
                {
                    continue;
                }
                UserReportScreenshot screenshot = this.Screenshots[i];
                byteCount += screenshot.DataBase64.Length;
                if (byteCount > totalBytes)
                {
                    break;
                }
                if (screenshot.Width > maximumWidth || screenshot.Height > maximumHeight)
                {
                    this.Screenshots.RemoveAt(i);
                }
            }
        }

        #endregion
    }
}