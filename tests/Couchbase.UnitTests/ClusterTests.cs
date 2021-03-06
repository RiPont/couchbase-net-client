using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Analytics;
using Couchbase.Core.Retry;
using Couchbase.Query;
using Moq;
using Xunit;

namespace Couchbase.UnitTests
{
    public class ClusterTests
    {
        #region ctor

        [Fact]
        public void ctor_Throws_InvalidConfigurationException_When_Credentials_Not_Provided()
        {
            Assert.Throws<InvalidConfigurationException>(() => new Cluster(new ClusterOptions().WithConnectionString("couchbase://localhost")));
        }

        #endregion

        #region QueryAsync

        [Fact]
        public async Task QueryAsync_WithObject_Success()
        {
            // Arrange

            var queryResult = new Mock<IQueryResult<TestClass>>();
            queryResult
                .SetupGet(m => m.MetaData)
                .Returns(new QueryMetaData()
                {
                    Status = QueryStatus.Success
                });
            queryResult
                .SetupGet(m => m.RetryReason)
                .Returns(RetryReason.NoRetry);

                var queryClient = new Mock<IQueryClient>();
            queryClient
                .Setup(m => m.QueryAsync<TestClass>(It.IsAny<string>(), It.IsAny<QueryOptions>()))
                .ReturnsAsync(queryResult.Object);

            var options = new ClusterOptions().WithCredentials("u", "p")
                .WithConnectionString("couchbase://localhost");

            var cluster = new Mock<Cluster>(options)
            {
                CallBase = true
            };
            cluster
                .Setup(m => m.EnsureBootstrapped())
                .Returns(Task.CompletedTask);
            cluster.Object.LazyQueryClient = new Lazy<IQueryClient>(() => queryClient.Object);

            // Act

            var result = await cluster.Object.QueryAsync<TestClass>("SELECT * FROM `default`").ConfigureAwait(false);

            // Assert

            Assert.Equal(queryResult.Object, result);
        }

        #endregion

        #region AnalyticsQueryAsync

        [Fact]
        public async Task AnalyticsQueryAsync_WithObject_Success()
        {
            // Arrange

            var analyticsResult = new Mock<IAnalyticsResult<TestClass>>();
            analyticsResult
                .SetupGet(m => m.MetaData)
                .Returns(new AnalyticsMetaData
                {
                    Status = AnalyticsStatus.Success
                });
            analyticsResult
                .SetupGet(m => m.RetryReason)
                .Returns(RetryReason.NoRetry);

            var analyticsClient = new Mock<IAnalyticsClient>();
            analyticsClient
                .Setup(m => m.QueryAsync<TestClass>(It.IsAny<IAnalyticsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(analyticsResult.Object);

            var options = new ClusterOptions().WithCredentials("u", "p")
                .WithConnectionString("couchbase://localhost");

            var cluster = new Mock<Cluster>(options)
            {
                CallBase = true
            };
            cluster
                .Setup(m => m.EnsureBootstrapped())
                .Returns(Task.CompletedTask);
            cluster.Object.LazyAnalyticsClient = new Lazy<IAnalyticsClient>(() => analyticsClient.Object);

            // Act

            var result = await cluster.Object.AnalyticsQueryAsync<TestClass>("SELECT * FROM `default`").ConfigureAwait(false);

            // Assert

            Assert.Equal(analyticsResult.Object, result);
        }

        #endregion

        #region Helpers

        public class TestClass
        {
            public string Name { get; set; }
        }

        #endregion
    }
}
