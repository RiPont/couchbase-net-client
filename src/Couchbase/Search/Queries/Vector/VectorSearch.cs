#nullable enable
using System.Collections.Generic;
using System.Linq;
using Couchbase.Core.Compatibility;

namespace Couchbase.Search.Queries.Vector;

[InterfaceStability(Level.Volatile)]
public sealed record VectorSearch(IEnumerable<VectorQuery> VectorQueries, VectorSearchOptions? Options)
{
    public static VectorSearch Create(VectorQuery vectorQuery, VectorSearchOptions? options = null) =>
        new VectorSearch(new[] { vectorQuery }, options);
}

[InterfaceStability(Level.Volatile)]
public sealed record VectorSearchOptions(VectorQueryCombination? VectoryQueryCombination = null);

[InterfaceStability(Level.Volatile)]
public enum VectorQueryCombination
{
    And = 0,
    Or = 1,
}
