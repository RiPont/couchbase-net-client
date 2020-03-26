using Newtonsoft.Json;

namespace Couchbase.Core.Diagnostics.Tracing.Activities
{
    internal struct SpanSummaryReport
    {
        public SpanSummaryReport(string service, long sampleCount, SpanSummary[] topSummaries)
        {
            Service = service;
            Count = sampleCount;
            TopSummaries = topSummaries;
        }

        [JsonProperty("service")]
        public string Service { get; }

        [JsonProperty("count")]
        public long Count { get; }

        [JsonProperty("top")]
        public SpanSummary[] TopSummaries { get; }
    }
}
