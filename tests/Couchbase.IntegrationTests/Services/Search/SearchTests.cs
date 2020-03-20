using System;
using System.Threading.Tasks;
using Couchbase.Core.Retry.Search;
using Couchbase.IntegrationTests.Fixtures;
using Couchbase.Search;
using Couchbase.Search.Queries.Simple;
using Xunit;

namespace Couchbase.IntegrationTests.Services.Search
{
    public class SearchTests : IClassFixture<ClusterFixture>
    {
        private readonly ClusterFixture _fixture;
        private const string IndexName = "travel-sample-index";

        public SearchTests(ClusterFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Test_Async()
        {
            var cluster = await _fixture.GetCluster().ConfigureAwait(false);
            var results = await cluster.SearchQueryAsync(IndexName,
                new MatchQuery("inn"),
                new SearchOptions().Limit(10).Timeout(TimeSpan.FromMilliseconds(10000))).
                ConfigureAwait(false);

            Assert.True(results.Hits.Count > 0);
        }

        [Fact]
        public async Task Test_Async_With_HighLightStyle_Html_And_Fields()
        {
            var cluster = await _fixture.GetCluster().ConfigureAwait(false);
            var results = await cluster.SearchQueryAsync(IndexName,
                new MatchQuery("inn"),
                new SearchOptions().Limit(10).Timeout(TimeSpan.FromMilliseconds(10000))
                    .Highlight(HighLightStyle.Html, "inn")
            ).ConfigureAwait(false);

            Assert.True(results.Hits.Count > 0);
        }

        [Fact]
        public async Task Facets_Async_Success()
        {
            var cluster = await _fixture.GetCluster().ConfigureAwait(false);
            var results = await cluster.SearchQueryAsync(IndexName,
                new MatchQuery("inn"),
                new SearchOptions().Facets(
                    new TermFacet("termfacet", "name", 1),
                    new DateRangeFacet("daterangefacet", "thefield", 10).AddRange(DateTime.Now, DateTime.Now.AddDays(1)),
                    new NumericRangeFacet("numericrangefacet", "thefield", 2).AddRange(2.2f, 3.5f)
                )
            ).ConfigureAwait(false);
            Assert.Equal(3, results.Facets.Count);
        }
    }
}

#region [ License information          ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2015 Couchbase, Inc.
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
