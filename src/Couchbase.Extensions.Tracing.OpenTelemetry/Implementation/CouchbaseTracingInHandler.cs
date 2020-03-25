using OpenTelemetry.Collector;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Couchbase.Extensions.Tracing.OpenTelemetry.Implementation
{
    public class CouchbaseTracingInHandler : ListenerHandler
    {
        private readonly CouchbaseCollectorOptions _options;

        public CouchbaseTracingInHandler(string sourceName, Tracer tracer, CouchbaseCollectorOptions options)
            : base(sourceName, tracer)
        {
            _options = options;
        }

        public override void OnStartActivity(Activity activity, object payload)
        {
            const string EventNameSuffix = nameof(CouchbaseTracingInHandler) + "." + nameof(OnStartActivity);
            if (activity == null)
            {
                CollectorEventSource.Log.NullActivity(EventNameSuffix);
                return;
            }

            if (payload == null)
            {
                CollectorEventSource.Log.NullPayload(EventNameSuffix);
            }

            // TODO: support filtering requests.

            if (_options.Verbose)
            {
                CollectorEventSource.Log.Write(activity.OperationName + "." + nameof(OnStartActivity));
            }

            if (_options.Verbose)
            {
                this.Tracer.StartActiveSpanFromActivity(activity.OperationName, activity, out var span);
            }
        }

        public override void OnStopActivity(Activity activity, object payload)
        {
            base.OnStopActivity(activity, payload);
            if (this.Tracer.CurrentSpan?.Context.IsValid != true)
            {
                return;
            }

            this.Tracer.CurrentSpan.End();
        }

        public override void OnCustom(string name, Activity activity, object payload)
        {
            base.OnCustom(name, activity, payload);
            switch (name)
            {
                case "couchbase.operations_over_threshold":
                case "couchbase.is_over_threshold":
                    CollectorEventSource.Log.Write(name, activity);
                    this.Tracer.StartSpanFromActivity(name, activity).End();
                    break;
            }
        }
    }
}
