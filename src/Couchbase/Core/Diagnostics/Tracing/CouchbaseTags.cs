namespace Couchbase.Core.Diagnostics.Tracing
{
    internal static class CouchbaseTags
    {
        public const string DbTypeCouchbase = "couchbase";

        public const string Service = "couchbase.service";
        public const string ServiceKv = "kv";
        public const string ServiceView = "view";
        public const string ServiceQuery = "n1ql";
        public const string ServiceSearch = "fts";
        public const string ServiceAnalytics = "cbas";

        public const string OperationId = "couchbase.operation_id";
        public const string DocumentKey = "couchbase.document_key";
        public const string LocalId = "couchbase.local_id";
        public const string Ignore = "couchbase.ignore";
        public const string ViewDesignDoc = "couchbase.design_doc";
        public const string ViewName = "couchbase.view_name";

        public const string LocalAddress = "local.address";
        public const string PeerLatency = "peer.latency";
        public const string IsOverThreshold = "couchbase.is_over_threshold";

        // These do not yet seem to be defined as constant in OpenTelemetry alpha, so we define them here.
        internal static class OpenTelemetry
        {
            public const string Component = "component";
            public const string DbType = "db.type";
            public const string DbStatement = "db.statement";
            public const string PeerHostIpv4 = "peer.ipv4";
            public const string SpanKind = "span.kind";
            public const string SpanKindClient = "client";
        }

        internal static class Summary
        {
            public const string OperationName = "operation_name";
            public const string LastOperationId = "last_operation_id";
            public const string LastLocalAddress = "last_local_address";
            public const string LastRemoteAddress = "last_remote_address";
            public const string LastLocalId = "last_local_id";
            public const string LastDispatchUs = "last_dispatch_us";
            public const string TotalDurationUs = "total_us";
            public const string EncodingDurationUs = "encode_us";
            public const string DispatchDurationUs = "dispatch_us";
            public const string ServerDurationUs = "server_us";
            public const string DecodingDurationUs = "decode_us";
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
