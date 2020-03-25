using Couchbase.Core.Diagnostics.Tracing.Activities;
using Couchbase.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace Couchbase.Core.Diagnostics.Tracing
{
    internal interface IActivityTracer
    {
        ActivitySpan StartRootQuerySpan(string statement, QueryOptions queryOptions);
    }
}
