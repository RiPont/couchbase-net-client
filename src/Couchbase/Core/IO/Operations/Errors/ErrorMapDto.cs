using System.Collections.Generic;
using Newtonsoft.Json;

namespace Couchbase.Core.IO.Operations.Errors
{
    /// <summary>
    /// A map of errors provided by the server that can be used to lookup messages.
    /// This is the version of <see cref="ErrorMap"/> designed for JSON deserialization.
    /// </summary>
    internal class ErrorMapDto
    {
        /// <summary>
        /// Gets or sets the version of the error map.
        /// </summary>
        [JsonProperty("version")]
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the revision of the error map.
        /// </summary>
        [JsonProperty("revision")]
        public int Revision { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of errors codes.
        /// </summary>
        [JsonProperty("errors")]
        public Dictionary<string, ErrorCode> Errors { get; set; }
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
