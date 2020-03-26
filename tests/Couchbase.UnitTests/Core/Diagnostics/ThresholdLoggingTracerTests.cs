using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Core.Diagnostics.Tracing;
using Couchbase.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Couchbase.UnitTests.Core.Diagnostics
{
    public class ThresholdLoggingTracerTests
    {
        [Fact]
        public async Task Can_add_lots_of_spans_concurrently()
        {
            var mockLogger = new Mock<ILogger<ThresholdLoggingTracer>>();
            var tracer = new ThresholdLoggingTracer(mockLogger.Object)
            {
                KvThreshold = 0 // really low threshold so all spans are logged
            };

            var tasks = Enumerable.Range(1, 1000).Select(x =>
            {
                var span = tracer.StartRootSpan(x.ToString(), x.ToString(), thresholdUs: 0);
                span.AddTag(CouchbaseTags.Service, CouchbaseTags.ServiceKv);
                return Task.FromResult(span);
            }).ToList();

            await Task.Delay(1000);

            var loopResult = Parallel.ForEach(tasks, async t =>
            {
                var disposable = await t;
                disposable.Dispose();
            });

            await Task.WhenAll(tasks);

            // wait for tasks to finish, gauranteeing Dispose is called.
            await Task.Delay(2000);

            // check all items made it into sample
            Assert.Equal(tasks.Count, tracer.TotalSummaryCount);

        }

        [Fact]
        public async Task Query_span_triggers_threshold_appropriately()
        {
            var mockLogger = new Mock<ILogger<ThresholdLoggingTracer>>();
            var tracer = new ThresholdLoggingTracer(mockLogger.Object)
            {
                N1qlThreshold = 1000000,
                Interval = 1000
            };

            using (var querySpanShort = tracer.StartRootQuerySpan("SHORT QUERY TEST", new QueryOptions()))
            using (var childSpanShort = querySpanShort.StartChild("fictitious_child_operation"))
            {
                // no wait, shouldn't trigger
            }

            // wait well past the reporting interval
            await Task.Delay(1000);

            Assert.Equal(0, tracer.TotalSummaryCount);

            using (var querySpanLong = tracer.StartRootQuerySpan("LONG QUERY TEST", new QueryOptions()))
            using (var childSpanLong = querySpanLong.StartChild("fictitious_child_operation"))
            {
                // task takes way longer than the threshold of 1000us
                await Task.Delay(2000);
            }

            // wait well past the reporting interval
            await Task.Delay(1000);

            Assert.Equal(0, tracer.TotalSummaryCount);
        }

        [Fact]
        public async Task Parent_child_relationship_is_correct_across_tasks()
        {
            var mockLogger = new Mock<ILogger<ThresholdLoggingTracer>>();
            var tracer = new ThresholdLoggingTracer(mockLogger.Object);
            var outerStarted = new SemaphoreSlim(1);
            var innerStarted = new SemaphoreSlim(1);

            var t1 = Task.Run(async () =>
            {
                using (var rootSpan = tracer.StartRootSpan("outer", "outer.id"))
                {
                    Assert.Null(rootSpan.ParentId);
                    outerStarted.Release();

                    // wait for the semaphore to be released by the second root span.
                    await innerStarted.WaitAsync();

                    // wait more so the other task finishes first.
                    await Task.Delay(250);
                    using (var childSpan = rootSpan.StartChild("outer.child"))
                    {
                        Assert.Equal(rootSpan.ActivityId, childSpan.ParentId);
                    }
                }
            });

            var t2 = Task.Run(async () =>
            {
                await outerStarted.WaitAsync();
                using (var rootSpan = tracer.StartRootSpan("inner", "inner.id"))
                {
                    innerStarted.Release();
                    Assert.Null(rootSpan.ParentId);
                    using (var childSpan = rootSpan.StartChild("inner.child"))
                    {
                        Assert.Equal(rootSpan.ActivityId, childSpan.ParentId);
                    }
                }
            });

            await t1;
            await t2;
        }
    }
}
