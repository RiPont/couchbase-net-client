namespace Couchbase.Extensions.Tracing.OpenTelemetry
{
    public class CouchbaseCollectorOptions
    {
        public static readonly CouchbaseCollectorOptions Default = new CouchbaseCollectorOptions();

        public bool Verbose { get; set; }
    }
}
