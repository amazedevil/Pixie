using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pixie.Core.Common.Streams
{
    internal class PXBinaryReaderAsync
    {
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

        public async Task<bool> ReadBool(CancellationToken? cancellationToken = null) {
            return await this.ReadByte() != 0;
        }

        public async Task<ushort> ReadUInt16(CancellationToken? cancellationToken = null) {
            return BitConverter.ToUInt16(await ReadBytes(sizeof(ushort), cancellationToken), 0);
        }

        public async Task<int> ReadInt32(CancellationToken? cancellationToken = null) {
            return BitConverter.ToInt32(await ReadBytes(sizeof(int), cancellationToken), 0);
        }

        public async Task<Guid> ReadGuid(CancellationToken? cancellationToken = null) {
            return new Guid(await ReadBytes(16, cancellationToken));
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

                readCount += bytesRead;
            }

            return buffer;
        }
    }
}
