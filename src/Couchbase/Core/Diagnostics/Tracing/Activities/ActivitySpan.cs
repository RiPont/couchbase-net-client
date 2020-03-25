using Couchbase.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SummaryTags = Couchbase.Core.Diagnostics.Tracing.CouchbaseTags.Summary;

namespace Couchbase.Core.Diagnostics.Tracing.Activities
{
    internal class ActivitySpan : IDisposable
    {
        private readonly Activity _activity;
        private readonly DiagnosticSource _diagSource;
        private readonly Durations _durations;
        private readonly long? _thresholdUs;
        private bool hasEnded = false;

        internal ActivitySpan(Activity activity, DiagnosticSource diagSource, Durations durations, long? thresholdUs = null)
        {
            _activity = activity ?? throw new ArgumentNullException(nameof(activity));
            _diagSource = diagSource ?? throw new ArgumentNullException(nameof(diagSource));
            _durations = durations ?? throw new ArgumentNullException(nameof(durations));
            _thresholdUs = thresholdUs;
        }

        public void Dispose()
        {
            if (hasEnded)
            {
                // TODO:  log error "unexpected double end"
                return;
            }

            hasEnded = true;

            if (_diagSource == null)
            {
                _activity.Stop();
            }
            else
            {
                _diagSource.StopActivity(_activity, null);
            }

            // if we are a root activity and over-threshold, then report it
            if (_thresholdUs != null && _activity.Duration.ToMicroseconds() > _thresholdUs)
            {
                _activity.AddTag(CouchbaseTags.IsOverThreshold, true);
                if (_diagSource.IsEnabled(TraceEventNames.IsOverThreshold))
                {
                    var spanSummary = new SpanSummary(_activity, _durations);
                    _diagSource.Write(TraceEventNames.IsOverThreshold, spanSummary);
                }
            }

            var rootActivity = _activity.Parent;
            for (int sanity = 0; rootActivity?.Parent != null; sanity++)
            {
                rootActivity = rootActivity.Parent;

                if (sanity > 500)
                {
                    _activity.AddTag("exception", "parent/child loop");
                    return;
                }
            }

            rootActivity = rootActivity ?? _activity;

            switch (_activity.OperationName)
            {
                case CouchbaseOperationNames.RequestEncoding:
                    _durations.AddEncodingDuration(_activity.Duration);
                    break;
                case CouchbaseOperationNames.DispatchToServer:
                    _durations.AddDispatchgDuration(_activity.Duration);
                    rootActivity.AddTag(SummaryTags.LastDispatchUs, _activity.Duration);

                    if (_activity.TryGetTagValue(CouchbaseTags.LocalAddress, out var local))
                    {
                        rootActivity.AddTag(SummaryTags.LastLocalAddress, local);
                    }

                    if (_activity.TryGetTagValue(CouchbaseTags.OpenTelemetry.PeerHostIpv4, out var remote))
                    {
                        rootActivity.AddTag(SummaryTags.LastRemoteAddress, remote);
                    }

                    if (_activity.TryGetTagValue(CouchbaseTags.LocalId, out var localId))
                    {
                        rootActivity.AddTag(SummaryTags.LastLocalId, localId);
                    }

                    if (_activity.TryGetTagValue(CouchbaseTags.PeerLatency, out var peerLatency))
                    {
                        if (TimeSpanExtensions.TryConvertToMicros(peerLatency, out var value))
                        {
                            _durations.AddServerDuration(value);
                        }
                    }
                    break;
                case CouchbaseOperationNames.ResponseDecoding:
                    _durations.AddDecodingDuration(_activity.Duration);
                    break;
            }
        }

        internal ActivitySpan StartChild(string operationName)
        {
            var childActivity = new Activity(operationName);
            //// childActivity.SetParentId(_activity.TraceId, _activity.SpanId);
            ActivitySpan childSpan = new ActivitySpan(childActivity, _diagSource, _durations);
            _diagSource.StartActivity(childActivity, _activity);
            return childSpan;
        }

        internal ActivitySpan AddTag(string key, string value)
        {
            _activity.AddTag(key, value);
            return this;
        }

        // TODO: consider adding AddTag method (need to handle scope.Span.SetPeerLatencyTag from 2.7 ThresholdLoggingTracer line 287
    }
}
