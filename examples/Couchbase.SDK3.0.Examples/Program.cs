using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Extensions.Tracing.OpenTelemetry;
using Couchbase.KeyValue;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTelemetry.Trace.Configuration;
using OpenTelemetry.Trace.Export;

namespace Couchbase.SDK3._0.Examples
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            try
            {
                var loggerFactory = new LoggerFactory()
                    .AddConsole();

                var tracerFactory = TracerFactory.Create(builder =>
                {
                    builder.AddCouchbaseCollector(new CouchbaseCollectorOptions() { Verbose = true })
                           .AddProcessorPipeline(b => b.SetExporter(new SimpleConsoleExporter()));
                });

                var clusterOptions = new ClusterOptions()
                {
                    UserName = "Administrator",
                    Password = "password"
                };

                clusterOptions.WithConnectionString("couchbase://127.0.0.1")
                              .WithLogging(loggerFactory);

                var cluster = await Cluster.ConnectAsync(clusterOptions);

                var bucket = await cluster.BucketAsync("beer-sample");
                var collection = bucket.DefaultCollection();

                await BasicCrud(collection);
                await BasicProjection(collection);
                await BasicQuery(cluster);

                ////await BasicDurability(collection);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (Debugger.IsAttached)
                {
                    throw;
                }
            }

            Console.WriteLine("Hello World!");
            Console.Read();
        }

        private static async Task BasicQuery(ICluster cluster)
        {
            var statement = "SELECT * FROM `beer-sample` WHERE type='brewery'";

            // TODO:  Using type parameter Brewery here doesn't seem to populate correctly.  Why?
            //        Using the WebUI, the results are returned as "beer-sample" roots.  Maybe that's it.
            var results = await cluster.QueryAsync<JObject>(
                statement,
                options => options.Timeout(TimeSpan.FromSeconds(70)));

            var asyncEnumerator = results.GetAsyncEnumerator();
            while (await asyncEnumerator.MoveNextAsync())
            {
                var result = asyncEnumerator.Current.ToString(Formatting.None);
                ////Console.WriteLine(result);
            }
        }

        private static async Task BasicCrud(ICouchbaseCollection collection)
        {
            var id = "brewery01";

            //upsert a new person
            var result = await collection.UpsertAsync(id, new Brewery()
            {
                address = new[] { "123 Main Street"},
                city = "Sometown",
                code = "95123",
                country = "Old Country",
                description = "The 01 Brewery is entirely fictional.",
                geo = new Location { accuracy = "ROOFTOP", lat = 73.7825m, @long = -122.393m },
                name = "The Oh One Gastropub",
                phone = "857-4309",
                state = "Heisenburg",
                updated = DateTimeOffset.UtcNow,
                website = new Uri("http://localhost/")
            }, options => options.Expiry(TimeSpan.FromDays(1)));

            var get = await collection.GetAsync(id,
                options => options.Timeout(TimeSpan.FromSeconds(10)));

            var person = get.ContentAs<Person>();
            Console.WriteLine(person.name);
        }

        private static async Task BasicDurability(ICouchbaseCollection collection)
        {
            // TODO: this is throwing "Durability Impossible", and I don't know enough about the feature to
            //       begin to know why.
            var id = "another_example_brewery";
            var result = await collection.InsertAsync(id, new Brewery
            {
                name = "Another Example Brewery",
                website = new Uri("http://localhost/?id=another_example_brewery")
            }, options =>
            {
                options.Durability(DurabilityLevel.MajorityAndPersistToActive);
                options.Timeout(TimeSpan.FromSeconds(30));
            });

            Console.WriteLine(result.Cas);
        }

        private static async Task BasicProjection(ICouchbaseCollection collection)
        {
            var id = "brewery01";

            var result = await collection.GetAsync(id,
                options => options.Projection("name", "phone", "website", "geo.lat", "geo.long"));

            var brewery = result.ContentAs<Brewery>();
            Console.WriteLine($"Name={brewery.name}, LatLong={brewery.geo.lat},{brewery.geo.@long}");
        }

        public class Brewery
        {
            public string[] address { get; set; }
            public string city { get; set; }
            public string code { get; set; }
            public string country { get; set; }
            public string description { get; set; }
            public Location geo { get; set; }
            public string name { get; set; }
            public string phone { get; set; }
            public string state { get; set; }
            public string type => "brewery";
            public DateTimeOffset updated { get; set; }
            public Uri website { get; set; }

            public override string ToString() => JObject.FromObject(this).ToString(Newtonsoft.Json.Formatting.Indented);
        }

        public class Dimensions
        {
            public int height { get; set; }
            public int weight { get; set; }
        }

        public class Location
        {
            public string accuracy { get; set; }
            public decimal lat { get; set; }
            public decimal @long { get; set; }
        }

        public class Details
        {
            public Location location { get; set; }
        }

        public class Hobby
        {
            public string type { get; set; }
            public string name { get; set; }
            public Details details { get; set; }
        }

        public class Attributes
        {
            public string hair { get; set; }
            public Dimensions dimensions { get; set; }
            public List<Hobby> hobbies { get; set; }
        }

        public class Person
        {
            public string name { get; set; }
            public int age { get; set; }
            public List<string> animals { get; set; }
            public Attributes attributes { get; set; }
            public string type => "person";
        }

        private class SimpleConsoleExporter : SpanExporter
        {
            private JsonSerializer _spanDataSerializer = new JsonSerializer();

            public SimpleConsoleExporter()
            {
                _spanDataSerializer.Converters.Add(new ValueToStringConverter<ActivitySpanId>());
                _spanDataSerializer.Converters.Add(new ValueToStringConverter<ActivityTraceId>());
                _spanDataSerializer.Converters.Add(new ValueToStringConverter<ActivityTraceFlags>());
                _spanDataSerializer.Formatting = Formatting.Indented;
            }

            public override Task<ExportResult> ExportAsync(IEnumerable<SpanData> batch, CancellationToken cancellationToken)
            {
                foreach (var spanData in batch)
                {
                    ////Console.Out.WriteLine("OpenTelemetry:" + JObject.FromObject(spanData).ToString(Formatting.Indented));
                    _spanDataSerializer.Serialize(Console.Out, spanData);
                }

                return Task.FromResult(ExportResult.Success);
            }

            public override Task ShutdownAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private class ValueToStringConverter<T> : JsonConverter<T>
        {
            public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException();
            public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer) => writer.WriteValue(value.ToString());
        }
    }
}
