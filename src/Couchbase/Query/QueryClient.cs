using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Couchbase.Core;
using Couchbase.Core.Configuration.Server;
using Couchbase.Core.Diagnostics.Tracing;
using Couchbase.Core.Diagnostics.Tracing.Activities;
using Couchbase.Core.Exceptions;
using Couchbase.Core.Exceptions.Query;
using Couchbase.Core.IO.HTTP;
using Couchbase.Core.IO.Serializers;
using Couchbase.Core.Logging;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Couchbase.Query
{
    /// <summary>
    /// A <see cref="QueryClient" /> implementation for executing N1QL queries against a Couchbase Server.
    /// </summary>
    internal class QueryClient : HttpServiceBase, IQueryClient
    {
        internal const string Error5000MsgQueryPortIndexNotFound = "queryport.indexNotFound";

        private readonly ConcurrentDictionary<string, QueryPlan> _queryCache = new ConcurrentDictionary<string, QueryPlan>();
        private readonly ITypeSerializer _queryPlanSerializer = new DefaultSerializer();
        private readonly IServiceUriProvider _serviceUriProvider;
        private readonly ITypeSerializer _serializer;
        private readonly ILogger<QueryClient> _logger;
        private readonly IActivityTracer _tracer;
        internal bool EnhancedPreparedStatementsEnabled;

        public QueryClient(
            CouchbaseHttpClient httpClient,
            IServiceUriProvider serviceUriProvider,
            ITypeSerializer serializer,
            ILogger<QueryClient> logger,
            IActivityTracer tracer)
            : base(httpClient)
        {
            _serviceUriProvider = serviceUriProvider ?? throw new ArgumentNullException(nameof(serviceUriProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        }

        /// <inheritdoc />
        public int InvalidateQueryCache()
        {
            var count = _queryCache.Count;
            _queryCache.Clear();
            return count;
        }

        /// <inheritdoc />
        public async Task<IQueryResult<T>> QueryAsync<T>(string statement, QueryOptions options)
        {
            if (string.IsNullOrEmpty(options.CurrentContextId))
            {
                options.ClientContextId(Guid.NewGuid().ToString());
            }

            using (var rootSpan = _tracer.StartRootQuerySpan(statement, options))
            {
                // does this query use a prepared plan?
                if (options.IsAdHoc)
                {
                    // don't use prepared plan, execute query directly
                    options.Statement(statement);
                    return await ExecuteQuery<T>(options, options.Serializer ?? _serializer, rootSpan).ConfigureAwait(false);
                }

                // try find cached query plan
                if (_queryCache.TryGetValue(statement, out var queryPlan))
                {
                    // if an upgrade has happened, don't use query plans that have an encoded plan
                    if (!EnhancedPreparedStatementsEnabled || string.IsNullOrWhiteSpace(queryPlan.EncodedPlan))
                    {
                        // plan is valid, execute query with it
                        options.Prepared(queryPlan, statement);
                        return await ExecuteQuery<T>(options, options.Serializer ?? _serializer, rootSpan).ConfigureAwait(false);
                    }

                    // entry is stale, remove from cache
                    _queryCache.TryRemove(statement, out _);
                }

                // TODO:  As metioned in the RFC for SDK3 Response Time Observability, we can't track a span for response_decoding because
                //        the change to ContentAs means we have no control over when and if the response is actually read.  However, that
                //        also means we can't use the old method of recording for SetPeerLatencyTag and the N1QL Profiling Baggage.

                // TODO:  Should we add spans for preparing statements separately, with request_encoding and server_dispatch as child spans?
                //        Currently, this will appear as one "n1ql" operation with multiple request_encoding and server_dispatch when doing
                //        prepare-and-execute.
                // create prepared statement
                var prepareStatement = statement;
                if (!prepareStatement.StartsWith("PREPARE ", StringComparison.InvariantCultureIgnoreCase))
                {
                    prepareStatement = $"PREPARE {statement}";
                }

                // set prepared statement
                options.Statement(prepareStatement);

                // server supports combined prepare & execute
                if (EnhancedPreparedStatementsEnabled)
                {
                    // execute combined prepare & execute query
                    options.AutoExecute(true);
                    IQueryResult<T> result;
                    using (var prepareAndExecuteSpan = rootSpan.StartChild(CouchbaseOperationNames.PrepareAndExecute))
                    {
                        result = await ExecuteQuery<T>(options, options.Serializer ?? _serializer, prepareAndExecuteSpan).ConfigureAwait(false);
                    }

                    // add/replace query plan name in query cache
                    if (result is StreamingQueryResult<T> streamingResult) // NOTE: hack to not make 'PreparedPlanName' property public
                    {
                        var plan = new QueryPlan { Name = streamingResult.PreparedPlanName, Text = statement };
                        _queryCache.AddOrUpdate(statement, plan, (k, p) => plan);
                    }

                    return result;
                }

                // older style, prepare then execute
                IQueryResult<QueryPlan> preparedResult;
                using (var prepareSpan = rootSpan.StartChild(CouchbaseOperationNames.Prepare))
                {
                    preparedResult = await ExecuteQuery<QueryPlan>(options, _queryPlanSerializer, rootSpan).ConfigureAwait(false);
                    queryPlan = await preparedResult.FirstAsync().ConfigureAwait(false);
                }

                // add plan to cache and execute
                _queryCache.TryAdd(statement, queryPlan);
                options.Prepared(queryPlan, statement);

                // execute query using plan
                return await ExecuteQuery<T>(options, options.Serializer ?? _serializer, rootSpan).ConfigureAwait(false);
            }
        }

        private async Task<IQueryResult<T>> ExecuteQuery<T>(QueryOptions options, ITypeSerializer serializer, ActivitySpan rootSpan)
        {
            // try get Query node
            var queryUri = _serviceUriProvider.GetRandomQueryUri();
            string body;
            using (rootSpan.StartChild(CouchbaseOperationNames.RequestEncoding))
            {
                body = options.GetFormValuesAsJson();
            }

            QueryResultBase<T> queryResult;
            using (var content = new StringContent(body, System.Text.Encoding.UTF8, MediaType.Json))
            {
                try
                {
                    HttpResponseMessage response;
                    using (rootSpan.StartChild(CouchbaseOperationNames.DispatchToServer))
                    {
                        response = await HttpClient.PostAsync(queryUri, content, options.Token).ConfigureAwait(false);
                    }

                    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                    if (serializer is IStreamingTypeDeserializer streamingDeserializer)
                    {
                        queryResult = new StreamingQueryResult<T>(stream, streamingDeserializer);
                    }
                    else
                    {
                        queryResult = new BlockQueryResult<T>(stream, serializer);
                    }

                    queryResult.HttpStatusCode = response.StatusCode;
                    queryResult.Success = response.StatusCode == HttpStatusCode.OK;

                    //read the header and stop when we reach the queried rows
                    await queryResult.InitializeAsync(options.Token).ConfigureAwait(false);

                    if (response.StatusCode != HttpStatusCode.OK || queryResult.MetaData?.Status != QueryStatus.Success)
                    {
                        _logger.LogDebug("Request {currentContextId} has failed because {status}.",
                            options.CurrentContextId, queryResult.MetaData?.Status);

                        if (queryResult.ShouldRetry())
                        {
                            return queryResult;
                        }
                        var context = new QueryErrorContext
                        {
                            ClientContextId = options.CurrentContextId,
                            Parameters = options.GetAllParametersAsJson(),
                            Statement = options.ToString(),
                            Message = queryResult.Message,
                            Errors = queryResult.Errors,
                            HttpStatus = response.StatusCode,
                            QueryStatus = queryResult.MetaData?.Status ?? QueryStatus.Fatal
                        };

                        if (queryResult.MetaData?.Status == QueryStatus.Timeout)
                        {
                            if (options.IsReadOnly)
                            {
                                throw new AmbiguousTimeoutException
                                {
                                    Context = context
                                };
                            }

                            throw new UnambiguousTimeoutException
                            {
                                Context = context
                            };
                        }
                        queryResult.ThrowExceptionOnError(context);
                    }
                }
                catch (OperationCanceledException e)
                {
                    var context = new QueryErrorContext
                    {
                        ClientContextId = options.CurrentContextId,
                        Parameters = options.GetAllParametersAsJson(),
                        Statement = options.ToString(),
                        HttpStatus = HttpStatusCode.RequestTimeout,
                        QueryStatus = QueryStatus.Fatal
                    };

                    _logger.LogDebug(LoggingEvents.QueryEvent, e, "Request timeout.");
                    if (options.IsReadOnly)
                    {
                        throw new UnambiguousTimeoutException("The query was timed out via the Token.", e)
                        {
                            Context = context
                        };
                    }
                    throw new AmbiguousTimeoutException("The query was timed out via the Token.", e)
                    {
                        Context = context
                    };
                }
                catch (HttpRequestException e)
                {
                    _logger.LogDebug(LoggingEvents.QueryEvent, e, "Request canceled");

                    var context = new QueryErrorContext
                    {
                        ClientContextId = options.CurrentContextId,
                        Parameters = options.GetAllParametersAsJson(),
                        Statement = options.ToString(),
                        HttpStatus = HttpStatusCode.RequestTimeout,
                        QueryStatus = QueryStatus.Fatal
                    };

                    throw new RequestCanceledException("The query was canceled.", e)
                    {
                        Context = context
                    };
                }
            }
            _logger.LogDebug($"Request {options.CurrentContextId} has succeeded.");
            return queryResult;
        }

        internal void UpdateClusterCapabilities(ClusterCapabilities clusterCapabilities)
        {
            if (!EnhancedPreparedStatementsEnabled && clusterCapabilities.EnhancedPreparedStatementsEnabled)
            {
                EnhancedPreparedStatementsEnabled = true;
                _logger.LogInformation("Enabling Enhanced Prepared Statements");
            }
        }
    }
}

#region [ License information          ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2014 Couchbase, Inc.
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
