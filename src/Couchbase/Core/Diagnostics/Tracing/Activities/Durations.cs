using System;
using System.Threading;

namespace Couchbase.Core.Diagnostics.Tracing.Activities
{
    internal class Durations
    {
        private long _encodingDurationUs = 0;
        private long _decodingDurationUs = 0;
        private long _dispatchDurationUs = 0;
        private long _serverDurationUs = 0;

        internal long EncodingDurationUs => Interlocked.Read(ref _encodingDurationUs);

        internal long? DecodingDurationUs => ReadOrNull(ref _decodingDurationUs);

        internal long DispatchDurationUs => Interlocked.Read(ref _dispatchDurationUs);

        internal long? ServerDurationUs => ReadOrNull(ref _serverDurationUs);

        internal void AddEncodingDuration(TimeSpan duration)
        {
            var durationUs = duration.ToMicroseconds();
            Interlocked.Add(ref _encodingDurationUs, durationUs);
        }

        internal void AddDecodingDuration(TimeSpan duration)
        {
            var durationUs = duration.ToMicroseconds();
            Interlocked.Add(ref _decodingDurationUs, durationUs);
        }

        internal void AddDispatchgDuration(TimeSpan duration)
        {
            var durationUs = duration.ToMicroseconds();
            Interlocked.Add(ref _dispatchDurationUs, durationUs);
        }

        internal void AddServerDuration(TimeSpan duration)
        {
            var durationUs = duration.ToMicroseconds();
            AddServerDuration(durationUs);
        }

        internal void AddServerDuration(long durationUs)
        {
            Interlocked.Add(ref _serverDurationUs, durationUs);
        }

        private long? ReadOrNull(ref long source)
        {
            var val = Interlocked.Read(ref source);
            return val != 0 ? val : (long?)null;
        }
    }
}
