using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Utils;
using Couchbase.Core.Diagnostics.Tracing.Activities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Couchbase.Query;

namespace Couchbase.Core.Diagnostics.Tracing
{
    public class ThresholdLoggingTracer : IDisposable
    {
        public const string DiagnosticListenerName = "Couchbase.Core.Tracing.Operations";

        private const int WorkerSleep = 100;
        //  private static readonly ILog Log = LogManager.GetLogger<ThresholdLoggingTracer>();

        private readonly CancellationTokenSource _source = new CancellationTokenSource();
        private readonly BlockingCollection<SpanSummary> _queue = new BlockingCollection<SpanSummary>(1000);
        private readonly DiagnosticListener _diagnosticSource = new DiagnosticListener(DiagnosticListenerName);
        private readonly List<OverThresholdObserver> _overThresholdObservers;

        private DateTime _lastrun = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the interval at which the <see cref="ThresholdLoggingTracer"/> writes to the log.
        /// Expressed as milliseconds.
        /// </summary>
        public int Interval { get; set; } = 10000; // 10 seconds

        /// <summary>
        /// Gets or sets the size of the sample used in the output written to the log.
        /// </summary>
        public int SampleSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the key-value operation threshold, expressed in microseconds.
        /// </summary>
        public int KvThreshold { get; set; } = 500000;

        /// <summary>
        /// Gets or sets the view operation threshold, expressed in microseconds.
        /// </summary>
        public int ViewThreshold { get; set; } = 1000000;

        /// <summary>
        /// Gets or sets the n1ql operation threshold, expressed in microseconds.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public int N1qlThreshold { get; set; } = 1000000;

        /// <summary>
        /// Gets or sets the search operation threshold, expressed in microseconds.
        /// </summary>
        public int SearchThreshold { get; set; } = 1000000;

        /// <summary>
        /// Gets or sets the analytics operation threshold, expressed in microseconds.
        /// </summary>
        public int AnalyticsThreshold { get; set; } = 1000000;

        /////// <summary>
        /////// Internal total count of all pending spans that have exceed the given service thresholds.
        /////// </summary>
        ////internal int TotalSummaryCount => _kvSummaryCount + _viewSummaryCount + _querySummaryCount + _searchSummaryCount + _analyticsSummaryCount;

        public ThresholdLoggingTracer(int maxSamples)
        {
            SampleSize = maxSamples;
            var observedServices = new[]
            {
                CouchbaseTags.ServiceKv,
                CouchbaseTags.ServiceView,
                CouchbaseTags.ServiceQuery,
                CouchbaseTags.ServiceSearch,
                CouchbaseTags.ServiceAnalytics,
            };

            _overThresholdObservers = observedServices.Select(s => new OverThresholdObserver(s, maxSamples)).ToList();

            Task.Factory.StartNew(DoWork, TaskCreationOptions.LongRunning);
        }


        internal ActivitySpan StartRootSpan(string operationName, string operationId, long? thresholdUs = null)
        {
            var activity = new Activity(operationName).AddDefaultTags();
            var newSpan = new ActivitySpan(activity, _diagnosticSource, new Durations(), thresholdUs);
            _diagnosticSource.StartActivity(activity, null);
            return newSpan;
        }
        
        internal ActivitySpan StartRootQuerySpan(QueryOptions queryOptions)
        {
            var span = this.StartRootSpan("n1ql", queryOptions.CurrentContextId, 0) //// N1qlThreshold)
                           .AddTag(CouchbaseTags.OperationId, queryOptions.CurrentContextId)
                           .AddTag(CouchbaseTags.Service, CouchbaseTags.ServiceQuery)
                           .AddTag(CouchbaseTags.OpenTelemetry.DbStatement, queryOptions.StatementValue);
            return span;
        }

        private void CheckAndReport()
        {
            var reports = _overThresholdObservers.Select(observer => observer.GetAndClearSamples())
                                                 .Where(summaryReport => summaryReport.Count > 0);
            var allReports = new JArray(reports.Select(r => JObject.FromObject(r)));
            _diagnosticSource.Write(TraceEventNames.OperationsOverThreshold, allReports);
        }

        private async Task DoWork()
        {
            // TODO:  Use a timer instead?  Was that already investigated and ruled out?
            while (!_source.Token.IsCancellationRequested)
            {
                try
                {
                    // determine if we need to write to log yet
                    if (DateTime.UtcNow.Subtract(_lastrun) > TimeSpan.FromMilliseconds(Interval))
                    {
                        ////if (_hasSummariesToLog)
                        ////{
                        ////    var result = new JArray();
                        ////    AddSummariesToResult(result, CouchbaseTags.ServiceKv, _kvSummaries, ref _kvSummaryCount);
                        ////    AddSummariesToResult(result, CouchbaseTags.ServiceView, _viewSummaries, ref _viewSummaryCount);
                        ////    AddSummariesToResult(result, CouchbaseTags.ServiceQuery, _querySummaries, ref _querySummaryCount);
                        ////    AddSummariesToResult(result, CouchbaseTags.ServiceSearch, _searchSummaries, ref _searchSummaryCount);
                        ////    AddSummariesToResult(result, CouchbaseTags.ServiceAnalytics, _analyticsSummaries, ref _analyticsSummaryCount);

                        ////    //Log.Info("Operations that exceeded service threshold: {0}", result.ToString(Formatting.None));

                        ////    _hasSummariesToLog = false;
                        ////}

                        CheckAndReport();

                        _lastrun = DateTime.UtcNow;
                    }

                    // sleep for a little while
                    await Task.Delay(TimeSpan.FromMilliseconds(WorkerSleep), _source.Token).ConfigureAwait(false);
                }
                catch (ObjectDisposedException) { } // ignore
                catch (OperationCanceledException) { } // ignore
                catch (Exception)
                {
                    // Log.Error("Error when procesing spans for spans over serivce thresholds", exception);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(WorkerSleep), _source.Token).ConfigureAwait(false);
            }
        }

        private static void AddSummariesToResult(JArray result, string serviceName, ICollection<SpanSummary> summaries, ref int summaryCount)
        {
            if (summaries.Any())
            {
                result.Add(new JObject
                {
                    {"service", serviceName},
                    {"count", summaryCount},
                    {"top", JArray.FromObject(summaries)}
                });
            }

            summaries.Clear();
            summaryCount = 0;
        }

        private static void AddSummryToSet(ICollection<SpanSummary> summaries, SpanSummary summary, ref int summaryCount, int maxSampleSize)
        {
            summaries.Add(summary);
            summaryCount += 1;

            while (summaries.Count > maxSampleSize)
            {
                summaries.Remove(summaries.First());
            }
        }

        public void Dispose()
        {
            _source?.Cancel();

            if (_queue != null)
            {
                _queue.CompleteAdding();
                while (_queue.Any())
                {
                    _queue.TryTake(out _);
                }

                _queue.Dispose();
            }
        }
    }
}

#region [ License information ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2018 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/

#endregion
