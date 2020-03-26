using Couchbase.Core.Diagnostics.Tracing.Activities;
using Couchbase.Query;

namespace Couchbase.Core.Diagnostics.Tracing
{
    internal interface IActivityTracer
    {
        ActivitySpan StartRootQuerySpan(string statement, QueryOptions queryOptions);
    }
}
