using System;
using Couchbase.Core.IO.Converters;
using Couchbase.Utils;

namespace Couchbase.Core.IO.Operations
{
    internal class Get<T> : OperationBase<T>
    {
        public override OpCode OpCode => OpCode.Get;

        public override void WriteExtras(OperationBuilder builder)
        {
        }

        public override void WriteBody(OperationBuilder builder)
        {
        }

        public override void ReadExtras(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length > Header.ExtrasOffset)
            {
                var format = new byte();
                var flags = buffer[Header.ExtrasOffset];
                BitUtils.SetBit(ref format, 0, BitUtils.GetBit(flags, 0));
                BitUtils.SetBit(ref format, 1, BitUtils.GetBit(flags, 1));
                BitUtils.SetBit(ref format, 2, BitUtils.GetBit(flags, 2));
                BitUtils.SetBit(ref format, 3, BitUtils.GetBit(flags, 3));

                var compression = new byte();
                BitUtils.SetBit(ref compression, 4, BitUtils.GetBit(flags, 4));
                BitUtils.SetBit(ref compression, 5, BitUtils.GetBit(flags, 5));
                BitUtils.SetBit(ref compression, 6, BitUtils.GetBit(flags, 6));

                var typeCode = (TypeCode)(ByteConverter.ToUInt16(buffer.Slice(26)) & 0xff);
                Format = (DataFormat)format;
                Compression = (Compression)compression;
                Flags.DataFormat = Format;
                Flags.Compression = Compression;
                Flags.TypeCode = typeCode;
            }
        }

        public override IOperation Clone()
        {
            var cloned = new Get<T>
            {
                Key = Key,
                ReplicaIdx = ReplicaIdx,
                Content = Content,
                Transcoder = Transcoder,
                VBucketId = VBucketId,
                Opaque = Opaque,
                Attempts = Attempts,
                Cas = Cas,
                CreationTime = CreationTime,
                LastConfigRevisionTried = LastConfigRevisionTried,
                BucketName = BucketName,
                ErrorCode = ErrorCode
            };
            return cloned;
        }

        public override bool Idempotent { get; } = true;

        public override bool CanRetry()
        {
            return ErrorCode == null || ErrorMapRequestsRetry();
        }

        public override bool RequiresKey => true;
    }
}

#region [ License information ]

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

#endregion [ License information ]
