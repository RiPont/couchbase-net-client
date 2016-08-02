﻿using System;
using System.Runtime.Serialization;

namespace Couchbase
{
    /// <summary>
    /// Thrown when an attempt is made to replace or get a document that does not exist.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class DocumentDoesNotExistException : Exception
    {
        public DocumentDoesNotExistException()
        {
        }

        public DocumentDoesNotExistException(string message) : base(message)
        {
        }

        public DocumentDoesNotExistException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DocumentDoesNotExistException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
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
