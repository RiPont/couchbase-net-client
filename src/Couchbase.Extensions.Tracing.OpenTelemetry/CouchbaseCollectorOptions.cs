namespace Couchbase.Extensions.Tracing.OpenTelemetry
{
    public class CouchbaseCollectorOptions
    {
        public static readonly CouchbaseCollectorOptions Default = new CouchbaseCollectorOptions();

        // TODO: this is rather generic and we'd want more fine-grained filtering.
        public bool Verbose { get; set; }
    }
}
