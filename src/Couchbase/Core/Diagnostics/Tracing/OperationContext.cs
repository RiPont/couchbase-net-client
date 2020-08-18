using System;
using Couchbase.Utils;
using Newtonsoft.Json;

namespace Couchbase.Core.Diagnostics.Tracing
{
    public class OperationContext : IComparable<OperationContext>
    {
        [JsonProperty("s")]
        public string ServiceType { get; }

        [JsonProperty("i")]
        public string OperationId { get; }

        [JsonProperty("c", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ConnectionId { get; set; }

        [JsonProperty("b", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string BucketName { get; set; }

        [JsonProperty("l", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string LocalEndpoint { get; set; }

        [JsonProperty("r", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string RemoteEndpoint { get; set; }

        [JsonProperty("t", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public uint TimeoutMicroseconds { get; set; }

        [JsonProperty("d", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long? ServerDuration { get; set; }

        public OperationContext(string serviceType, string operationId = null)
        {
            ServiceType = serviceType;
            OperationId = operationId;
        }

        public override string ToString()
        {
            return string.Join(" ",
                ExceptionUtil.OperationTimeout,
                JsonConvert.SerializeObject(this, Formatting.None).
                    Replace("{", "[").Replace("}","]")
            );
        }

        public int CompareTo(OperationContext other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Nullable.Compare(other.ServerDuration, ServerDuration);
        }

        public static OperationContext CreateKvContext(uint opaque)
        {
            const string hexPrefix = "0x", hexFormat = "x";
            return new OperationContext(RequestTracing.ServiceIdentifier.Kv, string.Join(hexPrefix, opaque.ToString(hexFormat)));
        }

        public static OperationContext CreateViewContext(string bucketName, string remoteEndpoint)
        {
            return new OperationContext(RequestTracing.ServiceIdentifier.View)
            {
                RemoteEndpoint = remoteEndpoint
            };
        }

        public static OperationContext CreateQueryContext(string contextId, string buckName, string remoteEndpoint)
        {
            return new OperationContext(RequestTracing.ServiceIdentifier.Query, contextId)
            {
                BucketName = buckName,
                RemoteEndpoint = remoteEndpoint
            };
        }

        public static OperationContext CreateSearchContext(string bucketName, string remoteEndpoint)
        {
            return new OperationContext(RequestTracing.ServiceIdentifier.Search)
            {
                BucketName = bucketName,
                RemoteEndpoint = remoteEndpoint
            };
        }

        public static OperationContext CreateAnalyticsContext(string contextId, string bucknameName, string remoteEndpoint)
        {
            return new OperationContext(RequestTracing.ServiceIdentifier.Analytics, contextId)
            {
                BucketName = bucknameName,
                RemoteEndpoint = remoteEndpoint
            };
        }
    }
}
