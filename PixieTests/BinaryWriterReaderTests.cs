using NUnit.Framework;
using Pixie.Core.Common.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PixieTests
{
    class BinaryWriterReaderTests
    {
        [Test]
        public async Task BasicTypesSerializationTest() {
            MemoryStream memory = new MemoryStream();

            var writer = new PXBinaryWriterAsync(memory);
            writer.Write((byte)1);
            writer.Write((short)2);
            writer.Write((ushort)3);
            writer.Write(new byte[] { 1, 2, 3 });
            await writer.FlushAsync();

            var reader = new PXBinaryReaderAsync(new MemoryStream(memory.ToArray()));
            Assert.AreEqual((byte)1, await reader.ReadByte());
            Assert.AreEqual((short)2, await reader.ReadInt16());
            Assert.AreEqual((ushort)3, await reader.ReadUInt16());
            Assert.AreEqual(new byte[] { 1, 2, 3 }, await reader.ReadBytes(3));
        }
    }
}
