using System;
using System.Text.Json.Serialization;

namespace Couchbase.Core.IO.Operations.Errors
{
    /// <summary>
    /// A retry strategy to be used for a given <see cref="ErrorCode"/>.
    /// </summary>
    public class RetrySpec
    {
        /// <summary>
        /// Gets or sets the <see cref="RetryStrategy"/>.
        /// </summary>
        [JsonPropertyName("strategy")]
        public RetryStrategy Strategy { get; set; }

        /// <summary>
        /// Gets or sets the base interval in ms.
        /// </summary>
        [JsonPropertyName("interval")]
        public int Interval { get; set; }

        /// <summary>
        /// Gets or sets the maximum retry interval.
        /// </summary>
        [JsonPropertyName("ceil")]
        public int? Ceiling { get; set; }

        /// <summary>
        /// Gets or sets the value to be added to the first interval.
        /// </summary>
        [JsonPropertyName("after")]
        public int? FirstRetryDelay { get; set; }

        /// <summary>
        /// Gets or sets the maximum duration for retries or will timeout.
        /// </summary>
        [JsonPropertyName("max-duration")]
        public int? RetryTimeout { get; set; }

        public RetrySpec()
        {
            Strategy = RetryStrategy.None;
        }

        /// <summary>
        /// Gets the next interval using the retry strategy.
        /// </summary>
        /// <param name="attempts">The attempts.</param>
        /// <returns>The next interval to wait before the next</returns>
        public int GetNextInterval(uint attempts)
        {
            var adjustedAttempts = attempts - 1;

            var nextInterval = 0;
            if (FirstRetryDelay.HasValue && adjustedAttempts <= 0)
            {
                nextInterval = FirstRetryDelay.Value;
            }

            switch (Strategy)
            {
                case RetryStrategy.Constant:
                    nextInterval += Interval;
                    break;
                case RetryStrategy.Linear:
                    nextInterval += (int)adjustedAttempts * Interval;
                    break;
                case RetryStrategy.Exponential:
                    nextInterval += (int) Math.Pow(Interval, adjustedAttempts);
                    break;
            }

            if (Ceiling.HasValue && nextInterval > Ceiling)
            {
                nextInterval = Ceiling.Value;
            }

            return nextInterval;
        }
    }
}

#region [ License information          ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2017 Couchbase, Inc.
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
