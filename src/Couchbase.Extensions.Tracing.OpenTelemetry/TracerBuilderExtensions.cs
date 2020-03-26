using OpenTelemetry.Trace.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Couchbase.Extensions.Tracing.OpenTelemetry
{
    public static class TracerBuilderExtensions
    {
        /// <summary>
        /// Enables the incoming requests automatic data collection.
        /// </summary>
        /// <param name="builder">Trace builder to use.</param>
        /// <returns>The instance of <see cref="TracerBuilder"/> to chain the calls.</returns>
        public static TracerBuilder AddCouchbaseCollector(this TracerBuilder builder, CouchbaseCollectorOptions options = null)
        {
            builder = builder ?? throw new ArgumentNullException(nameof(builder));

            return builder.AddCollector(t => new CouchbaseCollector(t, options ?? CouchbaseCollectorOptions.Default));
        }
    }
}
