using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

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
