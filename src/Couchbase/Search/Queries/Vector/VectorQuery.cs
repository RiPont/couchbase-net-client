#nullable enable
using Couchbase.Core.Compatibility;

namespace Couchbase.Search.Queries.Vector;

[InterfaceStability(Level.Volatile)]
public sealed record VectorQuery(string VectorFieldName, float[] Vector, VectorQueryOptions? Options)
{
    VectorQuery WithOptions(VectorQueryOptions options) => this with { Options = options };
}

[InterfaceStability(Level.Volatile)]
public sealed record VectorQueryOptions(uint? NumCandidates = null, float? Boost = null)
{
    VectorQueryOptions WithNumCandidates(uint numCandidates) => this with { NumCandidates = numCandidates };
    VectorQueryOptions WithBoost(float boost) => this with { Boost = boost };
}
