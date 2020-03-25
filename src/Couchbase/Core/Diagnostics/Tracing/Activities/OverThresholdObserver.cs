using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Couchbase.Core.Diagnostics.Tracing.Activities
{
    internal class OverThresholdObserver : IObserver<KeyValuePair<string, object>>
    {
        private readonly SortedSet<SpanSummary> _samples = new SortedSet<SpanSummary>();
        private readonly string _serviceName;
        private readonly int _maxSamples;
        private object _sampleLock = new object();
        private bool _completed = false;
        private long _samplesCounted = 0;

        public OverThresholdObserver(string serviceName, int maxSamples)
        {
            _serviceName = serviceName;
            _maxSamples = maxSamples;
        }

        public void OnCompleted() => _completed = true;

        // TODO: what to do on error?
        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object> value)
        {
            if (_completed)
            {
                return;
            }

            if (value.Key == TraceEventNames.IsOverThreshold
                && value.Value is SpanSummary spanSummary
                && spanSummary.ServiceType == _serviceName)
            {
                Interlocked.Increment(ref _samplesCounted);
                if (_samples.Count >= _maxSamples && spanSummary.CompareTo(_samples.Min) <= 0)
                {
                    // no need adding a sample that wouldn't be used.
                    return;
                }
                else
                {
                    lock (_sampleLock)
                    {
                        _samples.Add(spanSummary);
                        while (_samples.Count > _maxSamples)
                        {
                            _samples.Remove(_samples.Min);
                        }
                    }
                }
            }
        }

        internal SpanSummaryReport GetAndClearSamples()
        {
            SpanSummary[] results;
            long samplesCountedThisTime;
            lock (_sampleLock)
            {
                results = _samples.ToArray();
                _samples.Clear();
                samplesCountedThisTime = Interlocked.Exchange(ref _samplesCounted, 0);
            }

            return new SpanSummaryReport(_serviceName, samplesCountedThisTime, results);
        }
    }
}
