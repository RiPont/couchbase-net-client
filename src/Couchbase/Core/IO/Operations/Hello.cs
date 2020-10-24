using System;
using System.Buffers;
using Couchbase.Core.IO.Converters;
using Couchbase.Utils;
using Newtonsoft.Json;

namespace Couchbase.Core.IO.Operations
{
    internal class Hello : OperationBase<ServerFeatures[]>
    {
        public override OpCode OpCode => OpCode.Helo;

        public override void WriteBody(OperationBuilder builder)
        {
            var contentLength = Content.Length;

            using (var bufferOwner = MemoryPool<byte>.Shared.Rent(contentLength * 2))
            {
                var body = bufferOwner.Memory.Span;

                for (var i = 0; i < contentLength; i++)
                {
                    ByteConverter.FromInt16((short) Content[i], body);
                    body = body.Slice(2);
                }

                builder.Write(bufferOwner.Memory.Slice(0, contentLength * 2));
            }
        }

        public override void WriteExtras(OperationBuilder builder)
        {
        }

        public override ServerFeatures[] GetValue()
        {
            var result = default(ServerFeatures[]);
            if (Success && Data.Length > 0)
            {
                try
                {
                    var buffer = Data.Span.Slice(Header.BodyOffset);
                    result = new ServerFeatures[Header.BodyLength/2];

                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = (ServerFeatures) ByteConverter.ToInt16(buffer);

                        buffer = buffer.Slice(2);
                        if (buffer.Length <= 0) break;
                    }
                }
                catch (Exception e)
                {
                    Exception = e;
                    HandleClientError(e.Message, ResponseStatus.ClientFailure);
                }
            }
            return result;
        }

        public override bool RequiresKey => true;

        internal static string BuildHelloKey(ulong connectionId)
        {
            var agent = ClientIdentifier.GetClientDescription();
            if (agent.Length > 200)
            {
                agent = agent.Substring(0, 200);
            }

            return JsonConvert.SerializeObject(new
            {
                i = ClientIdentifier.FormatConnectionString(connectionId),
                a = agent
            }, Formatting.None);
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
