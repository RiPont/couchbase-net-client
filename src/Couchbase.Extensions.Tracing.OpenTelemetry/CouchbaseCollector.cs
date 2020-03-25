using Couchbase.Extensions.Tracing.OpenTelemetry.Implementation;
using OpenTelemetry.Collector;
using OpenTelemetry.Trace;
using System;

namespace Couchbase.Extensions.Tracing.OpenTelemetry
{
    public class CouchbaseCollector : IDisposable
    {
        private readonly DiagnosticSourceSubscriber _diagnosticSourceSubscriber;

        public CouchbaseCollector(Tracer tracer, CouchbaseCollectorOptions options)
        {
            _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(new CouchbaseTracingInHandler("Couchbase.Core.Tracing.Operations", tracer, options), null);
            _diagnosticSourceSubscriber.Subscribe();

        }
        public void Dispose() => _diagnosticSourceSubscriber?.Dispose();
    }
}
