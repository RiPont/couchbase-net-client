using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Core;
using Couchbase.Core.Diagnostics.Metrics;
using Couchbase.Core.Diagnostics.Tracing;
using Couchbase.Core.Exceptions;
using Couchbase.Core.IO.HTTP;
using Couchbase.Core.IO.Serializers;
using Couchbase.Core.Logging;
using Couchbase.Management.Query;
using Couchbase.Query;
using Couchbase.UnitTests.Helpers;
using Couchbase.UnitTests.Utils;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Couchbase.UnitTests.Management
{
    public class QueryIndexManagerTests
    {
        [Fact]
        public async Task Test_GetAllIndexesAsync()
        {
            using var response = ResourceHelper.ReadResourceAsStream(@"Documents\Query\Management\query-index-partition-response.json");

            var buffer = new byte[response.Length];
            response.Read(buffer, 0, buffer.Length);

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(buffer)
            });

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:8091")
            };
            var httpClientFactory = new MockHttpClientFactory(httpClient);

            var mockServiceUriProvider = new Mock<IServiceUriProvider>();
            mockServiceUriProvider
                .Setup(m => m.GetRandomQueryUri())
                .Returns(new Uri("http://localhost:8093"));

            var serializer = new DefaultSerializer();
            var client = new QueryClient(httpClientFactory, mockServiceUriProvider.Object, serializer,
                new Mock<ILogger<QueryClient>>().Object, NoopRequestTracer.Instance);

            var manager = new QueryIndexManager(client, new Mock<ILogger<QueryIndexManager>>().Object,
                new Redactor(new ClusterOptions()));

            var result =  await manager.GetAllIndexesAsync(It.IsAny<string>());

            var queryIndices = result as QueryIndex[] ?? result.ToArray();
            var rowWithPartition = queryIndices.FirstOrDefault(x => x.Partition == "HASH(`_type`)");
            Assert.NotNull(rowWithPartition);

            var rowWithCondition = queryIndices.FirstOrDefault(x => x.Condition == "(`_type` = \"User\")");
            Assert.NotNull(rowWithCondition);

            var rowWithIndexKey = queryIndices.FirstOrDefault(x=>x.IndexKey.Contains("`airportname`"));
            Assert.Equal("`airportname`", rowWithIndexKey.IndexKey.First());
        }

        [Fact]
        public async Task CreateIndexAsync_IgnoreIfExists_False_Do_Not_Throw_IndexExistsException()
        {
            var manager = CreateManager();

            await manager.CreateIndexAsync(It.IsAny<string>(), It.IsAny<string>(), new List<string>(), CreateQueryIndexOptions.Default.IgnoreIfExists(true));
        }

        [Fact]
        public async Task CreateIndexAsync_IgnoreIfExists_True_Throw_IndexExistsException()
        {
            var manager = CreateManager();

            await Assert.ThrowsAsync<IndexExistsException>(async () => await manager.CreateIndexAsync(It.IsAny<string>(), It.IsAny<string>(), new List<string>(), CreateQueryIndexOptions.Default.IgnoreIfExists(false)));
        }

        [Fact]
        public async Task CreatePrimaryIndexAsync_IgnoreIfExists_False_Do_Not_Throw_IndexExistsException()
        {
            var manager = CreateManager();

            await manager.CreatePrimaryIndexAsync(It.IsAny<string>(), CreatePrimaryQueryIndexOptions.Default.IgnoreIfExists(true));
        }

        [Fact]
        public async Task CreatePrimaryIndexAsync_IgnoreIfExists_True_Throw_IndexExistsException()
        {
            var manager = CreateManager();

           await Assert.ThrowsAsync<IndexExistsException>(async ()=>await manager.CreatePrimaryIndexAsync(It.IsAny<string>(), CreatePrimaryQueryIndexOptions.Default.IgnoreIfExists(false)));
        }

        [Fact]
        public async Task DropPrimaryIndexAsync_IgnoreIfExists_False_Do_Not_Throw_IndexExistsException()
        {
            var manager = CreateManager();

            await manager.DropPrimaryIndexAsync(It.IsAny<string>(), DropPrimaryQueryIndexOptions.Default.IgnoreIfExists(true));
        }

        [Fact]
        public async Task DropPrimaryIndexAsync_IgnoreIfExists_True_Throw_IndexExistsException()
        {
            var manager = CreateManager();

            await Assert.ThrowsAsync<IndexExistsException>(async () => await manager.DropPrimaryIndexAsync(It.IsAny<string>(), DropPrimaryQueryIndexOptions.Default.IgnoreIfExists(false)));
        }

        [Fact]
        public async Task DropIndexAsync_IgnoreIfExists_False_Do_Not_Throw_IndexExistsException()
        {
            var manager = CreateManager();

            await manager.DropIndexAsync(It.IsAny<string>(), It.IsAny<string>(), DropQueryIndexOptions.Default.IgnoreIfExists(true));
        }

        [Fact]
        public async Task DropIndexAsync_IgnoreIfExists_True_Throw_IndexExistsException()
        {
            var manager = CreateManager();

            await Assert.ThrowsAsync<IndexExistsException>(async () => await manager.DropIndexAsync(It.IsAny<string>(), It.IsAny<string>(), DropQueryIndexOptions.Default.IgnoreIfExists(false)));
        }

        private QueryIndexManager CreateManager()
        {
            using var response = ResourceHelper.ReadResourceAsStream(@"Documents\Query\Management\query-create-primary-index-exists-5000.json");

            var buffer = new byte[response.Length];
            response.Read(buffer, 0, buffer.Length);

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new ByteArrayContent(buffer)
            });

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:8093")
            };
            var httpClientFactory = new MockHttpClientFactory(httpClient);

            var mockServiceUriProvider = new Mock<IServiceUriProvider>();
            mockServiceUriProvider
                .Setup(m => m.GetRandomQueryUri())
                .Returns(new Uri("http://localhost:8093"));

            var serializer = new DefaultSerializer();
            var client = new QueryClient(httpClientFactory, mockServiceUriProvider.Object, serializer,
                new Mock<ILogger<QueryClient>>().Object, NoopRequestTracer.Instance);

            return new QueryIndexManager(client, new Mock<ILogger<QueryIndexManager>>().Object,
                new Redactor(new ClusterOptions()));
        }
    }
}
