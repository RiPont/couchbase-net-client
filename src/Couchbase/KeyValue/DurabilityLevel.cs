using System.ComponentModel;

namespace Couchbase.KeyValue
{
    /// <summary>
    /// The required number of nodes which the mutation must be replicated to (and/or persisted to) for durability requirements to be met. Possible values:
    /// </summary>
    public enum DurabilityLevel : byte
    {
        /// <summary>
        /// No durability requirements.
        /// </summary>
        [Description("none")]
        None = 0x00,

        /// <summary>
        /// Mutation must be replicated to (i.e. held in memory of that node) a majority of the configured nodes of the bucket.
        /// </summary>
        [Description("majority")]
        Majority = 0x01,

        /// <summary>
        /// Same as majority, but additionally persisted to the active node.
        /// </summary>
        [Description("majorityAndPersistActive")]
        MajorityAndPersistToActive = 0x02,

        /// <summary>
        /// Mutation must be persisted to (i.e. written and fsync'd to disk) a majority of the configured nodes of the bucket.
        /// </summary>
        [Description("persistToMajority")]
        PersistToMajority = 0x03
    }
}


/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2021 Couchbase, Inc.
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
