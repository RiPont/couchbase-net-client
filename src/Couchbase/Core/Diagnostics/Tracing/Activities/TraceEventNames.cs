using System;
using System.Collections.Generic;
using System.Text;

namespace Couchbase.Core.Diagnostics.Tracing.Activities
{
    public static class TraceEventNames
    {
        public const string IsOverThreshold = CouchbaseTags.IsOverThreshold;
        public const string OperationsOverThreshold = "couchbase.operations_over_threshold";
    }
}
