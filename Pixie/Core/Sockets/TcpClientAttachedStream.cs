using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pixie.Core.Sockets
{
    class TcpClientAttachedStream : Stream
    {
        private TcpClient client;
        private Stream stream;

        public TcpClientAttachedStream(TcpClient client) {
            this.client = client;
            this.stream = client.GetStream();
        }

        public override bool CanRead => this.stream.CanRead;

        public override bool CanSeek => this.stream.CanSeek;

        public override bool CanWrite => this.stream.CanWrite;

        public override long Length => this.stream.Length;

        public override long Position { get => this.stream.Position; set => this.stream.Position = value; }

        public override void Flush() {
            this.stream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken) {
            return this.stream.FlushAsync();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return this.stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) {
            return this.stream.Seek(offset, origin);
        }

        public override void SetLength(long value) {
            this.stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            this.stream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            return this.stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            return this.stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override void Close() {
            this.stream.Close();
            this.client.Close();
        }
    }
}
