using System;
using Couchbase.Core.Configuration.Server;

namespace Couchbase.Core.IO.Operations.Collections
{
    internal class GetManifest :  OperationBase<Manifest>
    {
        public override OpCode OpCode  => OpCode.GetCollectionsManifest;

        public override bool Idempotent { get; } = true;

        protected override void BeginSend()
        {
            Flags = new Flags
            {
                Compression = Compression.None,
                DataFormat = Format,
                TypeCode = TypeCode.Object
            };
        }

        public override void WriteExtras(OperationBuilder builder)
        {
        }

        public override void ReadExtras(ReadOnlySpan<byte> buffer)
        {
            //force it to treat the result as JSON for serialization
            Flags = new Flags
            {
                Compression = Compression.None,
                DataFormat = Format,
                TypeCode = TypeCode.Object
            };
        }
    }
}
