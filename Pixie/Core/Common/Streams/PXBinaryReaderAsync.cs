using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pixie.Core.Common.Streams
{
    internal class PXBinaryReaderAsync
    {
        public class EmptyStreamException : Exception {}

        private Stream innerStream;

        public PXBinaryReaderAsync(Stream innerStream) {
            this.innerStream = innerStream;
        }

        public async Task<short> ReadInt16(CancellationToken? cancellationToken = null) {
            return BitConverter.ToInt16(await ReadBytes(sizeof(short), cancellationToken), 0);
        }

        public async Task<byte> ReadByte(CancellationToken? cancellationToken = null) {
            return (await ReadBytes(sizeof(byte), cancellationToken))[0];
        }

        public async Task<ushort> ReadUInt16(CancellationToken? cancellationToken = null) {
            return BitConverter.ToUInt16(await ReadBytes(sizeof(ushort), cancellationToken), 0);
        }

        public async Task<byte[]> ReadBytes(int count, CancellationToken? cancellationToken = null) {
            byte[] buffer = new byte[count];
            int readCount = 0;

            while (readCount < count) {
                int bytesRead;

                if (cancellationToken.HasValue) {
                    bytesRead = await this.innerStream.ReadAsync(buffer, readCount, count - readCount, cancellationToken.Value);
                } else {
                    bytesRead = await this.innerStream.ReadAsync(buffer, readCount, count - readCount);
                }

                if (bytesRead == 0) {
                    throw new EmptyStreamException();
                }

                readCount += bytesRead;
            }

            return buffer;
        }
    }
}
