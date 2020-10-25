using System;
using System.IO;
using System.Threading.Tasks;

namespace Pixie.Core.Common.Streams
{
    internal class PXBinaryWriterAsync
    {
        private Stream innerStream;
        private MemoryStream buffer = new MemoryStream();

        public PXBinaryWriterAsync(Stream innerStream) {
            this.innerStream = innerStream;
        }

        public void Write(short value) {
            this.Write(BitConverter.GetBytes(value));
        }

        public void Write(byte value) {
            this.Write(new byte[] { value });
        }

        public void Write(bool value) {
            this.Write(value ? (byte)1 : (byte)0);
        }

        public void Write(ushort value) {
            this.Write(BitConverter.GetBytes(value));
        }

        public void Write(int value) {
            this.Write(BitConverter.GetBytes(value));
        }

        public void Write(Guid value) {
            this.Write(value.ToByteArray());
        }

        public void Write(byte[] value) {
            this.buffer.Write(value, 0, value.Length);
        }

        public async Task FlushAsync() {
            var bytes = this.buffer.ToArray();
            this.buffer = new MemoryStream();
            await this.innerStream.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
