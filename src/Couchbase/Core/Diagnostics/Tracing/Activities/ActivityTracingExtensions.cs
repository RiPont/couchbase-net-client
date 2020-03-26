using System;
using System.Diagnostics;
using Couchbase.Utils;

namespace Couchbase.Core.Diagnostics.Tracing.Activities
{
    internal static class ActivityTracingExtensions
    {
        private const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;

        internal static long ToMicroseconds(this TimeSpan duration)
        {
            return duration.Ticks / TicksPerMicrosecond;
        }

        internal static string ToTagValue(this long n)
        {
            return n.ToString("D", System.Globalization.CultureInfo.InvariantCulture);
        }

        internal static Activity AddTag(this Activity activity, string key, TimeSpan duration)
        {
            return activity.AddTag(key, duration.ToMicroseconds().ToTagValue());
        }

        internal static Activity AddTag(this Activity activity, string key, bool boolVal)
        {
            return activity.AddTag(key, boolVal ? "true" : "false");
        }

        internal static Activity WithIgnoreTag(this Activity activity)
        {
            return activity.AddTag(CouchbaseTags.Ignore, true);
        }

        internal static Activity AddDefaultTags(this Activity activity)
        {
            return activity
                .AddTag(CouchbaseTags.OpenTelemetry.Component, ClientIdentifier.GetClientDescription())
                .AddTag(CouchbaseTags.OpenTelemetry.DbType, CouchbaseTags.DbTypeCouchbase)
                .AddTag(CouchbaseTags.OpenTelemetry.SpanKind, CouchbaseTags.OpenTelemetry.SpanKindClient);
        }

        internal static bool TryGetTagValue(this Activity activity, string key, out string val)
        {
            val = null;

            // O(n) is inescapable, given the interface we have into Activity.Tags
            foreach (var kvp in activity.Tags)
            {
                if (kvp.Key.Equals(key, StringComparison.Ordinal))
                {
                    val = kvp.Value;
                    return true;
                }
            }

            return false;
        }
    }
}
