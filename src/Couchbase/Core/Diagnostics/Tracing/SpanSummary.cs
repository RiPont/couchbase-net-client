using System;
using System.Collections.Generic;
using System.Linq;
using Couchbase.Core.Diagnostics.Tracing;
using Couchbase.Core.Diagnostics.Tracing.Activities;
using Couchbase.Utils;
using Newtonsoft.Json;

namespace Couchbase.Core.Diagnostics.Tracing
{
    internal class SpanSummary : IComparable<SpanSummary>
    {
        private const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;

        [JsonIgnore]
        public string ServiceType { get; set; }

        [JsonProperty(CouchbaseTags.Summary.OperationName)]
        public string OperationName { get; set; }

        [JsonProperty(CouchbaseTags.Summary.LastOperationId, NullValueHandling = NullValueHandling.Ignore)]
        public string LastOperationId { get; set; }

        [JsonProperty(CouchbaseTags.Summary.LastLocalAddress, NullValueHandling = NullValueHandling.Ignore)]
        public string LastLocalAddress { get; set; }

        [JsonProperty(CouchbaseTags.Summary.LastRemoteAddress, NullValueHandling = NullValueHandling.Ignore)]
        public string LastRemoteAddress { get; set; }

        [JsonProperty(CouchbaseTags.Summary.LastLocalId, NullValueHandling = NullValueHandling.Ignore)]
        public string LastLocalId { get; set; }

        [JsonProperty(CouchbaseTags.Summary.LastDispatchUs, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long LastDispatchDuration { get; set; }

        [JsonProperty(CouchbaseTags.Summary.TotalDurationUs)]
        public long TotalDuration { get; set; }

        [JsonProperty(CouchbaseTags.Summary.EncodingDurationUs, NullValueHandling = NullValueHandling.Ignore)]
        public long EncodingDuration { get; set; }

        [JsonProperty(CouchbaseTags.Summary.DispatchDurationUs, NullValueHandling = NullValueHandling.Ignore)]
        public long DispatchDuration { get; set; }

        [JsonProperty(CouchbaseTags.Summary.ServerDurationUs, NullValueHandling = NullValueHandling.Ignore)]
        public long? ServerDuration { get; set; }

        [JsonProperty(CouchbaseTags.Summary.DecodingDurationUs, NullValueHandling = NullValueHandling.Ignore)]
        public long? DecodingDuration { get; set; }

        internal SpanSummary()
        {

        }

        internal SpanSummary(System.Diagnostics.Activity activity, Durations durations)
        {
            TotalDuration = activity.Duration.ToMicroseconds();
            OperationName = activity.OperationName;
            EncodingDuration = durations.EncodingDurationUs;
            DecodingDuration = durations.DecodingDurationUs;
            ServerDuration = durations.ServerDurationUs;
            DispatchDuration = durations.DispatchDurationUs;

            string lastDispatchDuration = null;
            foreach (var tag in activity.Tags)
            {
                switch (tag.Key)
                {
                    case CouchbaseTags.Summary.LastDispatchUs:
                        lastDispatchDuration = tag.Value;
                        break;
                    case CouchbaseTags.Summary.LastLocalAddress:
                        LastLocalAddress = tag.Value;
                        break;
                    case CouchbaseTags.Summary.LastLocalId:
                        LastLocalId = tag.Value;
                        break;
                    case CouchbaseTags.Summary.LastOperationId:
                        LastOperationId = tag.Value;
                        break;
                    case CouchbaseTags.Summary.LastRemoteAddress:
                        LastRemoteAddress = tag.Value;
                        break;
                    case CouchbaseTags.Service:
                        ServiceType = tag.Value;
                        break;
                }
            }

            // if we found a LastDispatchDuration, only then try and parse it.
            if (lastDispatchDuration != null && long.TryParse(lastDispatchDuration, out long lastDispatchDurationUs))
            {
                LastDispatchDuration = lastDispatchDurationUs;
            }
        }

        private long PreciseTimestampsToMicroseconds(DateTimeOffset startTimestamp, DateTimeOffset endTimestamp) => throw new NotImplementedException();

        public int CompareTo(SpanSummary other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Nullable.Compare(other.ServerDuration, ServerDuration);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
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
